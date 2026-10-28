namespace Sds.Api.Domain;

/// <summary>Sistemdeki kullanıcı türleri. Yönetici yetkisi olan öğretim üyesi ayrıca <see cref="Kullanici.YoneticiYetkisi"/> ile işaretlenir.</summary>
public enum KullaniciRolu
{
    Admin = 1,
    OgretimUyesi = 2,
}

public enum Donem
{
    Guz = 1,
    Bahar = 2,
}

/// <summary>Normal öğretim (NÖ) / İkinci öğretim (İÖ)</summary>
public enum OgretimTuru
{
    NormalOgretim = 1,
    IkinciOgretim = 2,
}

public enum SinavTuru
{
    Vize = 1,
    Final = 2,
    Butunleme = 3,
}

public enum SinavFormati
{
    CoktanSecmeli = 1,
    AcikUclu = 2,
}

public enum SinavDurumu
{
    /// <summary>Sihirbaz devam ediyor</summary>
    Taslak = 1,
    /// <summary>5 adım tamamlandı, analiz raporu hazır</summary>
    Tamamlandi = 2,
}

/// <summary>Sınıf listesindeki öğrencinin optik kayıtla eşleşme durumu (3. adım).</summary>
public enum EslesmeDurumu
{
    /// <summary>Öğrenci numarasından otomatik eşleşti</summary>
    Otomatik = 1,
    /// <summary>Öğretim üyesi elle eşleştirdi</summary>
    Manuel = 2,
    /// <summary>Sınıf listesinde var, optikte yok: sınava girmedi</summary>
    SinavaGirmedi = 3,
    /// <summary>Açık uçlu sınavda puan Excel'den geldi</summary>
    NotDosyasindan = 4,
}

/// <summary>Optik dosyada sınıf listesiyle eşleşmeyen kaydın durumu.</summary>
public enum EslesmeyenOptikDurumu
{
    Bekliyor = 1,
    GozArdiEdildi = 2,
}

public enum PuanEklemeTuru
{
    Bonus = 1,
    ProjeNotu = 2,
}

public enum TalepDurumu
{
    Bekliyor = 1,
    Onaylandi = 2,
    Reddedildi = 3,
}

/// <summary>Optik okuyucu dosyasındaki sabit genişlikli alanlar.</summary>
public enum OptikAlan
{
    OgrenciNo = 1,
    AdSoyad = 2,
    KitapcikTuru = 3,
    SinavTuru = 4,
    Cevaplar = 5,
}
