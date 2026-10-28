using System.Globalization;
using System.Text;

namespace Sds.Api.Dosyalar;

public static class Metin
{
    private static readonly CultureInfo Tr = new("tr-TR");

    /// <summary>
    /// Başlık ve isim karşılaştırması için anahtar: Türkçe küçük harf, Türkçe karakterler sadeleştirilmiş,
    /// harf/rakam dışı karakterler atılmış. "Öğrenci No" → "ogrencino", "ADI SOYADI" → "adisoyadi".
    /// </summary>
    public static string Anahtar(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.ToLower(Tr))
        {
            var d = c switch
            {
                'ı' or 'i' or 'î' => 'i',
                'ş' => 's',
                'ğ' => 'g',
                'ü' or 'û' => 'u',
                'ö' => 'o',
                'ç' => 'c',
                'â' => 'a',
                _ => c,
            };
            if (char.IsLetterOrDigit(d)) sb.Append(d);
        }
        return sb.ToString();
    }

    /// <summary>Ad soyadı tek boşluklu, Türkçe büyük harfli hâle getirir: " ayşe  naz yalçın" → "AYŞE NAZ YALÇIN".</summary>
    public static string AdSoyad(string? s) =>
        string.Join(' ', (s ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToUpper(Tr);
}
