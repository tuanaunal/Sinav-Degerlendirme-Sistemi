using System.Globalization;
using ClosedXML.Excel;

namespace Sds.Api.Dosyalar;

public record SinifListesiOgrencisi(int SatirNo, string OgrenciNo, string AdSoyad);

/// <summary>DersKodu: OBİS listesinin üst bilgisindeki "Ders: 3304701 - ..." satırından (yoksa null).</summary>
public record SinifListesiSonucu(List<SinifListesiOgrencisi> Ogrenciler, List<string> Uyarilar, string? DersKodu = null);

public record NotSatiri(int SatirNo, string OgrenciNo, string AdSoyad, Dictionary<string, decimal?> Degerler);

public record NotTablosuSonucu(List<string> Sutunlar, List<NotSatiri> Satirlar, List<string> Uyarilar);

/// <summary>
/// OBİS sınıf listesi ve öğretim üyesinin hazırladığı not tablolarını (.xlsx) okur.
/// Başlık satırı ilk 30 satır içinde aranır; OBİS'in üstteki ders bilgisi satırları atlanır.
/// </summary>
public static class ExcelOkuyucu
{
    private const int BaslikAramaSiniri = 30;

    // Başlık hücreleri Metin.Anahtar ile sadeleştirilip bu listelerle karşılaştırılır
    private static readonly string[] NoBasliklari = ["ogrencino", "ogrencinumarasi", "ogrno", "numara", "ogrencinumara", "okulno", "no"];
    private static readonly string[] AdSoyadBasliklari = ["adsoyad", "adisoyadi", "adsoyadi", "adisoyad", "ogrenciadisoyadi", "isimsoyisim", "adsoyisim"];
    private static readonly string[] AdBasliklari = ["adi", "ad", "isim", "ogrenciadi"];
    private static readonly string[] SoyadBasliklari = ["soyadi", "soyad", "soyisim", "ogrencisoyadi"];

    public static SinifListesiSonucu SinifListesiOku(Stream dosya)
    {
        using var wb = Ac(dosya);
        var ws = wb.Worksheets.First();
        var (satir, sutunlar) = BaslikBul(ws)
            ?? throw new DosyaOkumaHatasi(
                "Sınıf listesinde başlık satırı bulunamadı. Listede \"Öğrenci No\" ile \"Adı\" + \"Soyadı\" (veya \"Ad Soyad\") sütunları olmalıdır.");

        var noSutun = sutunlar.First(s => NoBasliklari.Contains(s.Anahtar)).Sutun;
        var adSoyadSutun = sutunlar.FirstOrDefault(s => AdSoyadBasliklari.Contains(s.Anahtar)).Sutun;
        var adSutun = sutunlar.FirstOrDefault(s => AdBasliklari.Contains(s.Anahtar)).Sutun;
        var soyadSutun = sutunlar.FirstOrDefault(s => SoyadBasliklari.Contains(s.Anahtar)).Sutun;

        var ogrenciler = new List<SinifListesiOgrencisi>();
        var uyarilar = new List<string>();
        var gorulen = new HashSet<string>();
        var sonSatir = ws.LastRowUsed()?.RowNumber() ?? satir;

        for (var r = satir + 1; r <= sonSatir; r++)
        {
            var no = HucreMetni(ws.Cell(r, noSutun)).Replace(" ", "");
            var adSoyad = adSoyadSutun > 0
                ? Metin.AdSoyad(HucreMetni(ws.Cell(r, adSoyadSutun)))
                : Metin.AdSoyad($"{HucreMetni(ws.Cell(r, adSutun))} {HucreMetni(ws.Cell(r, soyadSutun))}");

            // Boş satırlar ve "Toplam öğrenci sayısı: 120" gibi alt bilgi satırları
            if (no.Length == 0)
            {
                if (adSoyad.Length > 0) uyarilar.Add($"{r}. satırdaki {adSoyad} için öğrenci numarası boş; atlandı.");
                continue;
            }
            if (!no.All(char.IsDigit))
            {
                uyarilar.Add($"{r}. satırdaki öğrenci numarası geçersiz (\"{no}\"); atlandı.");
                continue;
            }
            if (!gorulen.Add(no))
            {
                uyarilar.Add($"{no} numaralı öğrenci listede birden fazla kez geçiyor; ilk kaydı kullanıldı.");
                continue;
            }
            if (adSoyad.Length == 0) uyarilar.Add($"{no} numaralı öğrencinin adı boş.");
            ogrenciler.Add(new SinifListesiOgrencisi(r, no, adSoyad));
        }

        if (ogrenciler.Count == 0) throw new DosyaOkumaHatasi("Sınıf listesinde öğrenci bulunamadı.");
        return new SinifListesiSonucu(ogrenciler, uyarilar, DersKoduBul(ws, satir));
    }

    /// <summary>Başlık satırının üstündeki "Ders | 3304701 - DERS ADI" bilgisinden ders kodu.</summary>
    private static string? DersKoduBul(IXLWorksheet ws, int baslikSatiri)
    {
        for (var r = 1; r < baslikSatiri; r++)
        {
            if (Metin.Anahtar(HucreMetni(ws.Cell(r, 1))) != Metin.Anahtar("Ders")) continue;
            var m = System.Text.RegularExpressions.Regex.Match(HucreMetni(ws.Cell(r, 2)), @"^\s*(\d{5,10})\s*-");
            if (m.Success) return m.Groups[1].Value;
        }
        return null;
    }

    /// <summary>
    /// Öğrenci numarası + ad soyad + sayısal sütunlar (SINAVNOTU veya S1…Sn) içeren tabloyu okur.
    /// Boş hücre null olarak döner (öğrenci sınava girmemiş).
    /// </summary>
    public static NotTablosuSonucu NotTablosuOku(Stream dosya)
    {
        using var wb = Ac(dosya);
        var ws = wb.Worksheets.First();
        var (satir, sutunlar) = BaslikBul(ws, adSoyadZorunlu: false)
            ?? throw new DosyaOkumaHatasi("Not dosyasında \"OGRENCINO\" başlıklı sütun bulunamadı.");

        var noSutun = sutunlar.First(s => NoBasliklari.Contains(s.Anahtar)).Sutun;
        var adSutun = sutunlar.FirstOrDefault(s => AdSoyadBasliklari.Contains(s.Anahtar)).Sutun;
        var degerSutunlari = sutunlar
            .Where(s => s.Sutun != noSutun && s.Sutun != adSutun && !AdBasliklari.Contains(s.Anahtar) &&
                        !SoyadBasliklari.Contains(s.Anahtar) && s.Anahtar is not ("sirano" or "sira" or "sinif"))
            .ToList();
        if (degerSutunlari.Count == 0) throw new DosyaOkumaHatasi("Not dosyasında puan sütunu bulunamadı.");

        var satirlar = new List<NotSatiri>();
        var uyarilar = new List<string>();
        var sonSatir = ws.LastRowUsed()?.RowNumber() ?? satir;
        for (var r = satir + 1; r <= sonSatir; r++)
        {
            var no = HucreMetni(ws.Cell(r, noSutun)).Replace(" ", "");
            if (no.Length == 0) continue;
            if (!no.All(char.IsDigit))
            {
                uyarilar.Add($"{r}. satırdaki öğrenci numarası geçersiz (\"{no}\"); atlandı.");
                continue;
            }
            var degerler = new Dictionary<string, decimal?>();
            foreach (var s in degerSutunlari)
            {
                var hucre = ws.Cell(r, s.Sutun);
                if (hucre.IsEmpty()) degerler[s.Baslik] = null;
                else if (Sayi(hucre) is { } d) degerler[s.Baslik] = d;
                else
                {
                    degerler[s.Baslik] = null;
                    uyarilar.Add($"{no} numaralı öğrencinin {s.Baslik} değeri sayı değil (\"{HucreMetni(hucre)}\").");
                }
            }
            satirlar.Add(new NotSatiri(r, no, adSutun > 0 ? Metin.AdSoyad(HucreMetni(ws.Cell(r, adSutun))) : "", degerler));
        }

        if (satirlar.Count == 0) throw new DosyaOkumaHatasi("Not dosyasında öğrenci satırı bulunamadı.");
        return new NotTablosuSonucu(degerSutunlari.Select(s => s.Baslik).ToList(), satirlar, uyarilar);
    }

    private static XLWorkbook Ac(Stream dosya)
    {
        try
        {
            return new XLWorkbook(dosya);
        }
        catch (Exception)
        {
            throw new DosyaOkumaHatasi("Excel dosyası açılamadı. Dosyanın .xlsx biçiminde olduğundan emin olun (eski .xls desteklenmez).");
        }
    }

    private readonly record struct BaslikSutunu(int Sutun, string Baslik, string Anahtar);

    private static (int Satir, List<BaslikSutunu> Sutunlar)? BaslikBul(IXLWorksheet ws, bool adSoyadZorunlu = true)
    {
        var son = Math.Min(ws.LastRowUsed()?.RowNumber() ?? 0, BaslikAramaSiniri);
        for (var r = 1; r <= son; r++)
        {
            var sutunlar = ws.Row(r).CellsUsed()
                .Select(c => new BaslikSutunu(c.Address.ColumnNumber, HucreMetni(c).Trim(), Metin.Anahtar(HucreMetni(c))))
                .Where(s => s.Anahtar.Length > 0)
                .ToList();

            if (!sutunlar.Any(s => NoBasliklari.Contains(s.Anahtar))) continue;
            var adSoyadVar = sutunlar.Any(s => AdSoyadBasliklari.Contains(s.Anahtar)) ||
                             (sutunlar.Any(s => AdBasliklari.Contains(s.Anahtar)) && sutunlar.Any(s => SoyadBasliklari.Contains(s.Anahtar)));
            if (adSoyadZorunlu && !adSoyadVar) continue;
            return (r, sutunlar);
        }
        return null;
    }

    /// <summary>Hücreyi metin olarak okur; sayı olarak saklanmış öğrenci numaralarını bilimsel gösterime çevirmeden döndürür.</summary>
    private static string HucreMetni(IXLCell c) => c.DataType switch
    {
        XLDataType.Number => c.GetDouble().ToString("0.############", CultureInfo.InvariantCulture),
        XLDataType.Blank => "",
        _ => c.GetFormattedString().Trim(),
    };

    private static decimal? Sayi(IXLCell c)
    {
        if (c.DataType == XLDataType.Number) return (decimal)c.GetDouble();
        var t = c.GetFormattedString().Trim().Replace(',', '.');
        return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
    }
}
