namespace Sds.Api.Domain;

public static class AkademikTakvim
{
    /// <summary>
    /// Verilen tarihin düştüğü akademik yıl. Eylül ve sonrası yeni yılın başıdır:
    /// 28.10.2026 → "2026-2027", 15.03.2027 → "2026-2027".
    /// </summary>
    public static string AkademikYil(DateTime tarih)
    {
        var baslangic = tarih.Month >= 9 ? tarih.Year : tarih.Year - 1;
        return $"{baslangic}-{baslangic + 1}";
    }

    /// <summary>Eylül-Ocak güz, Şubat-Ağustos bahar dönemidir.</summary>
    public static Donem Donem(DateTime tarih) => tarih.Month is >= 9 or 1 ? Domain.Donem.Guz : Domain.Donem.Bahar;

    /// <summary>"2026-2027" biçiminde ve ardışık iki yıl mı?</summary>
    public static bool GecerliMi(string? yil)
    {
        if (yil is null || yil.Length != 9 || yil[4] != '-') return false;
        return int.TryParse(yil[..4], out var a) && int.TryParse(yil[5..], out var b) && b == a + 1 && a is >= 2000 and < 2100;
    }
}
