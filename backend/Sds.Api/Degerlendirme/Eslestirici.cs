using Sds.Api.Dosyalar;

namespace Sds.Api.Degerlendirme;

public record EslestirmeOgrencisi(int Id, string OgrenciNo, string AdSoyad);
public record EslestirmeKagidi(int Id, string OgrenciNo, string AdSoyad);

/// <summary>Otomatik eşleşme. NumaraFarkli: optikteki numara listedekinden bir hane farklı (yanlış kodlama).</summary>
public record Eslesme(int KagitId, int OgrenciId, bool NumaraFarkli);

/// <summary>
/// Optik kağıtlarını sınıf listesiyle eşleştirir (3. adım).
/// 1) Numarası birebir aynı olanlar. 2) Numarası tek hane farklı (bir hane yanlış kodlanmış) ve
/// bu farkla eşleşebilecek tek bir öğrenci varsa. İsimden eşleştirme otomatik yapılmaz, öneri olarak sunulur.
/// </summary>
public static class Eslestirici
{
    public static List<Eslesme> Eslestir(IReadOnlyList<EslestirmeOgrencisi> ogrenciler, IReadOnlyList<EslestirmeKagidi> kagitlar)
    {
        var sonuc = new List<Eslesme>();
        var bosOgrenciler = ogrenciler.ToDictionary(o => o.Id);
        var bosKagitlar = kagitlar.Where(k => k.OgrenciNo.Length > 0).ToList();

        // 1) Birebir numara (aynı numarayla birden fazla kağıt varsa hiçbiri otomatik eşleşmez)
        foreach (var grup in bosKagitlar.GroupBy(k => k.OgrenciNo).Where(g => g.Count() == 1))
        {
            var k = grup.Single();
            var o = bosOgrenciler.Values.FirstOrDefault(x => x.OgrenciNo == k.OgrenciNo);
            if (o is null) continue;
            sonuc.Add(new Eslesme(k.Id, o.Id, false));
            bosOgrenciler.Remove(o.Id);
        }
        var eslesenKagitlar = sonuc.Select(e => e.KagitId).ToHashSet();
        bosKagitlar.RemoveAll(k => eslesenKagitlar.Contains(k.Id));

        // 2) Tek hane farkı: hem kağıdın hem öğrencinin tek adayı birbiri olmalı
        var adaylar = bosKagitlar
            .Select(k => (Kagit: k, Ogrenciler: bosOgrenciler.Values.Where(o => TekHaneFarkli(k.OgrenciNo, o.OgrenciNo)).ToList()))
            .Where(x => x.Ogrenciler.Count == 1)
            .ToList();
        foreach (var (kagit, liste) in adaylar)
        {
            var o = liste[0];
            if (!bosOgrenciler.ContainsKey(o.Id)) continue;
            if (adaylar.Count(x => x.Ogrenciler[0].Id == o.Id) > 1) continue; // iki kağıt aynı öğrenciye benziyor
            sonuc.Add(new Eslesme(kagit.Id, o.Id, true));
            bosOgrenciler.Remove(o.Id);
        }

        return sonuc;
    }

    /// <summary>Eşleşmeyen kağıt için isim benzerliğine göre en olası öğrenci (yoksa null).</summary>
    public static EslestirmeOgrencisi? Oner(EslestirmeKagidi kagit, IEnumerable<EslestirmeOgrencisi> bosOgrenciler)
    {
        var ad = Metin.Anahtar(kagit.AdSoyad);
        if (ad.Length < 3) return null;
        return bosOgrenciler
            .Select(o => (Ogrenci: o, Mesafe: Levenshtein(ad, Metin.Anahtar(o.AdSoyad))))
            .Where(x => x.Mesafe <= Math.Max(1, ad.Length / 6)) // küçük yazım farklarına izin ver
            .OrderBy(x => x.Mesafe)
            .Select(x => x.Ogrenci)
            .FirstOrDefault();
    }

    public static bool TekHaneFarkli(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0 || a == b) return false;
        if (a.Length == b.Length) return a.Zip(b).Count(p => p.First != p.Second) == 1;
        // Bir hane eksik / fazla kodlanmış
        return Math.Abs(a.Length - b.Length) == 1 && Levenshtein(a, b) == 1;
    }

    public static int Levenshtein(string a, string b)
    {
        var onceki = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var simdiki = new int[b.Length + 1];
            simdiki[0] = i;
            for (var j = 1; j <= b.Length; j++)
                simdiki[j] = Math.Min(Math.Min(simdiki[j - 1] + 1, onceki[j] + 1), onceki[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            onceki = simdiki;
        }
        return onceki[b.Length];
    }
}
