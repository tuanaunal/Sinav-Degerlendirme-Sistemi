using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Domain;
using Sds.Api.Dosyalar;

namespace Sds.Api.Controllers;

public record SinavBilgisi(
    int Id, int DersId, string Tur, string Format, string Durum, int TamamlananAdim,
    int SoruSayisi, decimal MaksimumPuan, int KitapcikSayisi, bool PuanlariDinamikDagit, bool BirdenFazlaDogruKabul,
    string? OptikDosyaAdi, string? SinifListesiDosyaAdi, int SinifMevcudu, int OptikKagitSayisi, List<string> Uyarilar);

public record SoruPuani(int SoruNo, decimal Puan);
public record KitapcikCevaplari(char Kitapcik, List<string> Cevaplar);
public record OptikSecenegi(int Id, string Etiket, char? Kitapcik, string Cevaplar, bool CevapAnahtariMi);

public record CevapAnahtari(
    int SoruSayisi, decimal MaksimumPuan, int KitapcikSayisi, bool PuanlariDinamikDagit, bool BirdenFazlaDogruKabul,
    List<SoruPuani> Sorular, List<KitapcikCevaplari> Kitapciklar, List<OptikSecenegi> OptikSecenekleri);

public record CevapAnahtariKaydi(
    int SoruSayisi, decimal MaksimumPuan, int KitapcikSayisi, bool PuanlariDinamikDagit, bool BirdenFazlaDogruKabul,
    List<SoruPuani> Sorular, List<KitapcikCevaplari> Kitapciklar);

/// <summary>Sınav girişi sihirbazı: 1. adım dosya yükleme, 2. adım cevap anahtarı.</summary>
[ApiController]
[Authorize(Policy = Yetki.OgretimUyesi)]
public class SinavlarController(SdsDbContext db) : ControllerBase
{
    private const string Siklar = "ABCDE";

    [HttpGet("api/dersler/{dersId:int}/sinavlar/{tur}")]
    public async Task<ActionResult<SinavBilgisi>> Getir(int dersId, string tur)
    {
        if (!Enum.TryParse<SinavTuru>(tur, true, out var sinavTuru)) return NotFound(new { mesaj = "Geçersiz sınav türü." });
        if (!await DersErisimiVar(dersId)) return NotFound(new { mesaj = "Ders bulunamadı veya bu derse erişim yetkiniz yok." });

        var sinav = await db.Sinavlar.FirstOrDefaultAsync(s => s.DersId == dersId && s.Tur == sinavTuru);
        return sinav is null ? NotFound(new { mesaj = "Bu sınav için henüz giriş yapılmamış." }) : await Bilgi(sinav, []);
    }

    /// <summary>
    /// 1. adım: optik dosyası ve sınıf listesi (en fazla iki şube listesi) yüklenir. Sınav yoksa oluşturulur,
    /// taslak hâldeyse dosyalar değiştirilir (girilmiş cevap anahtarı korunur).
    /// </summary>
    [HttpPost("api/dersler/{dersId:int}/sinavlar/{tur}/dosyalar")]
    [RequestSizeLimit(3 * DosyalarController.EnBuyukBoyut)]
    public async Task<ActionResult<SinavBilgisi>> DosyalariYukle(
        int dersId, string tur, [FromForm] SinavFormati format, IFormFile? optik, [FromForm] List<IFormFile>? sinifListeleri)
    {
        if (!Enum.TryParse<SinavTuru>(tur, true, out var sinavTuru)) return NotFound(new { mesaj = "Geçersiz sınav türü." });
        if (!await DersErisimiVar(dersId)) return NotFound(new { mesaj = "Ders bulunamadı veya bu derse erişim yetkiniz yok." });
        if (format != SinavFormati.CoktanSecmeli)
            return BadRequest(new { mesaj = "Bu adım çoktan seçmeli sınavlar içindir." });
        if (optik is null || optik.Length == 0) return BadRequest(new { mesaj = "Optik okuyucu dosyası (.txt) yüklenmedi." });
        if (sinifListeleri is null || sinifListeleri.Count == 0) return BadRequest(new { mesaj = "Sınıf listesi (.xlsx) yüklenmedi." });
        if (sinifListeleri.Count > 2) return BadRequest(new { mesaj = "En fazla iki sınıf listesi yüklenebilir." });

        var sinav = await db.Sinavlar.FirstOrDefaultAsync(s => s.DersId == dersId && s.Tur == sinavTuru);
        if (sinav is { Durum: SinavDurumu.Tamamlandi })
            return Conflict(new { mesaj = "Bu sınav tamamlanmış. Dosyaları değiştirmek için \"Sınavı Düzenle\"yi kullanın." });
        if (sinav is not null && sinav.Format != format)
            return Conflict(new { mesaj = "Bu sınav için farklı türde (açık uçlu) bir giriş başlatılmış." });

        // Dosyaları oku
        OptikOkumaSonucu optikSonuc;
        var ogrenciler = new Dictionary<string, SinifListesiOgrencisi>();
        var uyarilar = new List<string>();
        try
        {
            using var ms = new MemoryStream();
            await optik.CopyToAsync(ms);
            optikSonuc = OptikOkuyucu.Oku(ms.ToArray(), await db.OptikFormAlanlari.AsNoTracking().ToListAsync());
            uyarilar.AddRange(optikSonuc.Uyarilar);

            foreach (var liste in sinifListeleri)
            {
                using var akis = liste.OpenReadStream();
                var s = ExcelOkuyucu.SinifListesiOku(akis);
                uyarilar.AddRange(s.Uyarilar);
                foreach (var o in s.Ogrenciler)
                    if (!ogrenciler.TryAdd(o.OgrenciNo, o))
                        uyarilar.Add($"{o.OgrenciNo} numaralı öğrenci birden fazla listede geçiyor; bir kez alındı.");
            }
        }
        catch (DosyaOkumaHatasi e)
        {
            return BadRequest(new { mesaj = e.Message });
        }

        var beklenenTurKodu = sinavTuru switch { SinavTuru.Vize => '1', SinavTuru.Final => '2', _ => '3' };
        var farkliTur = optikSonuc.Ogrenciler.Count(o => o.SinavTuru is { } t && t != beklenenTurKodu);
        if (farkliTur > optikSonuc.Ogrenciler.Count() / 2)
            uyarilar.Add($"Optik dosyasındaki kağıtların çoğunda sınav türü {beklenenTurKodu} değil; doğru dosyayı yüklediğinizden emin olun.");

        // Tüm değişiklikler tek SaveChanges ile atomik olarak kaydedilir
        if (sinav is null)
        {
            sinav = new Sinav
            {
                DersId = dersId, Tur = sinavTuru, Format = format, OlusturanKullaniciId = User.KullaniciId(),
                Tarih = DateOnly.FromDateTime(DateTime.Now),
            };
            db.Sinavlar.Add(sinav);
        }
        else
        {
            // Eski yükleme: sonuçlar ve optik kayıtları silinir, cevap anahtarı korunur
            db.OptikKayitlari.RemoveRange(await db.OptikKayitlari.Where(o => o.SinavId == sinav.Id).ToListAsync());
            db.SinavSonuclari.RemoveRange(await db.SinavSonuclari.Where(c => c.SinavId == sinav.Id)
                .Include(c => c.SoruCevaplari).Include(c => c.Eklemeler).ToListAsync());
        }

        sinav.OptikDosyaAdi = optik.FileName;
        sinav.SinifListesiDosyaAdi = string.Join("; ", sinifListeleri.Select(l => l.FileName));
        sinav.TamamlananAdim = 1;
        sinav.GuncellemeTarihi = DateTime.UtcNow;
        if (sinav.SoruSayisi == 0) sinav.SoruSayisi = optikSonuc.TahminiSoruSayisi;
        if (sinav.Id == 0 || !await db.KitapcikSorulari.AnyAsync(k => k.SinavId == sinav.Id))
        {
            var enBuyuk = optikSonuc.Ogrenciler.Where(o => o.Kitapcik is >= 'A' and <= 'E').Select(o => o.Kitapcik!.Value).DefaultIfEmpty('A').Max();
            sinav.KitapcikSayisi = enBuyuk - 'A' + 1;
        }

        // Öğrenciler: numara ortak anahtardır, ad soyad son listeye göre güncellenir
        var numaralar = ogrenciler.Keys.ToList();
        var mevcut = await db.Ogrenciler.Where(o => numaralar.Contains(o.OgrenciNo)).ToDictionaryAsync(o => o.OgrenciNo);
        foreach (var o in ogrenciler.Values)
        {
            if (!mevcut.TryGetValue(o.OgrenciNo, out var ogr))
            {
                ogr = new Ogrenci { OgrenciNo = o.OgrenciNo };
                db.Ogrenciler.Add(ogr);
                mevcut[o.OgrenciNo] = ogr;
            }
            if (o.AdSoyad.Length > 0) ogr.AdSoyad = o.AdSoyad;
            sinav.Sonuclar.Add(new SinavSonucu { Ogrenci = ogr, Eslesme = EslesmeDurumu.Bekliyor });
        }

        foreach (var s in optikSonuc.Satirlar)
        {
            sinav.OptikKayitlari.Add(new OptikKaydi
            {
                SatirNo = s.SatirNo, OgrenciNo = s.OgrenciNo, AdSoyad = s.AdSoyad, Kitapcik = s.Kitapcik,
                SinavTuruKodu = s.SinavTuru, Cevaplar = s.Cevaplar, CevapAnahtariMi = s.CevapAnahtariMi,
            });
        }

        await db.SaveChangesAsync();
        return await Bilgi(sinav, uyarilar);
    }

    /// <summary>2. adım verileri: mevcut cevap anahtarı ve "Optikten seç" seçenekleri.</summary>
    [HttpGet("api/sinavlar/{id:int}/cevap-anahtari")]
    public async Task<ActionResult<CevapAnahtari>> CevapAnahtariGetir(int id)
    {
        var sinav = await ErisilebilirSinav(id);
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });

        var sorular = await db.SinavSorulari.Where(s => s.SinavId == id).OrderBy(s => s.SoruNo).ToListAsync();
        var kitapcikSorulari = await db.KitapcikSorulari.Where(k => k.SinavId == id).ToListAsync();
        var n = sinav.SoruSayisi;

        var puanlar = sorular.Count == n
            ? sorular.Select(s => new SoruPuani(s.SoruNo, s.Puan)).ToList()
            : PuanDagitimi.EsitDagit(sinav.MaksimumPuan, n).Select((p, i) => new SoruPuani(i + 1, p)).ToList();

        var kitapciklar = Enumerable.Range(0, sinav.KitapcikSayisi).Select(i =>
        {
            var harf = (char)('A' + i);
            var cevaplar = Enumerable.Range(1, n)
                .Select(no => kitapcikSorulari.FirstOrDefault(k => k.Kitapcik == harf && k.SoruNo == no)?.DogruCevap ?? "")
                .ToList();
            return new KitapcikCevaplari(harf, cevaplar);
        }).ToList();

        var kayitlar = await db.OptikKayitlari.Where(o => o.SinavId == id)
            .OrderByDescending(o => o.CevapAnahtariMi).ThenBy(o => o.SatirNo).ToListAsync();
        var turAdi = sinav.Tur switch { SinavTuru.Vize => "Vize", SinavTuru.Final => "Final", _ => "Bütünleme" };
        var secenekler = kayitlar.Select(o => new OptikSecenegi(
            o.Id,
            o.CevapAnahtariMi
                ? $"({o.Kitapcik} Kitapçık) {o.AdSoyad}"
                : $"{(o.OgrenciNo.Length > 0 ? o.OgrenciNo : "Numarasız")} - {o.AdSoyad} ({(o.Kitapcik is null ? "Kitapçık boş" : $"{o.Kitapcik} Kitapçık")} - {turAdi})",
            o.Kitapcik, o.Cevaplar, o.CevapAnahtariMi)).ToList();

        return new CevapAnahtari(n, sinav.MaksimumPuan, sinav.KitapcikSayisi, sinav.PuanlariDinamikDagit,
            sinav.BirdenFazlaDogruKabul, puanlar, kitapciklar, secenekler);
    }

    [HttpPut("api/sinavlar/{id:int}/cevap-anahtari")]
    public async Task<ActionResult<SinavBilgisi>> CevapAnahtariKaydet(int id, CevapAnahtariKaydi istek)
    {
        var sinav = await ErisilebilirSinav(id);
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (sinav.TamamlananAdim < 1) return BadRequest(new { mesaj = "Önce dosyaları yükleyin." });
        if (CevapAnahtariniDogrula(istek) is { } hata) return BadRequest(new { mesaj = hata });

        var n = istek.SoruSayisi;
        var puanlar = istek.PuanlariDinamikDagit
            ? PuanDagitimi.EsitDagit(istek.MaksimumPuan, n)
            : istek.Sorular.OrderBy(s => s.SoruNo).Select(s => s.Puan).ToArray();

        var eskiEslestirme = await db.KitapcikSorulari.Where(k => k.SinavId == id && k.Kitapcik != 'A')
            .ToDictionaryAsync(k => (k.Kitapcik, k.SoruNo), k => k.AnaSoruNo);
        var soruSayisiDegisti = sinav.SoruSayisi != n;

        db.SinavSorulari.RemoveRange(await db.SinavSorulari.Where(s => s.SinavId == id).ToListAsync());
        db.KitapcikSorulari.RemoveRange(await db.KitapcikSorulari.Where(k => k.SinavId == id).ToListAsync());

        for (var i = 0; i < n; i++)
            db.SinavSorulari.Add(new SinavSorusu { SinavId = id, SoruNo = i + 1, Puan = puanlar[i] });

        foreach (var k in istek.Kitapciklar)
        {
            for (var i = 0; i < n; i++)
            {
                var no = i + 1;
                db.KitapcikSorulari.Add(new KitapcikSorusu
                {
                    SinavId = id, Kitapcik = k.Kitapcik, SoruNo = no,
                    DogruCevap = new string(k.Cevaplar[i].ToUpperInvariant().Distinct().Order().ToArray()),
                    // A kitapçığı esastır; diğer kitapçıkların A karşılığı 5. adımda girilir (soru sayısı değişmediyse korunur)
                    AnaSoruNo = k.Kitapcik == 'A' ? no : soruSayisiDegisti ? null : eskiEslestirme.GetValueOrDefault((k.Kitapcik, no)),
                });
            }
        }

        sinav.SoruSayisi = n;
        sinav.MaksimumPuan = istek.MaksimumPuan;
        sinav.KitapcikSayisi = istek.KitapcikSayisi;
        sinav.PuanlariDinamikDagit = istek.PuanlariDinamikDagit;
        sinav.BirdenFazlaDogruKabul = istek.BirdenFazlaDogruKabul;
        sinav.KitapcikEslestirmeKaydedildi = istek.KitapcikSayisi == 1 || (!soruSayisiDegisti && sinav.KitapcikEslestirmeKaydedildi);
        sinav.TamamlananAdim = Math.Max(sinav.TamamlananAdim, 2);
        sinav.GuncellemeTarihi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await Bilgi(sinav, []);
    }

    public static string? CevapAnahtariniDogrula(CevapAnahtariKaydi k)
    {
        if (k.SoruSayisi is < 1 or > 200) return "Soru sayısı 1 ile 200 arasında olmalıdır.";
        if (k.MaksimumPuan is <= 0 or > 1000) return "Maksimum puan 0'dan büyük ve en fazla 1000 olmalıdır.";
        if (k.KitapcikSayisi is < 1 or > 5) return "Kitapçık sayısı 1 ile 5 arasında olmalıdır.";

        if (!k.PuanlariDinamikDagit)
        {
            if (k.Sorular is null || k.Sorular.Count != k.SoruSayisi ||
                !k.Sorular.Select(s => s.SoruNo).Order().SequenceEqual(Enumerable.Range(1, k.SoruSayisi)))
                return "Her soru için bir puan girilmelidir.";
            if (k.Sorular.Any(s => s.Puan < 0)) return "Soru puanları negatif olamaz.";
            var toplam = k.Sorular.Sum(s => s.Puan);
            if (Math.Abs(toplam - k.MaksimumPuan) > 0.01m)
                return $"Soru puanlarının toplamı ({toplam:0.##}) maksimum puana ({k.MaksimumPuan:0.##}) eşit olmalıdır.";
        }

        var beklenen = Enumerable.Range(0, k.KitapcikSayisi).Select(i => (char)('A' + i)).ToList();
        if (k.Kitapciklar is null || !k.Kitapciklar.Select(x => x.Kitapcik).Order().SequenceEqual(beklenen))
            return $"{string.Join(", ", beklenen)} kitapçıklarının her biri için cevap anahtarı girilmelidir.";

        foreach (var kit in k.Kitapciklar.OrderBy(x => x.Kitapcik))
        {
            if (kit.Cevaplar is null || kit.Cevaplar.Count != k.SoruSayisi)
                return $"{kit.Kitapcik} kitapçığında {k.SoruSayisi} sorunun cevabı olmalıdır.";
            for (var i = 0; i < kit.Cevaplar.Count; i++)
            {
                var c = (kit.Cevaplar[i] ?? "").Trim().ToUpperInvariant();
                if (c.Length == 0) return $"{kit.Kitapcik} kitapçığının {i + 1}. sorusunun cevabı girilmemiş.";
                if (c.Any(h => !Siklar.Contains(h))) return $"{kit.Kitapcik} kitapçığının {i + 1}. sorusunda geçersiz şık var (yalnızca A-E).";
                if (c.Distinct().Count() > 1 && !k.BirdenFazlaDogruKabul)
                    return $"{kit.Kitapcik} kitapçığının {i + 1}. sorusunda birden fazla doğru şık seçilmiş. " +
                           "İptal edilen sorular için \"Birden fazla şıkkı doğru kabul et\" seçeneğini işaretleyin.";
            }
        }
        return null;
    }

    private async Task<bool> DersErisimiVar(int dersId)
    {
        var kullaniciId = User.KullaniciId();
        return await db.DersEgitmenleri.AnyAsync(e => e.DersId == dersId && e.KullaniciId == kullaniciId);
    }

    /// <summary>Kullanıcının eğitmeni olduğu derse ait sınav; değilse null.</summary>
    private async Task<Sinav?> ErisilebilirSinav(int id)
    {
        var kullaniciId = User.KullaniciId();
        return await db.Sinavlar.FirstOrDefaultAsync(s => s.Id == id && s.Ders.Egitmenler.Any(e => e.KullaniciId == kullaniciId));
    }

    private async Task<SinavBilgisi> Bilgi(Sinav s, List<string> uyarilar)
    {
        var mevcut = await db.SinavSonuclari.CountAsync(x => x.SinavId == s.Id);
        var kagit = await db.OptikKayitlari.CountAsync(x => x.SinavId == s.Id && !x.CevapAnahtariMi);
        return new SinavBilgisi(s.Id, s.DersId, s.Tur.ToString(), s.Format.ToString(), s.Durum.ToString(), s.TamamlananAdim,
            s.SoruSayisi, s.MaksimumPuan, s.KitapcikSayisi, s.PuanlariDinamikDagit, s.BirdenFazlaDogruKabul,
            s.OptikDosyaAdi, s.SinifListesiDosyaAdi, mevcut, kagit, uyarilar);
    }
}
