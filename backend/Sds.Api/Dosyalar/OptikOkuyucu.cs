using System.Text;
using Sds.Api.Domain;

namespace Sds.Api.Dosyalar;

/// <summary>Optik dosyadaki bir satır.</summary>
public record OptikSatir(
    int SatirNo,
    string OgrenciNo,
    string AdSoyad,
    char? Kitapcik,
    char? SinavTuru,
    string Cevaplar,
    bool CevapAnahtariMi);

public record OptikOkumaSonucu(
    string Kodlama,
    List<OptikSatir> Satirlar,
    List<string> Uyarilar)
{
    public IEnumerable<OptikSatir> Ogrenciler => Satirlar.Where(s => !s.CevapAnahtariMi);
    public IEnumerable<OptikSatir> CevapAnahtarlari => Satirlar.Where(s => s.CevapAnahtariMi);

    /// <summary>En uzun cevap dizisi; cevap anahtarı girilmeden önce soru sayısı tahmini olarak kullanılır.</summary>
    public int TahminiSoruSayisi => Satirlar.Count == 0 ? 0 : Satirlar.Max(s => s.Cevaplar.TrimEnd().Length);
}

public class DosyaOkumaHatasi(string mesaj) : Exception(mesaj);

/// <summary>
/// Optik okuyucunun ürettiği sabit genişlikli metin dosyasını, yöneticinin tanımladığı alan indislerine göre ayrıştırır.
/// Düzen değişirse kod değil yalnızca Optik Form Ayarları güncellenir.
/// </summary>
public static class OptikOkuyucu
{
    static OptikOkuyucu() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static OptikOkumaSonucu Oku(byte[] icerik, IReadOnlyCollection<OptikFormAlani> alanlar)
    {
        if (icerik.Length == 0) throw new DosyaOkumaHatasi("Optik dosyası boş.");
        if (icerik.Take(4096).Contains((byte)0))
            throw new DosyaOkumaHatasi("Bu dosya bir metin dosyası değil. Optik okuyucunun ürettiği .txt dosyasını yükleyin.");

        var (metin, kodlama) = Coz(icerik);

        Aralik Al(OptikAlan a) => alanlar.FirstOrDefault(x => x.Alan == a) is { } x
            ? new Aralik(x.Baslangic, x.Bitis)
            : throw new DosyaOkumaHatasi($"Optik form ayarlarında {a} alanı tanımlı değil.");
        var no = Al(OptikAlan.OgrenciNo);
        var ad = Al(OptikAlan.AdSoyad);
        var kitapcik = Al(OptikAlan.KitapcikTuru);
        var tur = Al(OptikAlan.SinavTuru);
        var cevap = Al(OptikAlan.Cevaplar);

        var satirlar = new List<OptikSatir>();
        var uyarilar = new List<string>();
        var satirNo = 0;
        foreach (var hamSatir in metin.Split('\n'))
        {
            satirNo++;
            var satir = hamSatir.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(satir)) continue;

            if (satir.Length <= cevap.Baslangic)
            {
                uyarilar.Add($"{satirNo}. satır cevap alanına ulaşmayacak kadar kısa ({satir.Length} karakter); atlandı.");
                continue;
            }

            var ogrenciNo = no.Kes(satir).Trim();
            var adSoyad = Metin.AdSoyad(ad.Kes(satir));
            // Öğretim üyesinin optiğe okuttuğu cevap anahtarı kağıdı: numara sıfırlarla ya da ad "CEVAP ANAHTARI"
            var anahtarMi = (ogrenciNo.Length > 0 && ogrenciNo.All(c => c == '0')) ||
                            Metin.Anahtar(adSoyad).StartsWith("cevapanahtari");

            satirlar.Add(new OptikSatir(
                satirNo,
                ogrenciNo,
                adSoyad,
                Karakter(kitapcik.Kes(satir)),
                Karakter(tur.Kes(satir)),
                cevap.Kes(satir).ToUpperInvariant(),
                anahtarMi));

            if (!anahtarMi && ogrenciNo.Length > 0 && !ogrenciNo.All(char.IsDigit))
                uyarilar.Add($"{satirNo}. satırdaki öğrenci numarası rakam dışı karakter içeriyor: \"{ogrenciNo}\".");
        }

        if (satirlar.Count == 0)
            throw new DosyaOkumaHatasi("Optik dosyasında okunabilir satır bulunamadı. Optik form ayarlarını kontrol edin.");
        if (!satirlar.Any(s => !s.CevapAnahtariMi))
            throw new DosyaOkumaHatasi("Optik dosyasında öğrenci satırı bulunamadı.");

        var bosNo = satirlar.Count(s => !s.CevapAnahtariMi && s.OgrenciNo.Length == 0);
        if (bosNo > 0) uyarilar.Add($"{bosNo} kağıtta öğrenci numarası kodlanmamış; eşleştirme adımında elle eşleştirilebilir.");
        var bosKitapcik = satirlar.Count(s => !s.CevapAnahtariMi && s.Kitapcik is null);
        if (bosKitapcik > 0) uyarilar.Add($"{bosKitapcik} kağıtta kitapçık türü boş bırakılmış.");

        return new OptikOkumaSonucu(kodlama, satirlar, uyarilar);
    }

    /// <summary>
    /// Türkçe karakterlerin doğru okunması için kodlama tespiti: geçerli UTF-8 ise UTF-8,
    /// değilse optik okuyucuların varsayılanı Windows-1254 (Türkçe).
    /// </summary>
    private static (string Metin, string Kodlama) Coz(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF)
            return (Encoding.UTF8.GetString(b, 3, b.Length - 3), "UTF-8");
        try
        {
            var utf8 = new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(b);
            return (utf8, b.Any(x => x >= 0x80) ? "UTF-8" : "ASCII");
        }
        catch (DecoderFallbackException)
        {
            return (Encoding.GetEncoding(1254).GetString(b), "Windows-1254 (Türkçe)");
        }
    }

    private static char? Karakter(string s)
    {
        var t = s.Trim().ToUpperInvariant();
        return t.Length == 0 ? null : t[0];
    }

    /// <summary>0 tabanlı, bitiş dahil aralık; Bitiş null ise satır sonuna kadar.</summary>
    private readonly record struct Aralik(int Baslangic, int? Bitis)
    {
        public string Kes(string satir)
        {
            if (Baslangic >= satir.Length) return "";
            var son = Bitis is null ? satir.Length - 1 : Math.Min(Bitis.Value, satir.Length - 1);
            return satir.Substring(Baslangic, son - Baslangic + 1);
        }
    }
}
