using Microsoft.EntityFrameworkCore;
using Sds.Api.Controllers;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Api.Degerlendirme;

/// <summary>
/// Dönem sonu notu ve Dönem Analiz Raporu. Kurallar (Selçuk Üniversitesi Önlisans ve Lisans Eğitim-Öğretim Yönetmeliği):
/// dönem notu = vize × vize ağırlığı + etkin sınav × final ağırlığı; bütünlemeye giren öğrencide etkin sınav bütünlemedir.
/// Etkin sınavdan final alt sınırının altında puan alan veya final/bütünlemeye girmeyen öğrenci FF alır.
/// </summary>
public static class DonemAnalizServisi
{
    public const string AgirlikHatasi = "Notlandırma yönetmeliğinde vize ve final ağırlıklarının toplamı %100 olmalıdır.";

    public static async Task<DonemAnaliziYaniti> HesaplaAsync(SdsDbContext db, Ders ders)
    {
        var ayar = await db.NotlandirmaAyarlari.AsNoTracking().FirstAsync();
        if (ayar.VizeAgirligi + ayar.FinalAgirligi != 100) throw new InvalidOperationException(AgirlikHatasi);
        var harfler = await db.HarfNotuAraliklari.AsNoTracking().OrderBy(h => h.Sira).ToListAsync();
        var sinavlar = await db.Sinavlar.AsNoTracking().Where(s => s.DersId == ders.Id && s.TamamlananAdim >= 4).OrderBy(s => s.Tur).ToListAsync();
        var ogretimUyeleri = (await db.DersEgitmenleri.AsNoTracking().Include(e => e.Kullanici).Where(e => e.DersId == ders.Id).ToListAsync())
            .Select(e => e.Kullanici.TamAd).Order().ToList();

        // Öğrencinin her sınavdaki 100'lük puanı (girmediyse null)
        var puanlar = new Dictionary<SinavTuru, Dictionary<int, decimal?>>();
        var ogrenciler = new Dictionary<int, Ogrenci>();
        foreach (var sinav in sinavlar)
        {
            var sonuclar = await db.SinavSonuclari.AsNoTracking().Include(s => s.Ogrenci).Include(s => s.Eklemeler)
                .Where(s => s.SinavId == sinav.Id).ToListAsync();
            puanlar[sinav.Tur] = sonuclar.ToDictionary(s => s.OgrenciId,
                s => s.SinavaGirdi ? Math.Round(s.DuzeltilmisPuan(sinav.MaksimumPuan) * 100 / sinav.MaksimumPuan, 2) : (decimal?)null);
            foreach (var s in sonuclar) ogrenciler.TryAdd(s.OgrenciId, s.Ogrenci);
        }
        decimal? Puan(SinavTuru tur, int ogrenciId) => puanlar.GetValueOrDefault(tur)?.GetValueOrDefault(ogrenciId);
        bool Listede(SinavTuru tur, int ogrenciId) => puanlar.GetValueOrDefault(tur)?.ContainsKey(ogrenciId) == true;

        var ff = harfler.Last();
        var satirlar = ogrenciler.Values.OrderBy(o => o.OgrenciNo).Select(o =>
        {
            var vize = Puan(SinavTuru.Vize, o.Id);
            var final = Puan(SinavTuru.Final, o.Id);
            var but = Puan(SinavTuru.Butunleme, o.Id);
            var (etkinAd, etkin) = but is not null ? ("Bütünleme", but) : final is not null ? ("Final", final) : ((string?)null, (decimal?)null);
            var agirlikli = Math.Round(((vize ?? 0) * ayar.VizeAgirligi + (etkin ?? 0) * ayar.FinalAgirligi) / 100, 2, MidpointRounding.AwayFromZero);

            HarfNotuAraligi harf;
            string? aciklama = null;
            if (etkin is null)
            {
                harf = ff;
                aciklama = Listede(SinavTuru.Butunleme, o.Id) ? "Final ve bütünlemeye girmedi" : "Finale girmedi";
            }
            else if (etkin < ayar.FinalAltSiniri)
            {
                harf = ff;
                aciklama = $"{etkinAd} puanı alt sınırın ({ayar.FinalAltSiniri}) altında";
            }
            else harf = HarfNotu.Bul(agirlikli, harfler) ?? ff;
            if (vize is null) aciklama = aciklama is null ? "Vizeye girmedi" : $"Vizeye girmedi; {char.ToLower(aciklama[0])}{aciklama[1..]}";

            return new DonemOgrencisi(o.OgrenciNo, o.AdSoyad, vize, final, but, Listede(SinavTuru.Butunleme, o.Id), etkinAd,
                agirlikli, harf.Harf, harf.Durum, aciklama);
        }).ToList();

        var agirliklar = satirlar.Select(s => s.Agirlikli).ToList();
        var istatistik = Istatistik.Hesapla(satirlar.Count, agirliklar);
        int Say(string durum) => satirlar.Count(s => s.Durum == durum);
        var basarisiz = satirlar.Count(s => s.Harf == ff.Harf);
        var ozet = new DonemOzeti(satirlar.Count, istatistik.Ortalama, istatistik.Medyan, istatistik.StandartSapma,
            istatistik.EnDusuk, istatistik.EnYuksek,
            satirlar.Count == 0 ? null : SinavAnalizServisi.Yuzde(satirlar.Count - basarisiz, satirlar.Count),
            Say("Başarılı"), Say("Şartlı Geçer"), basarisiz,
            harfler.Count == 0 ? null : Math.Round(satirlar.Average(s => harfler.First(h => h.Harf == s.Harf).Katsayi), 2));

        // Sınav istatistikleri ve öğrenme çıktıları: her sınavın kendi analizinden
        var sinavIstatistikleri = new List<DonemSinavIstatistigi>();
        var ciktiBasarilari = new Dictionary<int, (string Aciklama, List<CiktiSinavBasarisi> Basarilar)>();
        foreach (var sinav in sinavlar)
        {
            var a = await SinavAnalizServisi.HesaplaAsync(db, sinav);
            var ad = SinavAdi(sinav.Tur);
            sinavIstatistikleri.Add(new DonemSinavIstatistigi(ad, a.Genel.Format, sinav.Tarih, sinav.SoruSayisi, a.Ozet.Toplam, a.Ozet.Giren,
                a.Ozet.Ortalama, a.Ozet.Medyan, a.Ozet.StandartSapma, a.Ozet.EnDusuk, a.Ozet.EnYuksek, a.BasariOrani));
            foreach (var c in a.Ciktilar)
            {
                if (!ciktiBasarilari.TryGetValue(c.SiraNo, out var k)) ciktiBasarilari[c.SiraNo] = k = (c.Aciklama, []);
                k.Basarilar.Add(new CiktiSinavBasarisi(ad, c.Sorular.Count, c.BasariOrani));
            }
        }
        var ciktilar = await db.OgrenmeCiktilari.AsNoTracking().Where(c => c.DersId == ders.Id).OrderBy(c => c.SiraNo).ToListAsync();
        var donemCiktilari = ciktilar.Select(c =>
        {
            var b = ciktiBasarilari.TryGetValue(c.SiraNo, out var k) ? k.Basarilar : [];
            var olculen = b.Where(x => x.Basari.HasValue).ToList();
            return new DonemCiktisi(c.SiraNo, c.Aciklama, b, olculen.Count == 0 ? null : Math.Round(olculen.Average(x => x.Basari!.Value), 1));
        }).ToList();

        var harfDagilimi = harfler.Select(h => new HarfSayisi(h.Harf, h.Durum, satirlar.Count(s => s.Harf == h.Harf))).ToList();
        var puanDagilimi = Enumerable.Range(0, 10).Select(i =>
        {
            var alt = i * 10;
            var ust = i == 9 ? 100 : alt + 10;
            return new DagilimAraligi($"{alt}-{ust}", alt, ust, agirliklar.Count(p => p >= alt && (i == 9 ? p <= 100 : p < ust)));
        }).ToList();

        var genel = new DonemGenelBilgi(ders.Kod, ders.Ad, ders.AkademikYil, ders.Donem.ToString(), ders.OgretimTuru.ToString(), ders.Sinif,
            ogretimUyeleri, ayar.VizeAgirligi, ayar.FinalAgirligi, ayar.FinalAltSiniri,
            harfler.Where(h => h.Durum != "Başarısız").Select(h => h.AltSinir).DefaultIfEmpty(50).Min());
        return new DonemAnaliziYaniti(genel, ozet, sinavIstatistikleri, harfDagilimi, puanDagilimi, satirlar, donemCiktilari);
    }

    public static string SinavAdi(SinavTuru tur) => tur switch { SinavTuru.Vize => "Vize", SinavTuru.Final => "Final", _ => "Bütünleme" };
}
