namespace Sds.Api.Domain;

public static class PuanDagitimi
{
    /// <summary>
    /// "Puanları dinamik dağıt": maksimum puanı sorulara eşit böler (2 ondalık). Yuvarlamadan kalan fark
    /// son sorulara 0,01'er eklenir; toplam her zaman maksimum puana eşittir. 100 / 25 → 4; 100 / 30 → 3,33 × 20 + 3,34 × 10.
    /// </summary>
    public static decimal[] EsitDagit(decimal maksimum, int soruSayisi)
    {
        if (soruSayisi <= 0) return [];
        var taban = Math.Floor(maksimum / soruSayisi * 100) / 100;
        var puanlar = Enumerable.Repeat(taban, soruSayisi).ToArray();
        var kalanKurus = (int)Math.Round((maksimum - taban * soruSayisi) * 100);
        for (var i = 0; i < kalanKurus; i++) puanlar[soruSayisi - 1 - i] += 0.01m;
        return puanlar;
    }
}
