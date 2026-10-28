namespace Sds.Api.Domain;

public class Ders
{
    public int Id { get; set; }
    /// <summary>Örn. "3301258"</summary>
    public string Kod { get; set; } = "";
    public string Ad { get; set; } = "";
    /// <summary>Örn. "2026-2027"</summary>
    public string AkademikYil { get; set; } = "";
    public Donem Donem { get; set; }
    public OgretimTuru OgretimTuru { get; set; } = OgretimTuru.NormalOgretim;
    /// <summary>1-4</summary>
    public int Sinif { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

    public List<DersEgitmen> Egitmenler { get; set; } = [];
    public List<OgrenmeCiktisi> OgrenmeCiktilari { get; set; } = [];
    public List<Sinav> Sinavlar { get; set; } = [];
}

/// <summary>Bir öğretim üyesi yalnızca eğitmen olarak atandığı derslere erişebilir.</summary>
public class DersEgitmen
{
    public int DersId { get; set; }
    public Ders Ders { get; set; } = null!;
    public int KullaniciId { get; set; }
    public Kullanici Kullanici { get; set; } = null!;
}

public class OgrenmeCiktisi
{
    public int Id { get; set; }
    public int DersId { get; set; }
    public Ders Ders { get; set; } = null!;
    public int SiraNo { get; set; }
    public string Aciklama { get; set; } = "";
}

/// <summary>OBİS sınıf listesinden gelen öğrenci. Öğrenci numarası tüm sınavlar arasında ortak anahtardır.</summary>
public class Ogrenci
{
    public int Id { get; set; }
    public string OgrenciNo { get; set; } = "";
    public string AdSoyad { get; set; } = "";
}
