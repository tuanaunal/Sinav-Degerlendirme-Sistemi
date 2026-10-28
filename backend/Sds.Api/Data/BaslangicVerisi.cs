using Microsoft.EntityFrameworkCore;
using Sds.Api.Domain;

namespace Sds.Api.Data;

/// <summary>
/// Veritabanı ilk kez oluşturulduğunda sistem ayarlarını ve demo hesapları ekler.
/// Var olan kayıtlara dokunmaz, bu yüzden her açılışta güvenle çalıştırılabilir.
/// </summary>
public static class BaslangicVerisi
{
    /// <summary>Şifre kuralı: e-postanın @ öncesi + personel no + "*" (örn. ali@selcuk.edu.tr, 12345 → ali12345*)</summary>
    public static string VarsayilanSifre(string eposta, string personelNo) =>
        $"{eposta.Split('@')[0]}{personelNo}*";

    public static async Task UygulaAsync(SdsDbContext db)
    {
        await NotlandirmaAsync(db);
        await OptikFormAsync(db);
        await KullanicilarVeDerslerAsync(db);
        await db.SaveChangesAsync();
        await GecmisDonemAsync(db);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// BMU dersinin geçen yılki kaydı ve öğrenme çıktıları: 5. adımdaki "Öğrenme Çıktılarını Getir"
    /// bu dönemden çıktıları kopyalar. Var olan veritabanlarına da bir kez eklenir.
    /// </summary>
    private static async Task GecmisDonemAsync(SdsDbContext db)
    {
        const string kod = "3304701", yil = "2025-2026";
        if (await db.Dersler.AnyAsync(d => d.Kod == kod && d.AkademikYil == yil)) return;
        var elif = await db.Kullanicilar.FirstOrDefaultAsync(k => k.Eposta == "elif.yildirim@selcuk.edu.tr");
        if (elif is null) return;

        var ders = new Ders { Kod = kod, Ad = "Bilgisayar Mühendisliği Uygulamaları", AkademikYil = yil, Donem = Donem.Guz, Sinif = 4 };
        ders.Egitmenler.Add(new DersEgitmen { Kullanici = elif });
        string[] ciktilar =
        [
            "Mühendislik problemlerini tanımlar, analiz eder ve çözüm için uygun yöntem ve araçları seçer.",
            "Yazılım geliştirme yaşam döngüsünün (gereksinim, tasarım, gerçekleştirme, test) aşamalarını bir projede uygular.",
            "Veritabanı ve web teknolojilerini kullanarak çok katmanlı bir uygulama geliştirir.",
            "Yazılım kalitesi, sürüm kontrolü ve test yöntemlerini uygulamalarında kullanır.",
            "Takım içinde çalışır, proje yönetimi araçlarını kullanır ve teknik rapor hazırlar.",
        ];
        for (var i = 0; i < ciktilar.Length; i++)
            ders.OgrenmeCiktilari.Add(new OgrenmeCiktisi { SiraNo = i + 1, Aciklama = ciktilar[i] });
        db.Dersler.Add(ders);
    }

    /// <summary>Selçuk Üniversitesi Ön Lisans ve Lisans Eğitim-Öğretim ve Sınav Yönetmeliği, Madde 13.</summary>
    private static async Task NotlandirmaAsync(SdsDbContext db)
    {
        if (!await db.NotlandirmaAyarlari.AnyAsync())
            db.NotlandirmaAyarlari.Add(new NotlandirmaAyari());

        if (await db.HarfNotuAraliklari.AnyAsync()) return;

        db.HarfNotuAraliklari.AddRange(
            new HarfNotuAraligi { Sira = 1, Harf = "AA", Katsayi = 4.00m, AltSinir = 88, UstSinir = 100, Durum = "Başarılı" },
            new HarfNotuAraligi { Sira = 2, Harf = "BA", Katsayi = 3.50m, AltSinir = 80, UstSinir = 87, Durum = "Başarılı" },
            new HarfNotuAraligi { Sira = 3, Harf = "BB", Katsayi = 3.00m, AltSinir = 73, UstSinir = 79, Durum = "Başarılı" },
            new HarfNotuAraligi { Sira = 4, Harf = "CB", Katsayi = 2.50m, AltSinir = 66, UstSinir = 72, Durum = "Başarılı" },
            new HarfNotuAraligi { Sira = 5, Harf = "CC", Katsayi = 2.00m, AltSinir = 60, UstSinir = 65, Durum = "Başarılı" },
            new HarfNotuAraligi { Sira = 6, Harf = "DC", Katsayi = 1.50m, AltSinir = 55, UstSinir = 59, Durum = "Şartlı Geçer" },
            new HarfNotuAraligi { Sira = 7, Harf = "DD", Katsayi = 1.00m, AltSinir = 50, UstSinir = 54, Durum = "Şartlı Geçer" },
            new HarfNotuAraligi { Sira = 8, Harf = "FF", Katsayi = 0.00m, AltSinir = 0, UstSinir = 49, Durum = "Başarısız" });
    }

    /// <summary>Optik okuyucunun varsayılan sütun düzeni (0 tabanlı, bitiş dahil).</summary>
    private static async Task OptikFormAsync(SdsDbContext db)
    {
        if (await db.OptikFormAlanlari.AnyAsync()) return;

        db.OptikFormAlanlari.AddRange(
            new OptikFormAlani { Alan = OptikAlan.OgrenciNo, Baslangic = 0, Bitis = 8 },
            new OptikFormAlani { Alan = OptikAlan.SinavTuru, Baslangic = 9, Bitis = 9 },
            new OptikFormAlani { Alan = OptikAlan.AdSoyad, Baslangic = 10, Bitis = 30 },
            new OptikFormAlani { Alan = OptikAlan.KitapcikTuru, Baslangic = 31, Bitis = 31 },
            new OptikFormAlani { Alan = OptikAlan.Cevaplar, Baslangic = 32, Bitis = null });
    }

    private static async Task KullanicilarVeDerslerAsync(SdsDbContext db)
    {
        if (await db.Kullanicilar.AnyAsync()) return;

        var admin = Kullanici("", "Sistem", "Yöneticisi", "admin@selcuk.edu.tr", "10001", KullaniciRolu.Admin, yonetici: false);
        // Admin + Öğretim Üyesi: kendi dersleri + sistem ayarları
        var elif = Kullanici("Dr. Öğr. Üyesi", "Elif", "Yıldırım", "elif.yildirim@selcuk.edu.tr", "20417", KullaniciRolu.OgretimUyesi, yonetici: true);
        var mehmet = Kullanici("Dr. Öğr. Üyesi", "Mehmet", "Kaya", "mehmet.kaya@selcuk.edu.tr", "20533", KullaniciRolu.OgretimUyesi, yonetici: false);
        db.Kullanicilar.AddRange(admin, elif, mehmet);

        const string yil = "2026-2027";
        var bmu = new Ders { Kod = "3304701", Ad = "Bilgisayar Mühendisliği Uygulamaları", AkademikYil = yil, Donem = Donem.Guz, Sinif = 4 };
        var ayrik = new Ders { Kod = "3301104", Ad = "Ayrık Matematik", AkademikYil = yil, Donem = Donem.Guz, Sinif = 1 };
        var veri = new Ders { Kod = "3302205", Ad = "Veri Yapıları ve Algoritmalar", AkademikYil = yil, Donem = Donem.Guz, Sinif = 2 };
        db.Dersler.AddRange(bmu, ayrik, veri);

        db.DersEgitmenleri.AddRange(
            new DersEgitmen { Ders = bmu, Kullanici = elif },
            new DersEgitmen { Ders = ayrik, Kullanici = elif },
            new DersEgitmen { Ders = veri, Kullanici = mehmet });
    }

    /// <summary>Demo hesaplar ilk girişte şifre değiştirmeye zorlanmaz; yeni eklenen kullanıcılar zorlanır.</summary>
    private static Kullanici Kullanici(string unvan, string ad, string soyad, string eposta, string personelNo,
        KullaniciRolu rol, bool yonetici) => new()
    {
        Unvan = unvan,
        Ad = ad,
        Soyad = soyad,
        Eposta = eposta,
        PersonelNo = personelNo,
        SifreHash = BCrypt.Net.BCrypt.HashPassword(VarsayilanSifre(eposta, personelNo)),
        Rol = rol,
        YoneticiYetkisi = yonetici,
        SifreDegistirmeli = false,
    };
}
