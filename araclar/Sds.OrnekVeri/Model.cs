namespace Sds.OrnekVeri;

public record Ogrenci(string No, string Ad, string Soyad, double Yetenek, bool Tekrar)
{
    public string AdSoyad => $"{Ad} {Soyad}";
}

/// <summary>Çoktan seçmeli bir sorunun simülasyon parametreleri.</summary>
public record Soru(int No, char Dogru, double Zorluk, char GucluCeldirici, double CeldiriciEgilimi);

/// <summary>Bir çoktan seçmeli sınav: A kitapçığı soruları ve B kitapçığının A'ya göre sıralaması.</summary>
public class CoktanSecmeliSinav
{
    public required string Ad { get; init; }
    /// <summary>Optikteki sınav türü kodu: 1 vize, 2 final, 3 bütünleme</summary>
    public required char TurKodu { get; init; }
    public required List<Soru> Sorular { get; init; }
    /// <summary>B[i] = B kitapçığındaki (i+1). sorunun A kitapçığındaki numarası</summary>
    public required int[] BdenAya { get; init; }
    public decimal SoruPuani => 100m / Sorular.Count;

    public char DogruCevap(char kitapcik, int kitapcikSoruNo) =>
        Sorular[(kitapcik == 'B' ? BdenAya[kitapcikSoruNo - 1] : kitapcikSoruNo) - 1].Dogru;
}

/// <summary>Bir öğrencinin optik kağıdındaki işaretlemeler (kitapçık sırasıyla).</summary>
public record OptikKagit(Ogrenci Ogrenci, char Kitapcik, string Cevaplar)
{
    /// <summary>Optiğe yazılan numara ve isim (hatalı kodlama senaryolarında öğrencinin gerçek bilgisinden farklıdır)</summary>
    public string YazilanNo { get; init; } = Ogrenci.No;
    public string YazilanAdSoyad { get; init; } = Isimler.Buyuk(Ogrenci.AdSoyad);
    public char YazilanKitapcik { get; init; } = Kitapcik;
    public string? Senaryo { get; init; }
}
