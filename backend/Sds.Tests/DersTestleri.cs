using System.Net;
using System.Net.Http.Json;
using Sds.Api.Controllers;
using Sds.Api.Domain;

namespace Sds.Tests;

public class DersTestleri(TestUygulamasi uygulama) : IClassFixture<TestUygulamasi>
{
    private Task<HttpClient> Elif() => uygulama.GirisYapmisIstemciAsync("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*");
    private Task<HttpClient> Mehmet() => uygulama.GirisYapmisIstemciAsync("mehmet.kaya@selcuk.edu.tr", "mehmet.kaya20533*");

    [Fact]
    public async Task OgretimUyesi_YalnizcaKendiDersleriniGorur()
    {
        var elif = await Elif();

        var dersler = await elif.GetFromJsonAsync<List<DersKarti>>("/api/dersler?akademikYil=2026-2027");

        Assert.Equal(["3301104", "3304701"], dersler!.Select(d => d.Kod).Order().ToList());
    }

    [Fact]
    public async Task BaskasininDersDetayi_404()
    {
        var elif = await Elif();
        var veriYapilari = uygulama.Veritabani().Dersler.Single(d => d.Kod == "3302205").Id;

        var yanit = await elif.GetAsync($"/api/dersler/{veriYapilari}");

        Assert.Equal(HttpStatusCode.NotFound, yanit.StatusCode);
    }

    [Fact]
    public async Task Admin_DersPanosunaErisemez()
    {
        var admin = await uygulama.GirisYapmisIstemciAsync("admin@selcuk.edu.tr", "admin10001*");

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.GetAsync("/api/dersler")).StatusCode);
    }

    [Fact]
    public async Task YeniDers_EkleyenEgitmenOlur_AyniDersTekrarEklenemez()
    {
        var mehmet = await Mehmet();
        var istek = new YeniDers("bil401", "Yapay Zeka", "2026-2027", Donem.Guz, OgretimTuru.IkinciOgretim, 4);

        var yanit = await mehmet.PostAsJsonAsync("/api/dersler", istek);
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        var ders = await yanit.Content.ReadFromJsonAsync<DersKarti>();
        Assert.Equal("BIL401", ders!.Kod);
        Assert.Equal("IkinciOgretim", ders.OgretimTuru);
        Assert.Equal(["Dr. Öğr. Üyesi Mehmet Kaya"], ders.Egitmenler);

        var detay = await mehmet.GetFromJsonAsync<DersDetayi>($"/api/dersler/{ders.Id}");
        Assert.False(detay!.DonemDegerlendirmeAcik);

        // Başka bir öğretim üyesi aynı dersi aynı dönem için tekrar oluşturamaz
        var elif = await Elif();
        var tekrar = await elif.PostAsJsonAsync("/api/dersler", istek);
        Assert.Equal(HttpStatusCode.Conflict, tekrar.StatusCode);
    }

    [Fact]
    public async Task YeniDers_EnumlarIsimleGonderilebilir_GecersizYilReddedilir()
    {
        var mehmet = await Mehmet();

        var gecerli = await mehmet.PostAsJsonAsync("/api/dersler",
            new { kod = "BIL302", ad = "Veritabanı Sistemleri", akademikYil = "2026-2027", donem = "Bahar", ogretimTuru = "NormalOgretim", sinif = 3 });
        Assert.Equal(HttpStatusCode.OK, gecerli.StatusCode);

        var hatali = await mehmet.PostAsJsonAsync("/api/dersler",
            new { kod = "BIL303", ad = "Ders", akademikYil = "2026-2028", donem = "Guz", ogretimTuru = "NormalOgretim", sinif = 3 });
        Assert.Equal(HttpStatusCode.BadRequest, hatali.StatusCode);
    }

    [Fact]
    public void DonemDegerlendirme_VizeVeFinalVeyaButunlemeTamamlaninca_Acilir()
    {
        Sinav S(SinavTuru t, SinavDurumu d = SinavDurumu.Tamamlandi) => new() { Tur = t, Durum = d };

        Assert.False(DerslerController.DonemDegerlendirmeAcikMi([]));
        Assert.False(DerslerController.DonemDegerlendirmeAcikMi([S(SinavTuru.Vize)]));
        Assert.False(DerslerController.DonemDegerlendirmeAcikMi([S(SinavTuru.Vize), S(SinavTuru.Final, SinavDurumu.Taslak)]));
        Assert.False(DerslerController.DonemDegerlendirmeAcikMi([S(SinavTuru.Final), S(SinavTuru.Butunleme)]));
        Assert.True(DerslerController.DonemDegerlendirmeAcikMi([S(SinavTuru.Vize), S(SinavTuru.Final)]));
        Assert.True(DerslerController.DonemDegerlendirmeAcikMi([S(SinavTuru.Vize), S(SinavTuru.Butunleme)]));
    }

    [Theory]
    [InlineData("2026-10-28", "2026-2027", Donem.Guz)]
    [InlineData("2027-01-15", "2026-2027", Donem.Guz)]
    [InlineData("2027-03-01", "2026-2027", Donem.Bahar)]
    [InlineData("2027-09-01", "2027-2028", Donem.Guz)]
    public void AkademikTakvim_TarihtenYilVeDonem(string tarih, string yil, Donem donem)
    {
        var t = DateTime.Parse(tarih);
        Assert.Equal(yil, AkademikTakvim.AkademikYil(t));
        Assert.Equal(donem, AkademikTakvim.Donem(t));
    }
}
