using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Dosyalar;

namespace Sds.Api.Controllers;

public record OptikOnizleme(
    string DosyaAdi, string Kodlama, int OgrenciSayisi, int CevapAnahtariSayisi, int TahminiSoruSayisi,
    Dictionary<string, int> KitapcikDagilimi, List<OptikSatir> Satirlar, List<string> Uyarilar);

public record SinifListesiOnizleme(string DosyaAdi, int OgrenciSayisi, List<SinifListesiOgrencisi> Ogrenciler, List<string> Uyarilar);

public record NotTablosuOnizleme(string DosyaAdi, List<string> Sutunlar, int SatirSayisi, List<NotSatiri> Satirlar, List<string> Uyarilar);

/// <summary>
/// Yüklenen dosyaları okuyup ne anlaşıldığını döndürür (veritabanına yazmaz).
/// Sınav girişi sihirbazının dosya yükleme adımı bu sonuçları gösterir.
/// </summary>
[ApiController]
[Route("api/dosyalar")]
[Authorize(Policy = Yetki.OgretimUyesi)]
[RequestSizeLimit(EnBuyukBoyut)]
public class DosyalarController(SdsDbContext db) : ControllerBase
{
    public const int EnBuyukBoyut = 5 * 1024 * 1024;

    [HttpPost("optik/onizle")]
    public async Task<ActionResult<OptikOnizleme>> Optik(IFormFile dosya)
    {
        if (Kontrol(dosya, ".txt", ".dat") is { } hata) return hata;
        var alanlar = await db.OptikFormAlanlari.AsNoTracking().ToListAsync();

        using var ms = new MemoryStream();
        await dosya.CopyToAsync(ms);
        try
        {
            var s = OptikOkuyucu.Oku(ms.ToArray(), alanlar);
            var dagilim = s.Ogrenciler
                .GroupBy(o => o.Kitapcik?.ToString() ?? "Boş")
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.Count());
            return new OptikOnizleme(dosya.FileName, s.Kodlama, s.Ogrenciler.Count(), s.CevapAnahtarlari.Count(),
                s.TahminiSoruSayisi, dagilim, s.Satirlar, s.Uyarilar);
        }
        catch (DosyaOkumaHatasi e)
        {
            return BadRequest(new { mesaj = e.Message });
        }
    }

    [HttpPost("sinif-listesi/onizle")]
    public ActionResult<SinifListesiOnizleme> SinifListesi(IFormFile dosya)
    {
        if (Kontrol(dosya, ".xlsx") is { } hata) return hata;
        try
        {
            using var akis = dosya.OpenReadStream();
            var s = ExcelOkuyucu.SinifListesiOku(akis);
            return new SinifListesiOnizleme(dosya.FileName, s.Ogrenciler.Count, s.Ogrenciler, s.Uyarilar);
        }
        catch (DosyaOkumaHatasi e)
        {
            return BadRequest(new { mesaj = e.Message });
        }
    }

    [HttpPost("not-tablosu/onizle")]
    public ActionResult<NotTablosuOnizleme> NotTablosu(IFormFile dosya)
    {
        if (Kontrol(dosya, ".xlsx") is { } hata) return hata;
        try
        {
            using var akis = dosya.OpenReadStream();
            var s = ExcelOkuyucu.NotTablosuOku(akis);
            return new NotTablosuOnizleme(dosya.FileName, s.Sutunlar, s.Satirlar.Count, s.Satirlar, s.Uyarilar);
        }
        catch (DosyaOkumaHatasi e)
        {
            return BadRequest(new { mesaj = e.Message });
        }
    }

    private BadRequestObjectResult? Kontrol(IFormFile? dosya, params string[] uzantilar)
    {
        if (dosya is null || dosya.Length == 0) return BadRequest(new { mesaj = "Dosya seçilmedi veya dosya boş." });
        if (dosya.Length > EnBuyukBoyut) return BadRequest(new { mesaj = "Dosya 5 MB'tan büyük olamaz." });
        var uzanti = Path.GetExtension(dosya.FileName).ToLowerInvariant();
        return uzantilar.Contains(uzanti)
            ? null
            : BadRequest(new { mesaj = $"Bu alana yalnızca {string.Join(" / ", uzantilar)} uzantılı dosya yüklenebilir." });
    }
}
