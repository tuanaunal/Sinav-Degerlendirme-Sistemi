namespace Sds.Api.Domain;

/// <summary>Notlandırma yönetmeliği: sınav ağırlıkları ve final alt sınırı (tek kayıt).</summary>
public class NotlandirmaAyari
{
    public int Id { get; set; }
    public string SemaAdi { get; set; } = "AA – BA – BB – CB – CC – DC – DD – FF";
    /// <summary>Yarıyıl içi (vize) katkısı, %</summary>
    public int VizeAgirligi { get; set; } = 40;
    /// <summary>Final katkısı, %. Bütünleme finalin yerine geçer ve aynı ağırlığı taşır.</summary>
    public int FinalAgirligi { get; set; } = 60;
    /// <summary>Final veya bütünlemeden alınması gereken en düşük puan; altında kalan FF alır.</summary>
    public int FinalAltSiniri { get; set; } = 30;
    public DateTime GuncellemeTarihi { get; set; } = DateTime.UtcNow;
}

/// <summary>Mutlak değerlendirme sistemindeki harf notu aralıkları.</summary>
public class HarfNotuAraligi
{
    public int Id { get; set; }
    public string Harf { get; set; } = "";
    public decimal Katsayi { get; set; }
    public int AltSinir { get; set; }
    public int UstSinir { get; set; }
    /// <summary>Başarılı / Şartlı geçer / Başarısız</summary>
    public string Durum { get; set; } = "";
    public int Sira { get; set; }
}

/// <summary>Optik okuyucu dosyasındaki bir alanın başlangıç ve bitiş indeksleri (0 tabanlı, bitiş dahil).</summary>
public class OptikFormAlani
{
    public int Id { get; set; }
    public OptikAlan Alan { get; set; }
    public int Baslangic { get; set; }
    /// <summary>Cevaplar alanında null: soru sayısı kadar okunur</summary>
    public int? Bitis { get; set; }
}
