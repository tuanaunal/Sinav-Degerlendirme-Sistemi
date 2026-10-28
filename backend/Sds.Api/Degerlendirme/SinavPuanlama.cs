using Microsoft.EntityFrameworkCore;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Api.Degerlendirme;

public static class SinavPuanlama
{
    /// <summary>
    /// Sınava giren (eşleşmiş) tüm öğrencilerin doğru/yanlış/boş sayılarını, ham puanlarını ve soru bazlı cevaplarını
    /// yeniden hesaplar. Cevap anahtarı veya kitapçık soru eşleştirmesi değiştiğinde çağrılır; SaveChanges çağıran tarafa aittir.
    /// </summary>
    public static async Task YenidenHesaplaAsync(SdsDbContext db, Sinav sinav)
    {
        var anahtar = await db.KitapcikSorulari.Where(k => k.SinavId == sinav.Id).ToListAsync();
        var sorular = await db.SinavSorulari.Where(s => s.SinavId == sinav.Id).ToListAsync();
        var sonuclar = await db.SinavSonuclari
            .Where(s => s.SinavId == sinav.Id)
            .Include(s => s.SoruCevaplari)
            .ToListAsync();

        foreach (var s in sonuclar)
        {
            db.SoruCevaplari.RemoveRange(s.SoruCevaplari);
            s.SoruCevaplari.Clear();

            if (!s.SinavaGirdi || s.OptikCevaplar is null)
            {
                (s.DogruSayisi, s.YanlisSayisi, s.BosSayisi, s.HamPuan) = (0, 0, 0, 0);
                continue;
            }

            var kitapcik = sinav.KitapcikSayisi == 1 ? 'A' : s.Kitapcik ?? 'A';
            var p = PuanHesaplayici.Hesapla(s.OptikCevaplar, kitapcik, anahtar, sorular);
            (s.DogruSayisi, s.YanlisSayisi, s.BosSayisi, s.HamPuan) = (p.Dogru, p.Yanlis, p.Bos, p.HamPuan);
            foreach (var q in p.Sorular)
                s.SoruCevaplari.Add(new SoruCevabi { SoruNo = q.SoruNo, VerilenCevap = q.VerilenCevap, DogruMu = q.DogruMu, Puan = q.Puan });
        }
    }
}

public static class HarfNotu
{
    /// <summary>Puan tam sayıya yuvarlanır (49,5 → 50) ve harf notu aralığından bulunur.</summary>
    public static HarfNotuAraligi? Bul(decimal puan, IEnumerable<HarfNotuAraligi> araliklar)
    {
        var p = (int)Math.Round(puan, 0, MidpointRounding.AwayFromZero);
        return araliklar.FirstOrDefault(a => p >= a.AltSinir && p <= a.UstSinir);
    }
}

public static class SinavErisimi
{
    /// <summary>Kullanıcının eğitmeni olduğu derse ait sınav; değilse null.</summary>
    public static Task<Sinav?> ErisilebilirSinavAsync(this SdsDbContext db, int sinavId, int kullaniciId) =>
        db.Sinavlar.FirstOrDefaultAsync(s => s.Id == sinavId && s.Ders.Egitmenler.Any(e => e.KullaniciId == kullaniciId));
}
