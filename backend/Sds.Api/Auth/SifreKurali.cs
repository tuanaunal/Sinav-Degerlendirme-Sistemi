namespace Sds.Api.Auth;

/// <summary>Kullanıcının belirlediği yeni şifrenin uyması gereken kurallar.</summary>
public static class SifreKurali
{
    public const int EnAzUzunluk = 8;

    /// <returns>Kurala uymuyorsa hata mesajı, uyuyorsa null.</returns>
    public static string? Dogrula(string sifre)
    {
        if (string.IsNullOrWhiteSpace(sifre) || sifre.Length < EnAzUzunluk)
            return $"Şifre en az {EnAzUzunluk} karakter olmalıdır.";
        if (!sifre.Any(char.IsLetter))
            return "Şifre en az bir harf içermelidir.";
        if (!sifre.Any(char.IsDigit))
            return "Şifre en az bir rakam içermelidir.";
        return null;
    }
}
