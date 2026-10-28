using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Degerlendirme;
using Sds.Api.Domain;
using Sds.Api.Dosyalar;

namespace Sds.Api.Controllers;

public record EklemeBilgisi(int Id, string Tur, decimal Puan, string? Aciklama);

public record NotSatirBilgisi(
    int SonucId, string OgrenciNo, string AdSoyad, bool SinavaGirdi, char? Kitapcik,
    int Dogru, int Yanlis, int Bos, decimal HamPuan, decimal? ManuelPuan, List<EklemeBilgisi> Eklemeler,
    decimal? DuzeltilmisPuan, string? HarfNotu);

public record NotListesi(decimal MaksimumPuan, List<NotSatirBilgisi> Satirlar, OzetIstatistik Ozet);

public record ManuelPuanIstegi(decimal? Puan);
public record BonusIstegi(List<int> SonucIdleri, decimal Puan, string? Aciklama);
public record ProjeYuklemeSonucu(int Eklenen, List<string> BulunamayanNumaralar, List<string> Uyarilar);

/// <summary>4. adım: ham puanlar, elle düzeltme, proje notu yükleme ve bonus puan.</summary>
[ApiController]
[Route("api/sinavlar/{id:int}/notlar")]
[Authorize(Policy = Yetki.OgretimUyesi)]
public class NotlarController(SdsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<NotListesi>> Listele(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        return await Liste(sinav);
    }

    /// <summary>İtiraz, iptal vb. durumlarda puanın elle düzeltilmesi; null gönderilirse ham puana dönülür.</summary>
    [HttpPut("{sonucId:int}/manuel")]
    public async Task<ActionResult<NotListesi>> Manuel(int id, int sonucId, ManuelPuanIstegi istek)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var sonuc = await db.SinavSonuclari.FirstOrDefaultAsync(s => s.Id == sonucId && s.SinavId == id);
        if (sonuc is null) return NotFound(new { mesaj = "Öğrenci bulunamadı." });
        if (!sonuc.SinavaGirdi) return BadRequest(new { mesaj = "Sınava girmeyen öğrencinin puanı düzeltilemez." });
        if (istek.Puan is { } p && (p < 0 || p > sinav.MaksimumPuan))
            return BadRequest(new { mesaj = $"Puan 0 ile {sinav.MaksimumPuan:0.##} arasında olmalıdır." });

        sonuc.ManuelPuan = istek.Puan;
        await db.SaveChangesAsync();
        return await Liste(sinav);
    }

    [HttpPost("bonus")]
    public async Task<ActionResult<NotListesi>> Bonus(int id, BonusIstegi istek)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (istek.Puan == 0 || Math.Abs(istek.Puan) > sinav.MaksimumPuan)
            return BadRequest(new { mesaj = "Bonus puan sıfırdan farklı ve maksimum puandan küçük olmalıdır." });
        if (istek.SonucIdleri is null || istek.SonucIdleri.Count == 0)
            return BadRequest(new { mesaj = "Bonus eklenecek öğrencileri seçin." });

        var sonuclar = await db.SinavSonuclari.Where(s => s.SinavId == id && istek.SonucIdleri.Contains(s.Id)).ToListAsync();
        var girenler = sonuclar.Where(s => s.SinavaGirdi).ToList();
        if (girenler.Count == 0) return BadRequest(new { mesaj = "Seçilen öğrencilerin hiçbiri sınava girmemiş." });

        foreach (var s in girenler)
            db.PuanEklemeleri.Add(new PuanEklemesi { SinavSonucuId = s.Id, Tur = PuanEklemeTuru.Bonus, Puan = istek.Puan, Aciklama = istek.Aciklama?.Trim() });
        await db.SaveChangesAsync();
        return await Liste(sinav);
    }

    [HttpDelete("eklemeler/{eklemeId:int}")]
    public async Task<ActionResult<NotListesi>> EklemeSil(int id, int eklemeId)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var ekleme = await db.PuanEklemeleri.FirstOrDefaultAsync(e => e.Id == eklemeId && e.SinavSonucu.SinavId == id);
        if (ekleme is null) return NotFound(new { mesaj = "Ekleme bulunamadı." });

        db.PuanEklemeleri.Remove(ekleme);
        await db.SaveChangesAsync();
        return await Liste(sinav);
    }

    /// <summary>
    /// Proje / ek sınav notları: OGRENCINO, ADSOYAD, SINAVNOTU sütunlu Excel. SINAVNOTU, öğrencinin notuna eklenir.
    /// Aynı dosya tekrar yüklenirse önceki proje eklemeleri silinip yenileri yazılır.
    /// </summary>
    [HttpPost("proje")]
    [RequestSizeLimit(DosyalarController.EnBuyukBoyut)]
    public async Task<ActionResult<ProjeYuklemeSonucu>> Proje(int id, IFormFile dosya)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (dosya is null || dosya.Length == 0 || !dosya.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { mesaj = "OGRENCINO, ADSOYAD ve SINAVNOTU sütunlarını içeren bir .xlsx dosyası yükleyin." });

        NotTablosuSonucu tablo;
        try
        {
            using var akis = dosya.OpenReadStream();
            tablo = ExcelOkuyucu.NotTablosuOku(akis);
        }
        catch (DosyaOkumaHatasi e)
        {
            return BadRequest(new { mesaj = e.Message });
        }
        var sutun = tablo.Sutunlar.FirstOrDefault(s => Metin.Anahtar(s) is "sinavnotu" or "not" or "puan" or "projenotu") ?? tablo.Sutunlar[0];

        var sonuclar = await db.SinavSonuclari.Include(s => s.Ogrenci).Include(s => s.Eklemeler)
            .Where(s => s.SinavId == id).ToListAsync();
        var numaraIle = sonuclar.ToDictionary(s => s.Ogrenci.OgrenciNo);

        foreach (var s in sonuclar)
            db.PuanEklemeleri.RemoveRange(s.Eklemeler.Where(e => e.Tur == PuanEklemeTuru.ProjeNotu));

        var eklenen = 0;
        var bulunamayan = new List<string>();
        var uyarilar = new List<string>(tablo.Uyarilar);
        foreach (var satir in tablo.Satirlar)
        {
            if (satir.Degerler[sutun] is not { } puan || puan == 0) continue;
            if (!numaraIle.TryGetValue(satir.OgrenciNo, out var sonuc)) { bulunamayan.Add(satir.OgrenciNo); continue; }
            if (!sonuc.SinavaGirdi) { uyarilar.Add($"{satir.OgrenciNo} sınava girmediği için proje notu eklenmedi."); continue; }
            db.PuanEklemeleri.Add(new PuanEklemesi { SinavSonucuId = sonuc.Id, Tur = PuanEklemeTuru.ProjeNotu, Puan = puan, Aciklama = dosya.FileName });
            eklenen++;
        }
        await db.SaveChangesAsync();
        return new ProjeYuklemeSonucu(eklenen, bulunamayan, uyarilar);
    }

    [HttpPost("tamamla")]
    public async Task<IActionResult> Tamamla(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (sinav.TamamlananAdim < 3) return BadRequest(new { mesaj = "Önce veri eşleştirmeyi tamamlayın." });

        sinav.TamamlananAdim = Math.Max(sinav.TamamlananAdim, 4);
        sinav.GuncellemeTarihi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { tamamlananAdim = sinav.TamamlananAdim });
    }

    /// <summary>"Excel'e Aktar": not düzenleme tablosunun Excel çıktısı.</summary>
    [HttpGet("excel")]
    public async Task<IActionResult> Excel(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var ders = await db.Dersler.AsNoTracking().FirstAsync(d => d.Id == sinav.DersId);
        var liste = await Liste(sinav);
        var turAdi = sinav.Tur switch { SinavTuru.Vize => "Vize", SinavTuru.Final => "Final", _ => "Butunleme" };

        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("SinavSonuclari");
        string[] basliklar = ["OGRENCINO", "ADSOYAD", "DURUM", "DOGRU", "YANLIS", "BOS", "HAMPUAN", "EKLEMELER", "SINAVNOTU", "HARFNOTU"];
        for (var c = 0; c < basliklar.Length; c++) ws.Cell(1, c + 1).Value = basliklar[c];
        ws.Row(1).Style.Font.SetBold();

        var r = 2;
        foreach (var x in liste.Satirlar)
        {
            ws.Cell(r, 1).Value = x.OgrenciNo;
            ws.Cell(r, 1).Style.NumberFormat.Format = "@";
            ws.Cell(r, 2).Value = x.AdSoyad;
            ws.Cell(r, 3).Value = x.SinavaGirdi ? "Girdi" : "Girmedi";
            if (x.SinavaGirdi)
            {
                ws.Cell(r, 4).Value = x.Dogru;
                ws.Cell(r, 5).Value = x.Yanlis;
                ws.Cell(r, 6).Value = x.Bos;
                ws.Cell(r, 7).Value = x.ManuelPuan ?? x.HamPuan;
                ws.Cell(r, 8).Value = x.Eklemeler.Sum(e => e.Puan);
                ws.Cell(r, 9).Value = x.DuzeltilmisPuan;
                ws.Cell(r, 10).Value = x.HarfNotu;
            }
            r++;
        }
        if (sinav.Format == SinavFormati.AcikUclu)
        {
            ws.Cell(1, 4).Value = "TAM PUANLI SORU";
            ws.Cell(1, 5).Value = "KISMI PUANLI SORU";
            ws.Cell(1, 6).Value = "BOS SORU";
        }
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"{ders.Kod}_{turAdi}_Sinav_Notlari.xlsx");
    }

    private async Task<NotListesi> Liste(Sinav sinav)
    {
        var harfler = await db.HarfNotuAraliklari.AsNoTracking().OrderBy(h => h.Sira).ToListAsync();
        var sonuclar = await db.SinavSonuclari.AsNoTracking()
            .Include(s => s.Ogrenci).Include(s => s.Eklemeler)
            .Where(s => s.SinavId == sinav.Id)
            .OrderBy(s => s.Ogrenci.OgrenciNo)
            .ToListAsync();

        var satirlar = sonuclar.Select(s =>
        {
            decimal? duzeltilmis = s.SinavaGirdi ? s.DuzeltilmisPuan(sinav.MaksimumPuan) : null;
            // Harf notu 100 üzerinden hesaplanır
            var harf = duzeltilmis is { } d ? HarfNotu.Bul(d * 100 / sinav.MaksimumPuan, harfler)?.Harf : null;
            return new NotSatirBilgisi(s.Id, s.Ogrenci.OgrenciNo, s.Ogrenci.AdSoyad, s.SinavaGirdi, s.Kitapcik,
                s.DogruSayisi, s.YanlisSayisi, s.BosSayisi, s.HamPuan, s.ManuelPuan,
                s.Eklemeler.OrderBy(e => e.Tarih).Select(e => new EklemeBilgisi(e.Id, e.Tur.ToString(), e.Puan, e.Aciklama)).ToList(),
                duzeltilmis, harf);
        }).ToList();

        var ozet = Istatistik.Hesapla(satirlar.Count, satirlar.Where(x => x.DuzeltilmisPuan.HasValue).Select(x => x.DuzeltilmisPuan!.Value).ToList());
        return new NotListesi(sinav.MaksimumPuan, satirlar, ozet);
    }
}
