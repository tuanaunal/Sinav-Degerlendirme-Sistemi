using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sds.Api.Controllers;
using Sds.Api.Domain;

namespace Sds.Tests;

/// <summary>Çoktan seçmeli sınav girişinin 1. (dosya yükleme) ve 2. (cevap anahtarı) adımları.</summary>
public class SinavGirisiTestleri(TestUygulamasi uygulama) : IClassFixture<TestUygulamasi>
{
    private static readonly string Ornek = Path.Combine(KokKlasor(), "ornek-veriler");

    private static string KokKlasor()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Sds.sln"))) d = d.Parent;
        return d!.FullName;
    }

    private Task<HttpClient> Elif() => uygulama.GirisYapmisIstemciAsync("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*");
    private int DersId(string kod) => uygulama.Veritabani().Dersler.Single(d => d.Kod == kod && d.AkademikYil == "2026-2027").Id;

    private static MultipartFormDataContent Form(string optik, params string[] listeler)
    {
        var form = new MultipartFormDataContent { { new StringContent("CoktanSecmeli"), "format" } };
        form.Add(Dosya(optik), "optik", optik);
        foreach (var l in listeler) form.Add(Dosya(l), "sinifListeleri", l);
        return form;

        static ByteArrayContent Dosya(string ad)
        {
            var c = new ByteArrayContent(File.ReadAllBytes(Path.Combine(Ornek, ad)));
            c.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return c;
        }
    }

    private async Task<SinavBilgisi> VizeYukle(HttpClient istemci, int dersId)
    {
        var yanit = await istemci.PostAsync($"/api/dersler/{dersId}/sinavlar/vize/dosyalar",
            Form("Optik_3304701_Vize.txt", "OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx"));
        Assert.True(yanit.IsSuccessStatusCode, await yanit.Content.ReadAsStringAsync());
        return (await yanit.Content.ReadFromJsonAsync<SinavBilgisi>())!;
    }

    [Fact]
    public async Task DosyaYukleme_SinaviOlusturur_OgrencileriVeOptikKayitlariniSaklar()
    {
        var elif = await Elif();
        var dersId = DersId("3304701");

        Assert.Equal(HttpStatusCode.NotFound, (await elif.GetAsync($"/api/dersler/{dersId}/sinavlar/final")).StatusCode);

        var s = await VizeYukle(elif, dersId);

        Assert.Equal((1, 25, 2, 120, 116), (s.TamamlananAdim, s.SoruSayisi, s.KitapcikSayisi, s.SinifMevcudu, s.OptikKagitSayisi));
        Assert.Contains(s.Uyarilar, u => u.Contains("kitapçık türü boş"));

        var db = uygulama.Veritabani();
        Assert.Equal(118, db.OptikKayitlari.Count(o => o.SinavId == s.Id));
        Assert.Equal(2, db.OptikKayitlari.Count(o => o.SinavId == s.Id && o.CevapAnahtariMi));
        Assert.True(db.SinavSonuclari.Where(r => r.SinavId == s.Id).All(r => r.Eslesme == EslesmeDurumu.Bekliyor));
        Assert.Equal("AYŞE NAZ YALÇIN", db.Ogrenciler.Single(o => o.OgrenciNo == "233301032").AdSoyad);

        // Aynı dosyalar tekrar yüklenince kayıtlar çoğalmaz
        await VizeYukle(elif, dersId);
        Assert.Equal(120, uygulama.Veritabani().SinavSonuclari.Count(r => r.SinavId == s.Id));

        var getir = await elif.GetFromJsonAsync<SinavBilgisi>($"/api/dersler/{dersId}/sinavlar/vize");
        Assert.Equal(s.Id, getir!.Id);
    }

    [Fact]
    public async Task DosyaYukleme_EksikVeyaYanlisDosyadaHataVerir()
    {
        var elif = await Elif();
        var dersId = DersId("3301104");

        var yanlis = await elif.PostAsync($"/api/dersler/{dersId}/sinavlar/vize/dosyalar",
            Form("OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx", "OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx"));
        Assert.Equal(HttpStatusCode.BadRequest, yanlis.StatusCode);

        var listesiz = new MultipartFormDataContent { { new StringContent("CoktanSecmeli"), "format" } };
        listesiz.Add(new ByteArrayContent(File.ReadAllBytes(Path.Combine(Ornek, "Optik_3304701_Vize.txt"))), "optik", "o.txt");
        Assert.Equal(HttpStatusCode.BadRequest, (await elif.PostAsync($"/api/dersler/{dersId}/sinavlar/vize/dosyalar", listesiz)).StatusCode);

        // Başkasının dersine yüklenemez
        var veri = DersId("3302205");
        Assert.Equal(HttpStatusCode.NotFound, (await elif.PostAsync($"/api/dersler/{veri}/sinavlar/vize/dosyalar",
            Form("Optik_3304701_Vize.txt", "OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx"))).StatusCode);
    }

    [Fact]
    public async Task DosyaYukleme_FarkliSinavTuruKoduIcinUyarir()
    {
        var elif = await Elif();
        var dersId = DersId("3301104");

        var yanit = await elif.PostAsync($"/api/dersler/{dersId}/sinavlar/final/dosyalar",
            Form("Optik_3304701_Vize.txt", "OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx"));
        var s = await yanit.Content.ReadFromJsonAsync<SinavBilgisi>();

        Assert.Contains(s!.Uyarilar, u => u.Contains("sınav türü 2 değil"));
    }

    [Fact]
    public async Task CevapAnahtari_OptiktenSecilipKaydedilir()
    {
        var elif = await Elif();
        var s = await VizeYukle(elif, DersId("3304701"));

        var veri = await elif.GetFromJsonAsync<CevapAnahtari>($"/api/sinavlar/{s.Id}/cevap-anahtari");
        Assert.Equal(25, veri!.Sorular.Count);
        Assert.All(veri.Sorular, q => Assert.Equal(4m, q.Puan));
        Assert.Equal("(A Kitapçık) CEVAP ANAHTARI", veri.OptikSecenekleri[0].Etiket);
        Assert.Contains(veri.OptikSecenekleri, o => o.Etiket.StartsWith("Numarasız - MERYEM YAMAN"));

        // Kullanıcı "Optikten seç" ile anahtar satırlarını seçti
        var anahtarlar = veri.OptikSecenekleri.Where(o => o.CevapAnahtariMi).ToDictionary(o => o.Kitapcik!.Value, o => o.Cevaplar);
        var beklenen = File.ReadAllLines(Path.Combine(Ornek, "_dogrulama", "vize_cevap_anahtari.txt"));
        Assert.Contains($"A kitapçığı: {anahtarlar['A'].Trim()}", beklenen);

        var kayit = new CevapAnahtariKaydi(25, 100, 2, true, false, [],
            anahtarlar.OrderBy(a => a.Key).Select(a => new KitapcikCevaplari(a.Key, a.Value.Select(c => c.ToString()).ToList())).ToList());
        var yanit = await elif.PutAsJsonAsync($"/api/sinavlar/{s.Id}/cevap-anahtari", kayit);
        Assert.True(yanit.IsSuccessStatusCode, await yanit.Content.ReadAsStringAsync());
        Assert.Equal(2, (await yanit.Content.ReadFromJsonAsync<SinavBilgisi>())!.TamamlananAdim);

        var db = uygulama.Veritabani();
        Assert.Equal(50, db.KitapcikSorulari.Count(k => k.SinavId == s.Id));
        Assert.All(db.KitapcikSorulari.Where(k => k.SinavId == s.Id && k.Kitapcik == 'A'), k => Assert.Equal(k.SoruNo, k.AnaSoruNo));
        Assert.Equal(100m, db.SinavSorulari.Where(q => q.SinavId == s.Id).AsEnumerable().Sum(q => q.Puan));

        // Tekrar kaydetmek kayıtları çoğaltmaz; geri dönüldüğünde girilen anahtar gelir
        await elif.PutAsJsonAsync($"/api/sinavlar/{s.Id}/cevap-anahtari", kayit);
        var tekrar = await elif.GetFromJsonAsync<CevapAnahtari>($"/api/sinavlar/{s.Id}/cevap-anahtari");
        Assert.Equal(anahtarlar['B'][0].ToString(), tekrar!.Kitapciklar[1].Cevaplar[0]);
    }

    [Fact]
    public void CevapAnahtari_Dogrulama()
    {
        List<KitapcikCevaplari> Tek(params string[] c) => [new('A', c.ToList())];

        Assert.Null(SinavlarController.CevapAnahtariniDogrula(new(3, 100, 1, true, false, [], Tek("A", "B", "C"))));
        Assert.Contains("girilmemiş", SinavlarController.CevapAnahtariniDogrula(new(3, 100, 1, true, false, [], Tek("A", "", "C"))));
        Assert.Contains("birden fazla", SinavlarController.CevapAnahtariniDogrula(new(3, 100, 1, true, false, [], Tek("A", "BD", "C"))));
        Assert.Null(SinavlarController.CevapAnahtariniDogrula(new(3, 100, 1, true, true, [], Tek("A", "BD", "C"))));
        Assert.Contains("geçersiz", SinavlarController.CevapAnahtariniDogrula(new(3, 100, 1, true, false, [], Tek("A", "F", "C"))));
        Assert.Contains("B kitapçıklarının", SinavlarController.CevapAnahtariniDogrula(new(3, 100, 2, true, false, [], Tek("A", "B", "C"))));

        var puanlar = new List<SoruPuani> { new(1, 50), new(2, 30), new(3, 10) };
        Assert.Contains("toplamı (90)", SinavlarController.CevapAnahtariniDogrula(new(3, 100, 1, false, false, puanlar, Tek("A", "B", "C"))));
    }

    [Theory]
    [InlineData(100, 25, 4.00, 4.00)]
    [InlineData(100, 30, 3.33, 3.34)]
    [InlineData(100, 3, 33.33, 33.34)]
    public void EsitDagitim_ToplamMaksimumaEsittir(decimal maks, int n, decimal ilk, decimal son)
    {
        var p = PuanDagitimi.EsitDagit(maks, n);
        Assert.Equal(maks, p.Sum());
        Assert.Equal((ilk, son), (p[0], p[^1]));
    }
}
