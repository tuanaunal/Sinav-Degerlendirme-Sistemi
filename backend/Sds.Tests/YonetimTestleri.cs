using System.Net;
using System.Net.Http.Json;
using Sds.Api.Controllers;
using Sds.Api.Controllers.Yonetim;
using Sds.Api.Domain;

namespace Sds.Tests;

public class YonetimTestleri(TestUygulamasi uygulama) : IClassFixture<TestUygulamasi>
{
    private Task<HttpClient> Admin() => uygulama.GirisYapmisIstemciAsync("admin@selcuk.edu.tr", "admin10001*");
    private Task<HttpClient> Elif() => uygulama.GirisYapmisIstemciAsync("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*");
    private Task<HttpClient> Mehmet() => uygulama.GirisYapmisIstemciAsync("mehmet.kaya@selcuk.edu.tr", "mehmet.kaya20533*");

    [Fact]
    public async Task YetkisizOgretimUyesi_YonetimeErisemez()
    {
        var mehmet = await Mehmet();

        Assert.Equal(HttpStatusCode.Forbidden, (await mehmet.GetAsync("/api/yonetim/kullanicilar")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await mehmet.PutAsJsonAsync("/api/ayarlar/optik-form", new List<OptikAlanBilgisi>())).StatusCode);
    }

    [Fact]
    public async Task YoneticiYetkiliOgretimUyesi_YonetimeErisir()
    {
        var elif = await Elif();

        var kullanicilar = await elif.GetFromJsonAsync<List<KullaniciSatiri>>("/api/yonetim/kullanicilar");

        Assert.Contains(kullanicilar!, k => k.Eposta == "mehmet.kaya@selcuk.edu.tr" && k.Dersler.Count == 1);
    }

    [Fact]
    public async Task YeniOgretimUyesi_VarsayilanSifreyleGirerVeSifreDegistirmeliOlur()
    {
        var admin = await Admin();

        var yanit = await admin.PostAsJsonAsync("/api/yonetim/kullanicilar",
            new KullaniciKaydi("Doç. Dr.", "Ali", "Veli", "Ali.Veli@selcuk.edu.tr", "12345", false));
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        var giris = await uygulama.CreateClient().PostAsJsonAsync("/api/auth/giris", new GirisIstegi("ali.veli@selcuk.edu.tr", "ali.veli12345*"));
        var oturum = await giris.Content.ReadFromJsonAsync<OturumYaniti>();
        Assert.True(oturum!.Kullanici.SifreDegistirmeli);

        // Aynı e-posta ikinci kez eklenemez
        var tekrar = await admin.PostAsJsonAsync("/api/yonetim/kullanicilar",
            new KullaniciKaydi("", "Ali", "Veli", "ali.veli@selcuk.edu.tr", "99999", false));
        Assert.Equal(HttpStatusCode.Conflict, tekrar.StatusCode);
    }

    [Fact]
    public async Task YoneticiYetkisini_YalnizcaAdminVerebilir()
    {
        var elif = await Elif();
        var mehmetId = uygulama.Veritabani().Kullanicilar.Single(k => k.Eposta == "mehmet.kaya@selcuk.edu.tr").Id;

        var yanit = await elif.PutAsJsonAsync($"/api/yonetim/kullanicilar/{mehmetId}",
            new KullaniciGuncelleme("Dr. Öğr. Üyesi", "Mehmet", "Kaya", "20533", YoneticiYetkisi: true, Aktif: true));

        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
    }

    [Fact]
    public async Task SifreTalebiOnaylaninca_SifreKuralaDoner()
    {
        var db = uygulama.Veritabani();
        db.Kullanicilar.Add(new Kullanici
        {
            Ad = "Zeynep", Soyad = "Ak", Eposta = "zeynep.ak@selcuk.edu.tr", PersonelNo = "45678",
            SifreHash = BCrypt.Net.BCrypt.HashPassword("UnutulanSifre1"), Rol = KullaniciRolu.OgretimUyesi, SifreDegistirmeli = false,
        });
        await db.SaveChangesAsync();
        await uygulama.CreateClient().PostAsJsonAsync("/api/auth/sifremi-unuttum", new SifremiUnuttumIstegi("zeynep.ak@selcuk.edu.tr"));

        var admin = await Admin();
        var talepler = await admin.GetFromJsonAsync<List<SifreTalebiSatiri>>("/api/yonetim/sifre-talepleri?durum=Bekliyor");
        var talep = talepler!.Single(t => t.Eposta == "zeynep.ak@selcuk.edu.tr");

        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/yonetim/sifre-talepleri/{talep.Id}/onayla", null)).StatusCode);
        // İkinci kez işlenemez
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync($"/api/yonetim/sifre-talepleri/{talep.Id}/reddet", null)).StatusCode);

        var giris = await uygulama.CreateClient().PostAsJsonAsync("/api/auth/giris", new GirisIstegi("zeynep.ak@selcuk.edu.tr", "zeynep.ak45678*"));
        Assert.Equal(HttpStatusCode.OK, giris.StatusCode);
        Assert.True((await giris.Content.ReadFromJsonAsync<OturumYaniti>())!.Kullanici.SifreDegistirmeli);

        var onaylananlar = await admin.GetFromJsonAsync<List<SifreTalebiSatiri>>("/api/yonetim/sifre-talepleri?durum=Onaylandi");
        Assert.Contains(onaylananlar!, t => t.Id == talep.Id && t.IsleyenKullanici != null);
    }

    [Fact]
    public async Task EgitmenAtama_DersinEgitmenleriniDegistirir()
    {
        var admin = await Admin();
        var dersler = await admin.GetFromJsonAsync<List<DersSatiri>>("/api/yonetim/dersler");
        var veri = dersler!.Single(d => d.Kod == "3302205");
        var elifId = uygulama.Veritabani().Kullanicilar.Single(k => k.Eposta == "elif.yildirim@selcuk.edu.tr").Id;
        var adminId = uygulama.Veritabani().Kullanicilar.Single(k => k.Eposta == "admin@selcuk.edu.tr").Id;

        // Admin'in kendi dersi olmaz
        var hatali = await admin.PutAsJsonAsync($"/api/yonetim/dersler/{veri.Id}/egitmenler", new EgitmenAtama([adminId]));
        Assert.Equal(HttpStatusCode.BadRequest, hatali.StatusCode);

        var yanit = await admin.PutAsJsonAsync($"/api/yonetim/dersler/{veri.Id}/egitmenler",
            new EgitmenAtama([veri.Egitmenler[0].Id, elifId]));
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        var sonra = (await admin.GetFromJsonAsync<List<DersSatiri>>("/api/yonetim/dersler"))!.Single(d => d.Id == veri.Id);
        Assert.Equal(2, sonra.Egitmenler.Count);
    }

    [Fact]
    public async Task Notlandirma_ToplamYuzOlmazsaReddedilir_VarsayilanlarKaydedilebilir()
    {
        var admin = await Admin();
        var mevcut = await admin.GetFromJsonAsync<NotlandirmaBilgisi>("/api/ayarlar/notlandirma");
        Assert.Equal("AA – BA – BB – CB – CC – DC – DD – FF", mevcut!.SemaAdi);
        Assert.Equal(mevcut.FinalAgirligi, mevcut.ButunlemeAgirligi);

        var hatali = await admin.PutAsJsonAsync("/api/ayarlar/notlandirma",
            new NotlandirmaGuncelleme(50, 60, 30, mevcut.Harfler));
        Assert.Equal(HttpStatusCode.BadRequest, hatali.StatusCode);

        var dogru = await admin.PutAsJsonAsync("/api/ayarlar/notlandirma",
            new NotlandirmaGuncelleme(40, 60, 30, mevcut.Harfler));
        Assert.Equal(HttpStatusCode.OK, dogru.StatusCode);
    }

    [Fact]
    public void HarfAraliklari_BoslukVeyaCakismaKabulEdilmez()
    {
        var sira = new List<string> { "AA", "BA", "FF" };
        HarfAraligi H(string h, int alt, int ust) => new(h, 0, alt, ust, "");

        Assert.Null(AyarlarController.HarfAraliklariniDogrula(sira, [H("AA", 80, 100), H("BA", 50, 79), H("FF", 0, 49)]));
        Assert.NotNull(AyarlarController.HarfAraliklariniDogrula(sira, [H("AA", 80, 100), H("BA", 50, 78), H("FF", 0, 49)]));
        Assert.NotNull(AyarlarController.HarfAraliklariniDogrula(sira, [H("AA", 80, 100), H("BA", 50, 79), H("FF", 5, 49)]));
    }

    [Fact]
    public void OptikAlanlar_CakismaVeCevaplarSirasiKontrolEdilir()
    {
        OptikAlanBilgisi A(OptikAlan a, int b, int? s) => new(a.ToString(), b, s);
        List<OptikAlanBilgisi> Duzen(int adBitis, int cevapBaslangic) =>
        [
            A(OptikAlan.OgrenciNo, 0, 8), A(OptikAlan.SinavTuru, 9, 9), A(OptikAlan.AdSoyad, 10, adBitis),
            A(OptikAlan.KitapcikTuru, 31, 31), A(OptikAlan.Cevaplar, cevapBaslangic, null),
        ];

        Assert.Null(AyarlarController.OptikAlanlariniDogrula(Duzen(30, 32)));
        Assert.NotNull(AyarlarController.OptikAlanlariniDogrula(Duzen(31, 32))); // ad soyad kitapçıkla çakışıyor
        Assert.NotNull(AyarlarController.OptikAlanlariniDogrula(Duzen(30, 31))); // cevaplar kitapçıktan önce başlıyor
    }
}
