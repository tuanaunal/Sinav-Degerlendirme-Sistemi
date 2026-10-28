namespace Sds.Api.Degerlendirme;

public record OzetIstatistik(int Toplam, int Giren, decimal? Ortalama, decimal? Medyan, decimal? StandartSapma, decimal? EnYuksek, decimal? EnDusuk);

public static class Istatistik
{
    /// <summary>Sınava girenlerin puanlarından özet istatistik (2 ondalık). Standart sapma popülasyon formülüyle hesaplanır.</summary>
    public static OzetIstatistik Hesapla(int toplamOgrenci, IReadOnlyCollection<decimal> puanlar)
    {
        if (puanlar.Count == 0) return new OzetIstatistik(toplamOgrenci, 0, null, null, null, null, null);

        var sirali = puanlar.Order().ToList();
        var ort = sirali.Average();
        var n = sirali.Count;
        var medyan = n % 2 == 1 ? sirali[n / 2] : (sirali[n / 2 - 1] + sirali[n / 2]) / 2;
        var varyans = sirali.Sum(p => (double)((p - ort) * (p - ort))) / n;

        return new OzetIstatistik(toplamOgrenci, n, Yuvarla(ort), Yuvarla(medyan), Yuvarla((decimal)Math.Sqrt(varyans)),
            sirali[^1], sirali[0]);
    }

    private static decimal Yuvarla(decimal d) => Math.Round(d, 2, MidpointRounding.AwayFromZero);
}
