namespace Sds.Api.Domain;

public class Kullanici
{
    public int Id { get; set; }
    /// <summary>"Dr. Öğr. Üyesi", "Doç. Dr.", "Prof. Dr." gibi</summary>
    public string Unvan { get; set; } = "";
    public string Ad { get; set; } = "";
    public string Soyad { get; set; } = "";
    public string Eposta { get; set; } = "";
    /// <summary>Şifre sıfırlamada kullanılır: e-postanın @ öncesi + PersonelNo + *</summary>
    public string PersonelNo { get; set; } = "";
    public string SifreHash { get; set; } = "";
    public KullaniciRolu Rol { get; set; }
    /// <summary>Öğretim üyesine verilen yönetici yetkisi (Admin + Öğretim Üyesi rolü)</summary>
    public bool YoneticiYetkisi { get; set; }
    /// <summary>İlk girişte veya şifre sıfırlandıktan sonra true: kullanıcı önce şifresini değiştirir.</summary>
    public bool SifreDegistirmeli { get; set; } = true;
    public bool Aktif { get; set; } = true;
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

    public List<DersEgitmen> Dersleri { get; set; } = [];

    public string TamAd => string.Join(' ', new[] { Unvan, Ad, Soyad }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

public class SifreSifirlamaTalebi
{
    public int Id { get; set; }
    public int KullaniciId { get; set; }
    public Kullanici Kullanici { get; set; } = null!;
    public DateTime TalepTarihi { get; set; } = DateTime.UtcNow;
    public TalepDurumu Durum { get; set; } = TalepDurumu.Bekliyor;
    public DateTime? IslemTarihi { get; set; }
    public int? IsleyenKullaniciId { get; set; }
    public Kullanici? IsleyenKullanici { get; set; }
}
