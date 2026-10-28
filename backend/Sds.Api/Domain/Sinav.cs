namespace Sds.Api.Domain;

/// <summary>Bir dersin vize, final veya bütünleme sınavı. Her ders için her türden en fazla bir sınav vardır.</summary>
public class Sinav
{
    public int Id { get; set; }
    public int DersId { get; set; }
    public Ders Ders { get; set; } = null!;
    public SinavTuru Tur { get; set; }
    public SinavFormati Format { get; set; }
    public SinavDurumu Durum { get; set; } = SinavDurumu.Taslak;
    /// <summary>Sihirbazda en son tamamlanan adım (0-5)</summary>
    public int TamamlananAdim { get; set; }
    public DateOnly? Tarih { get; set; }
    public int SoruSayisi { get; set; }
    public decimal MaksimumPuan { get; set; } = 100;
    /// <summary>A, B, ... kitapçık sayısı (açık uçluda 1)</summary>
    public int KitapcikSayisi { get; set; } = 1;
    /// <summary>"Puanları dinamik dağıt": maksimum puan sorulara eşit bölünür</summary>
    public bool PuanlariDinamikDagit { get; set; } = true;
    /// <summary>İptal edilen sorularda birden fazla şık doğru kabul edilebilir</summary>
    public bool BirdenFazlaDogruKabul { get; set; }
    /// <summary>B, C... kitapçıklarının A kitapçığına göre soru eşleştirmesi kaydedildi mi (5. adım)</summary>
    public bool KitapcikEslestirmeKaydedildi { get; set; }
    /// <summary>Resmi formdaki "Sınavın Değerlendirmesi" metni (öğretim üyesi yazar / düzenler)</summary>
    public string? DegerlendirmeMetni { get; set; }
    public string? OptikDosyaAdi { get; set; }
    public string? SinifListesiDosyaAdi { get; set; }
    public int OlusturanKullaniciId { get; set; }
    public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;
    public DateTime GuncellemeTarihi { get; set; } = DateTime.UtcNow;

    public List<SinavSorusu> Sorular { get; set; } = [];
    public List<KitapcikSorusu> KitapcikSorulari { get; set; } = [];
    public List<SinavSonucu> Sonuclar { get; set; } = [];
    public List<OptikKaydi> OptikKayitlari { get; set; } = [];
    public List<SoruOgrenmeCiktisi> SoruOgrenmeCiktilari { get; set; } = [];
}

/// <summary>Sınavın soruları A kitapçığı sırasıyla tutulur; analizler bu sıraya göre yapılır.</summary>
public class SinavSorusu
{
    public int Id { get; set; }
    public int SinavId { get; set; }
    public Sinav Sinav { get; set; } = null!;
    /// <summary>A kitapçığındaki soru numarası (1'den başlar)</summary>
    public int SoruNo { get; set; }
    public decimal Puan { get; set; }
    /// <summary>İptal edilen soru: herkese tam puan verilir</summary>
    public bool Iptal { get; set; }
}

/// <summary>
/// Cevap anahtarı ve kitapçık soru eşleştirmesi. Her kitapçıktaki her soru için doğru şık(lar)
/// ve o sorunun A kitapçığındaki karşılığı tutulur.
/// </summary>
public class KitapcikSorusu
{
    public int Id { get; set; }
    public int SinavId { get; set; }
    public Sinav Sinav { get; set; } = null!;
    /// <summary>'A', 'B', ...</summary>
    public char Kitapcik { get; set; }
    /// <summary>Bu kitapçıktaki soru numarası</summary>
    public int SoruNo { get; set; }
    /// <summary>A kitapçığındaki karşılığı (A kitapçığı için SoruNo ile aynıdır)</summary>
    public int? AnaSoruNo { get; set; }
    /// <summary>Doğru şık(lar): "C" veya birden fazla doğru kabulünde "AC"</summary>
    public string DogruCevap { get; set; } = "";
}

/// <summary>Sınıf listesindeki bir öğrencinin bu sınavdaki sonucu.</summary>
public class SinavSonucu
{
    public int Id { get; set; }
    public int SinavId { get; set; }
    public Sinav Sinav { get; set; } = null!;
    public int OgrenciId { get; set; }
    public Ogrenci Ogrenci { get; set; } = null!;
    public EslesmeDurumu Eslesme { get; set; }
    public char? Kitapcik { get; set; }
    /// <summary>Optik dosyadaki ham satır bilgileri (eşleştirme ve inceleme için)</summary>
    public string? OptikOgrenciNo { get; set; }
    public string? OptikAdSoyad { get; set; }
    /// <summary>Kitapçık sırasıyla işaretlenen şıklar, boşlar ' ' olarak</summary>
    public string? OptikCevaplar { get; set; }
    public int DogruSayisi { get; set; }
    public int YanlisSayisi { get; set; }
    public int BosSayisi { get; set; }
    /// <summary>Cevap anahtarından hesaplanan puan (açık uçluda soru puanları toplamı)</summary>
    public decimal HamPuan { get; set; }
    /// <summary>İtiraz vb. nedenle öğretim üyesinin elle girdiği puan; doluysa ham puanın yerine geçer</summary>
    public decimal? ManuelPuan { get; set; }

    public List<SoruCevabi> SoruCevaplari { get; set; } = [];
    public List<PuanEklemesi> Eklemeler { get; set; } = [];

    public bool SinavaGirdi => Eslesme is EslesmeDurumu.Otomatik or EslesmeDurumu.Manuel or EslesmeDurumu.NotDosyasindan;

    /// <summary>Düzeltilmiş puan = (manuel veya ham puan) + eklemeler, 0-100 aralığında</summary>
    public decimal DuzeltilmisPuan(decimal maksimum = 100) =>
        Math.Clamp((ManuelPuan ?? HamPuan) + Eklemeler.Sum(e => e.Puan), 0, maksimum);
}

/// <summary>Öğrencinin bir soruya verdiği cevap, A kitapçığı soru numarasına çevrilmiş olarak.</summary>
public class SoruCevabi
{
    public int Id { get; set; }
    public int SinavSonucuId { get; set; }
    public SinavSonucu SinavSonucu { get; set; } = null!;
    public int SoruNo { get; set; }
    /// <summary>İşaretlenen şık; boş bırakıldıysa null. Açık uçluda kullanılmaz.</summary>
    public char? VerilenCevap { get; set; }
    public bool? DogruMu { get; set; }
    public decimal Puan { get; set; }
}

public class PuanEklemesi
{
    public int Id { get; set; }
    public int SinavSonucuId { get; set; }
    public SinavSonucu SinavSonucu { get; set; } = null!;
    public PuanEklemeTuru Tur { get; set; }
    public decimal Puan { get; set; }
    public string? Aciklama { get; set; }
    public DateTime Tarih { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Yüklenen optik dosyanın bir satırı (öğrenci kağıdı veya cevap anahtarı kağıdı).
/// 2. adımda "Optikten seç", 3. adımda sınıf listesiyle eşleştirme bu kayıtlar üzerinden yapılır.
/// </summary>
public class OptikKaydi
{
    public int Id { get; set; }
    public int SinavId { get; set; }
    public Sinav Sinav { get; set; } = null!;
    public int SatirNo { get; set; }
    public string OgrenciNo { get; set; } = "";
    public string AdSoyad { get; set; } = "";
    public char? Kitapcik { get; set; }
    public char? SinavTuruKodu { get; set; }
    /// <summary>Kitapçık sırasıyla işaretlenen şıklar; boş ' ', çoklu işaretleme '*'</summary>
    public string Cevaplar { get; set; } = "";
    public bool CevapAnahtariMi { get; set; }
    public OptikKayitDurumu Durum { get; set; } = OptikKayitDurumu.Bekliyor;
    /// <summary>Eşleştiği sınıf listesi öğrencisinin sonucu</summary>
    public int? SinavSonucuId { get; set; }
}

/// <summary>Öğrenme çıktısı - soru ilişkilendirmesi (5. adımdaki işaretleme matrisi).</summary>
public class SoruOgrenmeCiktisi
{
    public int SinavId { get; set; }
    public Sinav Sinav { get; set; } = null!;
    public int OgrenmeCiktisiId { get; set; }
    public OgrenmeCiktisi OgrenmeCiktisi { get; set; } = null!;
    /// <summary>A kitapçığı soru numarası</summary>
    public int SoruNo { get; set; }
}
