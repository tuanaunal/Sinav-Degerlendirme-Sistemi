using Microsoft.EntityFrameworkCore;
using Sds.Api.Controllers;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Api.Degerlendirme;

/// <summary>Analiz raporu ve resmi formun ortak hesaplaması; iki çıktı da aynı sayıları gösterir.</summary>
public static class SinavAnalizServisi
{
    private const string Siklar = "ABCDE";

    public static async Task<SinavAnaliziYaniti> HesaplaAsync(SdsDbContext db, Sinav sinav)
    {
        var id = sinav.Id;
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

    public static decimal Yuzde(int pay, int payda) => payda == 0 ? 0 : Math.Round(pay * 100m / payda, 1);
}
