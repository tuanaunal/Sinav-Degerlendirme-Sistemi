using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Degerlendirme;
using Sds.Api.Domain;

namespace Sds.Api.Controllers;

public record AnalizGenelBilgi(
    string DersKodu, string DersAdi, string AkademikYil, string Donem, string Tur, string Format,
    int SoruSayisi, decimal MaksimumPuan, int KitapcikSayisi, DateOnly? Tarih);

public record DagilimAraligi(string Etiket, int Alt, int Ust, int Sayi);

public record SoruAnalizi(
    int SoruNo, decimal Puan, string? DogruCevap, decimal BasariOrani, decimal OrtalamaPuan,
    int Dogru, int Yanlis, int Bos, Dictionary<string, decimal> SikOranlari);

public record CiktiAnalizi(int SiraNo, string Aciklama, List<int> Sorular, decimal? BasariOrani);
public record HarfSayisi(string Harf, string Durum, int Sayi);
public record OgrenciSecenegi(int SonucId, string OgrenciNo, string AdSoyad, bool SinavaGirdi);

public record SinavAnaliziYaniti(
    AnalizGenelBilgi Genel, OzetIstatistik Ozet, decimal? BasariOrani, int GecmeSiniri,
    List<DagilimAraligi> PuanDagilimi, List<SoruAnalizi> Sorular, List<CiktiAnalizi> Ciktilar,
    List<HarfSayisi> HarfDagilimi, List<OgrenciSecenegi> Ogrenciler);

public record OgrenciSorusu(int SoruNo, string? Verilen, string? DogruCevap, bool? DogruMu, decimal Puan, decimal MaksPuan);
public record OgrenciAnalizi(
    int SonucId, string OgrenciNo, string AdSoyad, bool SinavaGirdi, char? Kitapcik,
    decimal HamPuan, decimal? DuzeltilmisPuan, string? HarfNotu, decimal Eklemeler, List<OgrenciSorusu> Sorular);

/// <summary>Sınav Başarı Analiz Raporu: özet istatistikler, puan dağılımı, soru ve şık analizi, öğrenme çıktısı başarısı.</summary>
[ApiController]
[Route("api/sinavlar/{id:int}/analiz")]
[Authorize(Policy = Yetki.OgretimUyesi)]
public class AnalizController(SdsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SinavAnaliziYaniti>> Getir(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (sinav.TamamlananAdim < 4) return BadRequest(new { mesaj = "Analiz için sınav girişinin en az not düzenleme adımına kadar tamamlanması gerekir." });
        return await SinavAnalizServisi.HesaplaAsync(db, sinav);
    }

    /// <summary>Seçilen öğrencinin her soruya verdiği cevap ve aldığı puan (A kitapçığı sırasıyla).</summary>
    [HttpGet("ogrenci/{sonucId:int}")]
    public async Task<ActionResult<OgrenciAnalizi>> Ogrenci(int id, int sonucId)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var s = await db.SinavSonuclari.AsNoTracking().Include(x => x.Ogrenci).Include(x => x.Eklemeler).Include(x => x.SoruCevaplari)
            .FirstOrDefaultAsync(x => x.Id == sonucId && x.SinavId == id);
        if (s is null) return NotFound(new { mesaj = "Öğrenci bulunamadı." });

        var sorular = await db.SinavSorulari.AsNoTracking().Where(q => q.SinavId == id).OrderBy(q => q.SoruNo).ToListAsync();
        var anahtarA = await db.KitapcikSorulari.AsNoTracking().Where(k => k.SinavId == id && k.Kitapcik == 'A')
            .ToDictionaryAsync(k => k.SoruNo, k => k.DogruCevap);
        var harfler = await db.HarfNotuAraliklari.AsNoTracking().ToListAsync();

        decimal? duzeltilmis = s.SinavaGirdi ? s.DuzeltilmisPuan(sinav.MaksimumPuan) : null;
        var detay = sorular.Select(q =>
        {
            var c = s.SoruCevaplari.FirstOrDefault(x => x.SoruNo == q.SoruNo);
            return new OgrenciSorusu(q.SoruNo, c?.VerilenCevap?.ToString(), anahtarA.GetValueOrDefault(q.SoruNo), c?.DogruMu, c?.Puan ?? 0, q.Puan);
        }).ToList();

        return new OgrenciAnalizi(s.Id, s.Ogrenci.OgrenciNo, s.Ogrenci.AdSoyad, s.SinavaGirdi, s.Kitapcik, s.HamPuan, duzeltilmis,
            duzeltilmis is { } d ? HarfNotu.Bul(d * 100 / sinav.MaksimumPuan, harfler)?.Harf : null,
            s.Eklemeler.Sum(e => e.Puan), detay);
    }

    /// <summary>Sınav Başarısı Analiz ve Değerlendirme Formu (Analiz, Liste, İÖK Kağıt sayfaları).</summary>
    [HttpGet("~/api/sinavlar/{id:int}/resmi-form")]
    public async Task<IActionResult> ResmiForm(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (sinav.TamamlananAdim < 4) return BadRequest(new { mesaj = "Form için sınav girişinin en az not düzenleme adımına kadar tamamlanması gerekir." });
        var (icerik, ad) = await ResmiFormUretici.UretAsync(db, sinav);
        return File(icerik, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad);
    }

    /// <summary>Kayıtlı "Sınavın Değerlendirmesi" metni ve istatistiklerden üretilen öneri metni.</summary>
    [HttpGet("~/api/sinavlar/{id:int}/degerlendirme")]
    public async Task<ActionResult<DegerlendirmeMetni>> Degerlendirme(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (sinav.TamamlananAdim < 4) return BadRequest(new { mesaj = "Değerlendirme için sınav girişinin en az not düzenleme adımına kadar tamamlanması gerekir." });
        var analiz = await SinavAnalizServisi.HesaplaAsync(db, sinav);
        return new DegerlendirmeMetni(sinav.DegerlendirmeMetni, ResmiFormUretici.OtomatikDegerlendirme(analiz));
    }

    [HttpPut("~/api/sinavlar/{id:int}/degerlendirme")]
    public async Task<IActionResult> DegerlendirmeKaydet(int id, DegerlendirmeKaydi kayit)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var metin = kayit.Metin?.Trim();
        if (metin?.Length > 4000) return BadRequest(new { mesaj = "Değerlendirme metni en fazla 4000 karakter olabilir." });
        var izlenen = await db.Sinavlar.FirstAsync(s => s.Id == id);
        izlenen.DegerlendirmeMetni = string.IsNullOrEmpty(metin) ? null : metin;
        await db.SaveChangesAsync();
        return NoContent();
    }
}

public record DegerlendirmeMetni(string? Kayitli, string Otomatik);
public record DegerlendirmeKaydi(string? Metin);
