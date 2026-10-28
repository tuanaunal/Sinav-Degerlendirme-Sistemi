using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sds.Api.Controllers;
using Sds.Api.Domain;

namespace Sds.Tests;

public class KimlikDogrulamaTestleri(TestUygulamasi uygulama) : IClassFixture<TestUygulamasi>
{
    [Fact]
    public async Task Giris_DogruBilgilerle_TokenVeRolDoner()
    {
        var yanit = await uygulama.CreateClient()
            .PostAsJsonAsync("/api/auth/giris", new GirisIstegi("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*"));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        var oturum = await yanit.Content.ReadFromJsonAsync<OturumYaniti>();
        Assert.False(string.IsNullOrEmpty(oturum!.Token));
        Assert.Equal("OgretimUyesi", oturum.Kullanici.Rol);
        Assert.True(oturum.Kullanici.YoneticiYetkisi);
        Assert.Equal("Dr. Öğr. Üyesi Elif Yıldırım", oturum.Kullanici.TamAd);
    }

    [Fact]
    public async Task Giris_EpostaBuyukHarfVeBosluklaYazilsaDaCalisir()
    {
        var yanit = await uygulama.CreateClient()
            .PostAsJsonAsync("/api/auth/giris", new GirisIstegi("  ADMIN@selcuk.edu.tr ", "admin10001*"));

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
    }

    [Theory]
    [InlineData("admin@selcuk.edu.tr", "yanlis-sifre")]
    [InlineData("olmayan@selcuk.edu.tr", "admin10001*")]
    public async Task Giris_HataliBilgilerle_401(string eposta, string sifre)
    {
        var yanit = await uygulama.CreateClient().PostAsJsonAsync("/api/auth/giris", new GirisIstegi(eposta, sifre));

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact]
    public async Task Ben_TokenOlmadan_401()
    {
        var yanit = await uygulama.CreateClient().GetAsync("/api/auth/ben");

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact]
    public async Task Ben_TokenIle_KullaniciBilgisiDoner()
    {
        var istemci = await uygulama.GirisYapmisIstemciAsync("mehmet.kaya@selcuk.edu.tr", "mehmet.kaya20533*");

        var ben = await istemci.GetFromJsonAsync<KullaniciBilgisi>("/api/auth/ben");

        Assert.Equal("mehmet.kaya@selcuk.edu.tr", ben!.Eposta);
        Assert.False(ben.YoneticiYetkisi);
    }

    [Fact]
    public async Task SifreDegistirmeliKullanici_SifresiniDegistirinceZorunlulukKalkar()
    {
        var db = uygulama.Veritabani();
        db.Kullanicilar.Add(new Kullanici
        {
            Ad = "Yeni", Soyad = "Hoca", Eposta = "yeni.hoca@selcuk.edu.tr", PersonelNo = "30001",
            SifreHash = BCrypt.Net.BCrypt.HashPassword("yeni.hoca30001*"),
            Rol = KullaniciRolu.OgretimUyesi, SifreDegistirmeli = true,
        });
        await db.SaveChangesAsync();

        var istemci = uygulama.CreateClient();
        var giris = await (await istemci.PostAsJsonAsync("/api/auth/giris",
            new GirisIstegi("yeni.hoca@selcuk.edu.tr", "yeni.hoca30001*"))).Content.ReadFromJsonAsync<OturumYaniti>();
        Assert.True(giris!.Kullanici.SifreDegistirmeli);
        istemci.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", giris.Token);

        // Kurala uymayan şifre reddedilir
        var zayif = await istemci.PostAsJsonAsync("/api/auth/sifre-degistir",
            new SifreDegistirIstegi("yeni.hoca30001*", "kisa", "kisa"));
        Assert.Equal(HttpStatusCode.BadRequest, zayif.StatusCode);

        var yanit = await istemci.PostAsJsonAsync("/api/auth/sifre-degistir",
            new SifreDegistirIstegi("yeni.hoca30001*", "GuvenliSifre2026", "GuvenliSifre2026"));
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        var yeni = await yanit.Content.ReadFromJsonAsync<OturumYaniti>();
        Assert.False(yeni!.Kullanici.SifreDegistirmeli);

        // Eski şifre artık çalışmaz, yenisi çalışır
        var eski = await uygulama.CreateClient().PostAsJsonAsync("/api/auth/giris",
            new GirisIstegi("yeni.hoca@selcuk.edu.tr", "yeni.hoca30001*"));
        Assert.Equal(HttpStatusCode.Unauthorized, eski.StatusCode);
        await uygulama.GirisYapmisIstemciAsync("yeni.hoca@selcuk.edu.tr", "GuvenliSifre2026");
    }

    [Fact]
    public async Task SifremiUnuttum_TekBekleyenTalepOlusturur_VeHesapVarligiBelliEdilmez()
    {
        var istemci = uygulama.CreateClient();

        var var1 = await istemci.PostAsJsonAsync("/api/auth/sifremi-unuttum", new SifremiUnuttumIstegi("mehmet.kaya@selcuk.edu.tr"));
        var var2 = await istemci.PostAsJsonAsync("/api/auth/sifremi-unuttum", new SifremiUnuttumIstegi("mehmet.kaya@selcuk.edu.tr"));
        var yok = await istemci.PostAsJsonAsync("/api/auth/sifremi-unuttum", new SifremiUnuttumIstegi("olmayan@selcuk.edu.tr"));

        Assert.Equal(HttpStatusCode.OK, var1.StatusCode);
        Assert.Equal(await var1.Content.ReadAsStringAsync(), await yok.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, var2.StatusCode);

        var db = uygulama.Veritabani();
        var mehmet = db.Kullanicilar.Single(k => k.Eposta == "mehmet.kaya@selcuk.edu.tr");
        Assert.Equal(1, db.SifreSifirlamaTalepleri.Count(t => t.KullaniciId == mehmet.Id && t.Durum == TalepDurumu.Bekliyor));
    }
}
