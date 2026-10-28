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
    private const string Siklar = "ABCDE";

    [HttpGet]
    public async Task<ActionResult<SinavAnaliziYaniti>> Getir(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (sinav.TamamlananAdim < 4) return BadRequest(new { mesaj = "Analiz için sınav girişinin en az not düzenleme adımına kadar tamamlanması gerekir." });

        var ders = await db.Dersler.AsNoTracking().FirstAsync(d => d.Id == sinav.DersId);
        var harfler = await db.HarfNotuAraliklari.AsNoTracking().OrderBy(h => h.Sira).ToListAsync();
        var sorular = await db.SinavSorulari.AsNoTracking().Where(s => s.SinavId == id).OrderBy(s => s.SoruNo).ToListAsync();
        var anahtarA = await db.KitapcikSorulari.AsNoTracking().Where(k => k.SinavId == id && k.Kitapcik == 'A')
            .ToDictionaryAsync(k => k.SoruNo, k => k.DogruCevap);
        var sonuclar = await db.SinavSonuclari.AsNoTracking()
            .Include(s => s.Ogrenci).Include(s => s.Eklemeler).Include(s => s.SoruCevaplari)
            .Where(s => s.SinavId == id).OrderBy(s => s.Ogrenci.OgrenciNo).ToListAsync();
        var girenler = sonuclar.Where(s => s.SinavaGirdi).ToList();

        // Puanlar raporda 100 üzerinden gösterilir
        decimal Yuzluk(SinavSonucu s) => s.DuzeltilmisPuan(sinav.MaksimumPuan) * 100 / sinav.MaksimumPuan;
        var puanlar = girenler.Select(Yuzluk).ToList();
        var ozet = Istatistik.Hesapla(sonuclar.Count, puanlar);

        // Geçme sınırı: başarısız olmayan en düşük harfin alt sınırı (Selçuk: DD = 50)
        var gecme = harfler.Where(h => h.Durum != "Başarısız").Select(h => h.AltSinir).DefaultIfEmpty(50).Min();
        decimal? basari = puanlar.Count == 0 ? null : Yuzde(puanlar.Count(p => Math.Round(p, MidpointRounding.AwayFromZero) >= gecme), puanlar.Count);

        var dagilim = Enumerable.Range(0, 10).Select(i =>
        {
            var alt = i * 10;
            var ust = i == 9 ? 100 : alt + 10;
            var sayi = puanlar.Count(p => p >= alt && (i == 9 ? p <= 100 : p < ust));
            return new DagilimAraligi($"{alt}-{ust}", alt, ust, sayi);
        }).ToList();

        var soruAnalizleri = sorular.Select(q =>
        {
            var cevaplar = girenler.Select(g => g.SoruCevaplari.FirstOrDefault(c => c.SoruNo == q.SoruNo)).Where(c => c is not null).Select(c => c!).ToList();
            var n = cevaplar.Count;
            var siklar = new Dictionary<string, decimal>();
            if (sinav.Format == SinavFormati.CoktanSecmeli)
            {
                foreach (var h in Siklar) siklar[h.ToString()] = Yuzde(cevaplar.Count(c => c.VerilenCevap == h), n);
                siklar["Çoklu"] = Yuzde(cevaplar.Count(c => c.VerilenCevap == '*'), n);
                siklar["Boş"] = Yuzde(cevaplar.Count(c => c.VerilenCevap is null), n);
            }
            var ort = n == 0 ? 0 : cevaplar.Average(c => c.Puan);
            // Çoktan seçmelide doğru oranı; açık uçluda ortalama puanın soru puanına oranı
            var oran = sinav.Format == SinavFormati.CoktanSecmeli
                ? Yuzde(cevaplar.Count(c => c.DogruMu == true), n)
                : q.Puan == 0 ? 0 : Math.Round(ort * 100 / q.Puan, 1);
            // Çoktan seçmeli: doğru / yanlış / boş. Açık uçlu: tam puan / kısmi puan / boş (DogruMu null).
            var (dogru, yanlis, bos) = sinav.Format == SinavFormati.CoktanSecmeli
                ? (cevaplar.Count(c => c.DogruMu == true), cevaplar.Count(c => c.DogruMu != true && c.VerilenCevap is not null),
                   cevaplar.Count(c => c.DogruMu != true && c.VerilenCevap is null))
                : (cevaplar.Count(c => c.DogruMu == true), cevaplar.Count(c => c.DogruMu == false), cevaplar.Count(c => c.DogruMu is null));
            return new SoruAnalizi(q.SoruNo, q.Puan, anahtarA.GetValueOrDefault(q.SoruNo), oran, Math.Round(ort, 2),
                dogru, yanlis, bos, siklar);
        }).ToList();

        // Öğrenme çıktısı başarısı = ilişkili sorulardan alınan toplam puan / alınabilecek toplam puan
        var ciktiEslesme = await db.SoruOgrenmeCiktilari.AsNoTracking().Where(s => s.SinavId == id).ToListAsync();
        var ciktilar = await db.OgrenmeCiktilari.AsNoTracking().Where(c => c.DersId == sinav.DersId).OrderBy(c => c.SiraNo).ToListAsync();
        var ciktiAnalizi = ciktilar.Select(c =>
        {
            var no = ciktiEslesme.Where(e => e.OgrenmeCiktisiId == c.Id).Select(e => e.SoruNo).Order().ToList();
            if (no.Count == 0 || girenler.Count == 0) return new CiktiAnalizi(c.SiraNo, c.Aciklama, no, null);
            var alinan = girenler.Sum(g => g.SoruCevaplari.Where(x => no.Contains(x.SoruNo)).Sum(x => x.Puan));
            var alinabilir = sorular.Where(q => no.Contains(q.SoruNo)).Sum(q => q.Puan) * girenler.Count;
            return new CiktiAnalizi(c.SiraNo, c.Aciklama, no, alinabilir == 0 ? 0 : Math.Round(alinan * 100 / alinabilir, 1));
        }).Where(c => c.Sorular.Count > 0).ToList();

        var harfDagilimi = harfler.Select(h => new HarfSayisi(h.Harf, h.Durum,
            girenler.Count(g => HarfNotu.Bul(Yuzluk(g), harfler)?.Harf == h.Harf))).ToList();

        var genel = new AnalizGenelBilgi(ders.Kod, ders.Ad, ders.AkademikYil, ders.Donem == Donem.Guz ? "Güz" : "Bahar",
            sinav.Tur.ToString(), sinav.Format.ToString(), sinav.SoruSayisi, sinav.MaksimumPuan, sinav.KitapcikSayisi, sinav.Tarih);

        return new SinavAnaliziYaniti(genel, ozet, basari, gecme, dagilim, soruAnalizleri, ciktiAnalizi, harfDagilimi,
            sonuclar.Select(s => new OgrenciSecenegi(s.Id, s.Ogrenci.OgrenciNo, s.Ogrenci.AdSoyad, s.SinavaGirdi)).ToList());
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

    private static decimal Yuzde(int pay, int payda) => payda == 0 ? 0 : Math.Round(pay * 100m / payda, 1);
}
