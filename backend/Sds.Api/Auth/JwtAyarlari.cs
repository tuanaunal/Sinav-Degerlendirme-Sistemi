namespace Sds.Api.Auth;

public class JwtAyarlari
{
    public string Anahtar { get; set; } = "";
    public string Yayinci { get; set; } = "Sds.Api";
    public string Hedef { get; set; } = "Sds.Frontend";
    public int SureDakika { get; set; } = 480;
}

/// <summary>JWT içine yazılan özel claim adları ve yetki politikaları.</summary>
public static class Yetki
{
    public const string ClaimYonetici = "yonetici";
    public const string ClaimSifreDegistirmeli = "sifre_degistirmeli";

    /// <summary>Giriş yapmış ve şifresini değiştirmiş kullanıcı (varsayılan politika)</summary>
    public const string Aktif = "Aktif";
    /// <summary>Sadece giriş yapmış olmak yeterli (şifre değiştirme ekranı için)</summary>
    public const string GirisYapmis = "GirisYapmis";
    /// <summary>Admin veya yönetici yetkisi verilmiş öğretim üyesi</summary>
    public const string Yonetici = "Yonetici";
    /// <summary>Dersleri olan öğretim üyesi</summary>
    public const string OgretimUyesi = "OgretimUyesi";
}
