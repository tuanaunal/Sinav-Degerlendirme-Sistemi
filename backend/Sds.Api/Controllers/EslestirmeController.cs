using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Degerlendirme;
using Sds.Api.Domain;

namespace Sds.Api.Controllers;

public record EslesenKayit(
    int SonucId, string OgrenciNo, string AdSoyad, string? OptikNo, string? OptikAdSoyad, char? Kitapcik,
    bool NumaraFarkli, bool Manuel);
public record BekleyenKagit(int Id, string OgrenciNo, string AdSoyad, char? Kitapcik, int SatirNo, int? OneriSonucId, string? OneriAdSoyad);
public record BekleyenOgrenci(int SonucId, string OgrenciNo, string AdSoyad);

public record EslestirmeDurumu(
    int SinifMevcudu, int KagitSayisi, List<EslesenKayit> Eslesenler, List<BekleyenKagit> BekleyenKagitlar,
    List<BekleyenKagit> GozArdiEdilenler, List<BekleyenOgrenci> BekleyenOgrenciler, bool KitapcikGerekli);

public record ElleEslestir(int OptikKaydiId, int SinavSonucuId);
public record KitapcikSec(int SinavSonucuId, char Kitapcik);

/// <summary>
/// 3. adım: optik kağıtlarının sınıf listesiyle eşleştirilmesi. İlk açılışta otomatik eşleştirme yapılır;
/// kalanlar elle eşleştirilir veya göz ardı edilir. Tamamlanınca eşleşmeyen öğrenciler "sınava girmedi" olur ve puanlar hesaplanır.
/// </summary>
[ApiController]
[Route("api/sinavlar/{id:int}/eslestirme")]
[Authorize(Policy = Yetki.OgretimUyesi)]
public class EslestirmeController(SdsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EslestirmeDurumu>> Getir(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (sinav.TamamlananAdim < 2) return BadRequest(new { mesaj = "Önce cevap anahtarını kaydedin." });

        // Hiç eşleştirme yapılmamışsa otomatik eşleştir
        if (!await db.OptikKayitlari.AnyAsync(o => o.SinavId == id && !o.CevapAnahtariMi && o.Durum != OptikKayitDurumu.Bekliyor))
        {
            await OtomatikEslestirAsync(sinav);
            await db.SaveChangesAsync();
        }
        return await Durum(sinav);
    }

    [HttpPost("eslestir")]
    public async Task<ActionResult<EslestirmeDurumu>> Eslestir(int id, ElleEslestir istek)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });

        var kagit = await db.OptikKayitlari.FirstOrDefaultAsync(o => o.Id == istek.OptikKaydiId && o.SinavId == id && !o.CevapAnahtariMi);
        var sonuc = await db.SinavSonuclari.Include(s => s.Ogrenci).FirstOrDefaultAsync(s => s.Id == istek.SinavSonucuId && s.SinavId == id);
        if (kagit is null || sonuc is null) return NotFound(new { mesaj = "Kağıt veya öğrenci bulunamadı." });
        if (kagit.Durum == OptikKayitDurumu.Eslesti) return BadRequest(new { mesaj = "Bu kağıt zaten bir öğrenciyle eşleşmiş." });
        if (sonuc.SinavaGirdi) return BadRequest(new { mesaj = $"{sonuc.Ogrenci.AdSoyad} zaten bir kağıtla eşleşmiş." });

        Bagla(kagit, sonuc, EslesmeDurumu.Manuel);
        await db.SaveChangesAsync();
        return await Durum(sinav);
    }

    [HttpPost("gozardi/{kagitId:int}")]
    public async Task<ActionResult<EslestirmeDurumu>> GozArdi(int id, int kagitId, [FromQuery] bool geriAl = false)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var kagit = await db.OptikKayitlari.FirstOrDefaultAsync(o => o.Id == kagitId && o.SinavId == id && !o.CevapAnahtariMi);
        if (kagit is null) return NotFound(new { mesaj = "Kağıt bulunamadı." });
        if (kagit.Durum == OptikKayitDurumu.Eslesti) return BadRequest(new { mesaj = "Eşleşmiş kağıt göz ardı edilemez; önce eşleşmeyi kaldırın." });

        kagit.Durum = geriAl ? OptikKayitDurumu.Bekliyor : OptikKayitDurumu.GozArdiEdildi;
        await db.SaveChangesAsync();
        return await Durum(sinav);
    }

    [HttpPost("kaldir/{sonucId:int}")]
    public async Task<ActionResult<EslestirmeDurumu>> EslesmeyiKaldir(int id, int sonucId)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var sonuc = await db.SinavSonuclari.FirstOrDefaultAsync(s => s.Id == sonucId && s.SinavId == id);
        if (sonuc is null) return NotFound(new { mesaj = "Öğrenci bulunamadı." });

        var kagit = await db.OptikKayitlari.FirstOrDefaultAsync(o => o.SinavSonucuId == sonucId);
        if (kagit is not null)
        {
            kagit.Durum = OptikKayitDurumu.Bekliyor;
            kagit.SinavSonucuId = null;
        }
        sonuc.Eslesme = EslesmeDurumu.Bekliyor;
        (sonuc.Kitapcik, sonuc.OptikOgrenciNo, sonuc.OptikAdSoyad, sonuc.OptikCevaplar) = (null, null, null, null);
        sinav.TamamlananAdim = Math.Min(sinav.TamamlananAdim, 2);
        await db.SaveChangesAsync();
        return await Durum(sinav);
    }

    /// <summary>Kitapçık türünü boş bırakan öğrenci için öğretim üyesinin seçtiği kitapçık.</summary>
    [HttpPost("kitapcik")]
    public async Task<ActionResult<EslestirmeDurumu>> Kitapcik(int id, KitapcikSec istek)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var sonuc = await db.SinavSonuclari.FirstOrDefaultAsync(s => s.Id == istek.SinavSonucuId && s.SinavId == id);
        if (sonuc is null || !sonuc.SinavaGirdi) return NotFound(new { mesaj = "Eşleşmiş öğrenci bulunamadı." });
        var harf = char.ToUpperInvariant(istek.Kitapcik);
        if (harf < 'A' || harf >= 'A' + sinav.KitapcikSayisi) return BadRequest(new { mesaj = "Geçersiz kitapçık." });

        sonuc.Kitapcik = harf;
        await db.SaveChangesAsync();
        return await Durum(sinav);
    }

    /// <summary>Tüm eşleştirmeleri silip otomatik eşleştirmeyi yeniden çalıştırır ("Sıfırla").</summary>
    [HttpPost("sifirla")]
    public async Task<ActionResult<EslestirmeDurumu>> Sifirla(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });

        foreach (var k in await db.OptikKayitlari.Where(o => o.SinavId == id).ToListAsync())
            (k.Durum, k.SinavSonucuId) = (OptikKayitDurumu.Bekliyor, null);
        foreach (var s in await db.SinavSonuclari.Where(s => s.SinavId == id).ToListAsync())
            (s.Eslesme, s.Kitapcik, s.OptikOgrenciNo, s.OptikAdSoyad, s.OptikCevaplar) = (EslesmeDurumu.Bekliyor, null, null, null, null);
        await db.SaveChangesAsync();

        await OtomatikEslestirAsync(sinav);
        sinav.TamamlananAdim = Math.Min(sinav.TamamlananAdim, 2);
        await db.SaveChangesAsync();
        return await Durum(sinav);
    }

    /// <summary>
    /// "İleri": bekleyen kağıt kalmamalı (eşleştirilmeli veya göz ardı edilmeli) ve her kağıdın kitapçığı belli olmalı.
    /// Eşleşmeyen öğrenciler sınava girmedi sayılır, puanlar hesaplanır.
    /// </summary>
    [HttpPost("tamamla")]
    public async Task<IActionResult> Tamamla(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });

        var bekleyen = await db.OptikKayitlari.CountAsync(o => o.SinavId == id && !o.CevapAnahtariMi && o.Durum == OptikKayitDurumu.Bekliyor);
        if (bekleyen > 0)
            return BadRequest(new { mesaj = $"{bekleyen} optik kağıdı henüz eşleştirilmedi. Bir öğrenciyle eşleştirin veya göz ardı edin." });

        var sonuclar = await db.SinavSonuclari.Where(s => s.SinavId == id).ToListAsync();
        if (sinav.KitapcikSayisi > 1 && sonuclar.Any(s => s.SinavaGirdi && s.Kitapcik is null))
            return BadRequest(new { mesaj = "Kitapçık türü boş olan öğrenciler için kitapçık seçin." });

        foreach (var s in sonuclar.Where(s => s.Eslesme == EslesmeDurumu.Bekliyor))
            s.Eslesme = EslesmeDurumu.SinavaGirmedi;
        await db.SaveChangesAsync();

        await SinavPuanlama.YenidenHesaplaAsync(db, sinav);
        sinav.TamamlananAdim = Math.Max(sinav.TamamlananAdim, 3);
        sinav.GuncellemeTarihi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { mesaj = "Eşleştirme tamamlandı.", tamamlananAdim = sinav.TamamlananAdim });
    }

    private async Task OtomatikEslestirAsync(Sinav sinav)
    {
        var sonuclar = await db.SinavSonuclari.Include(s => s.Ogrenci)
            .Where(s => s.SinavId == sinav.Id && s.Eslesme == EslesmeDurumu.Bekliyor).ToListAsync();
        var kagitlar = await db.OptikKayitlari
            .Where(o => o.SinavId == sinav.Id && !o.CevapAnahtariMi && o.Durum == OptikKayitDurumu.Bekliyor).ToListAsync();

        var eslesmeler = Eslestirici.Eslestir(
            sonuclar.Select(s => new EslestirmeOgrencisi(s.Id, s.Ogrenci.OgrenciNo, s.Ogrenci.AdSoyad)).ToList(),
            kagitlar.Select(k => new EslestirmeKagidi(k.Id, k.OgrenciNo, k.AdSoyad)).ToList());

        foreach (var e in eslesmeler)
            Bagla(kagitlar.Single(k => k.Id == e.KagitId), sonuclar.Single(s => s.Id == e.OgrenciId), EslesmeDurumu.Otomatik);
    }

    private static void Bagla(OptikKaydi kagit, SinavSonucu sonuc, EslesmeDurumu durum)
    {
        kagit.Durum = OptikKayitDurumu.Eslesti;
        kagit.SinavSonucuId = sonuc.Id;
        sonuc.Eslesme = durum;
        sonuc.Kitapcik = kagit.Kitapcik;
        sonuc.OptikOgrenciNo = kagit.OgrenciNo;
        sonuc.OptikAdSoyad = kagit.AdSoyad;
        sonuc.OptikCevaplar = kagit.Cevaplar;
    }

    private async Task<EslestirmeDurumu> Durum(Sinav sinav)
    {
        var sonuclar = await db.SinavSonuclari.Include(s => s.Ogrenci).Where(s => s.SinavId == sinav.Id)
            .OrderBy(s => s.Ogrenci.OgrenciNo).ToListAsync();
        var kagitlar = await db.OptikKayitlari.Where(o => o.SinavId == sinav.Id && !o.CevapAnahtariMi)
            .OrderBy(o => o.SatirNo).ToListAsync();

        var bosOgrenciler = sonuclar.Where(s => s.Eslesme is EslesmeDurumu.Bekliyor or EslesmeDurumu.SinavaGirmedi).ToList();
        var bosAdaylar = bosOgrenciler.Select(s => new EslestirmeOgrencisi(s.Id, s.Ogrenci.OgrenciNo, s.Ogrenci.AdSoyad)).ToList();

        BekleyenKagit Kagit(OptikKaydi k)
        {
            var oneri = Eslestirici.Oner(new EslestirmeKagidi(k.Id, k.OgrenciNo, k.AdSoyad), bosAdaylar);
            return new BekleyenKagit(k.Id, k.OgrenciNo, k.AdSoyad, k.Kitapcik, k.SatirNo, oneri?.Id, oneri?.AdSoyad);
        }

        var eslesenler = sonuclar.Where(s => s.SinavaGirdi).Select(s => new EslesenKayit(
            s.Id, s.Ogrenci.OgrenciNo, s.Ogrenci.AdSoyad, s.OptikOgrenciNo, s.OptikAdSoyad, s.Kitapcik,
            s.OptikOgrenciNo != s.Ogrenci.OgrenciNo, s.Eslesme == EslesmeDurumu.Manuel)).ToList();

        return new EslestirmeDurumu(
            sonuclar.Count, kagitlar.Count, eslesenler,
            kagitlar.Where(k => k.Durum == OptikKayitDurumu.Bekliyor).Select(Kagit).ToList(),
            kagitlar.Where(k => k.Durum == OptikKayitDurumu.GozArdiEdildi).Select(Kagit).ToList(),
            bosOgrenciler.Select(s => new BekleyenOgrenci(s.Id, s.Ogrenci.OgrenciNo, s.Ogrenci.AdSoyad)).ToList(),
            sinav.KitapcikSayisi > 1);
    }
}
