using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Sds.Api.Controllers;
using Sds.Api.Degerlendirme;
using Sds.Api.Domain;

namespace Sds.Tests;

/// <summary>3. adım (eşleştirme), puanlama motoru ve 4. adım (not düzenleme).</summary>
public class DegerlendirmeTestleri(TestUygulamasi uygulama) : IClassFixture<TestUygulamasi>
{
    private static readonly string Ornek = Path.Combine(KokKlasor(), "ornek-veriler");

    private static string KokKlasor()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Sds.sln"))) d = d.Parent;
        return d!.FullName;
    }

    // ------------------------------------------------------------------ Saf fonksiyonlar

    [Fact]
    public void Eslestirici_BirebirVeTekHaneFarkliNumaralariEslestirir()
    {
        List<EslestirmeOgrencisi> sinif = [new(1, "233301032", "AYŞE NAZ YALÇIN"), new(2, "233301110", "EMRE BAYRAM"), new(3, "233301103", "MERYEM YAMAN"), new(4, "233301200", "ALİ VELİ")];
        List<EslestirmeKagidi> kagitlar = [new(10, "133301032", "AYŞE NAZ YALÇIN"), new(11, "233307110", "EMRE BAYRAM"), new(12, "", "MERYEM YAMAN"), new(13, "233301200", "ALİ VELİ"), new(14, "233302014", "BURAK ŞAHİN")];

        var e = Eslestirici.Eslestir(sinif, kagitlar);

        Assert.Equal([(10, 1, true), (11, 2, true), (13, 4, false)], e.Select(x => (x.KagitId, x.OgrenciId, x.NumaraFarkli)).OrderBy(x => x.Item1));
        Assert.Equal(3, Eslestirici.Oner(kagitlar[2], [sinif[2]])!.Id); // numarasız kağıt isimden önerilir
        Assert.Null(Eslestirici.Oner(kagitlar[4], [sinif[2]]));
    }

    [Fact]
    public void Eslestirici_BelirsizTekHaneFarkiniEslestirmez()
    {
        // 233301005 iki öğrenciye de tek hane uzak: hangisi olduğu bilinemez
        List<EslestirmeOgrencisi> sinif = [new(1, "233301004", "A"), new(2, "233301006", "B")];
        Assert.Empty(Eslestirici.Eslestir(sinif, [new(10, "233301005", "C")]));
        Assert.True(Eslestirici.TekHaneFarkli("23330110", "233301110")); // bir hane eksik kodlanmış
    }

    [Fact]
    public void PuanHesaplayici_BosCokluIptalVeKitapcikEslestirmesi()
    {
        List<SinavSorusu> sorular = [new() { SoruNo = 1, Puan = 25 }, new() { SoruNo = 2, Puan = 25 }, new() { SoruNo = 3, Puan = 25, Iptal = true }, new() { SoruNo = 4, Puan = 25 }];
        List<KitapcikSorusu> anahtar =
        [
            new() { Kitapcik = 'A', SoruNo = 1, AnaSoruNo = 1, DogruCevap = "A" }, new() { Kitapcik = 'A', SoruNo = 2, AnaSoruNo = 2, DogruCevap = "BC" },
            new() { Kitapcik = 'A', SoruNo = 3, AnaSoruNo = 3, DogruCevap = "C" }, new() { Kitapcik = 'A', SoruNo = 4, AnaSoruNo = 4, DogruCevap = "D" },
            // B kitapçığı: 1. soru A'nın 4. sorusu, 4. soru A'nın 1. sorusu
            new() { Kitapcik = 'B', SoruNo = 1, AnaSoruNo = 4, DogruCevap = "D" }, new() { Kitapcik = 'B', SoruNo = 2, AnaSoruNo = 2, DogruCevap = "BC" },
            new() { Kitapcik = 'B', SoruNo = 3, AnaSoruNo = 3, DogruCevap = "C" }, new() { Kitapcik = 'B', SoruNo = 4, AnaSoruNo = 1, DogruCevap = "A" },
        ];

        var a = PuanHesaplayici.Hesapla("AC E", 'A', anahtar, sorular);   // doğru, doğru (çoklu kabul), iptal(boş da olsa), yanlış
        Assert.Equal((3, 1, 0, 75m), (a.Dogru, a.Yanlis, a.Bos, a.HamPuan));

        var b = PuanHesaplayici.Hesapla("D* A", 'B', anahtar, sorular);
        Assert.Equal((3, 1, 0, 75m), (b.Dogru, b.Yanlis, b.Bos, b.HamPuan));
        Assert.Equal([1, 2, 3, 4], b.Sorular.Select(s => s.SoruNo)); // A numarasına çevrildi
        Assert.True(b.Sorular[3].DogruMu); // B'nin 1. sorusu = A'nın 4. sorusu

        var bos = PuanHesaplayici.Hesapla("  ", 'A', anahtar, sorular); // kısa satır: kalan sorular boş
        Assert.Equal((1, 0, 3, 25m), (bos.Dogru, bos.Yanlis, bos.Bos, bos.HamPuan));
    }

    [Fact]
    public void Istatistik_OrtalamaMedyanStandartSapma()
    {
        var o = Istatistik.Hesapla(5, [40m, 60m, 80m, 100m]);
        Assert.Equal((5, 4, 70m, 70m, 22.36m, 100m, 40m), (o.Toplam, o.Giren, o.Ortalama!.Value, o.Medyan!.Value, o.StandartSapma!.Value, o.EnYuksek!.Value, o.EnDusuk!.Value));
        Assert.Null(Istatistik.Hesapla(3, []).Ortalama);
    }

    [Theory]
    [InlineData(87.5, "AA")] // 88'e yuvarlanır
    [InlineData(87.4, "BA")]
    [InlineData(49.4, "FF")]
    [InlineData(49.5, "DD")]
    [InlineData(100, "AA")]
    [InlineData(0, "FF")]
    public void HarfNotu_YuvarlanmisPuandanBulunur(double puan, string beklenen)
    {
        List<HarfNotuAraligi> h =
        [
            new() { Harf = "AA", AltSinir = 88, UstSinir = 100 }, new() { Harf = "BA", AltSinir = 80, UstSinir = 87 },
            new() { Harf = "DD", AltSinir = 50, UstSinir = 54 }, new() { Harf = "FF", AltSinir = 0, UstSinir = 49 },
        ];
        Assert.Equal(beklenen, HarfNotu.Bul((decimal)puan, h)!.Harf);
    }

    // ------------------------------------------------------------------ Tam akış (örnek vize)

    [Fact]
    public async Task TamAkis_VizePuanlariBeklenenSonuclarlaBirebirAyni()
    {
        var elif = await uygulama.GirisYapmisIstemciAsync("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*");
        var sinavId = await VizeyiHazirla(elif);

        // Otomatik eşleştirme
        var durum = await elif.GetFromJsonAsync<EslestirmeDurumu>($"/api/sinavlar/{sinavId}/eslestirme");
        Assert.Equal(114, durum!.Eslesenler.Count);
        Assert.Equal(2, durum.Eslesenler.Count(e => e.NumaraFarkli));
        Assert.Equal(["", "233302014"], durum.BekleyenKagitlar.Select(k => k.OgrenciNo).Order());
        var meryem = durum.BekleyenKagitlar.Single(k => k.OgrenciNo == "");
        Assert.Equal("MERYEM YAMAN", meryem.OneriAdSoyad);
        Assert.Equal(6, durum.BekleyenOgrenciler.Count); // 5 girmeyen + numarasını kodlamayan

        // Bekleyen kağıt varken tamamlanamaz
        Assert.Equal(HttpStatusCode.BadRequest, (await elif.PostAsync($"/api/sinavlar/{sinavId}/eslestirme/tamamla", null)).StatusCode);

        // Elle eşleştir, misafiri göz ardı et, boş kitapçığı seç
        await elif.PostAsJsonAsync($"/api/sinavlar/{sinavId}/eslestirme/eslestir", new ElleEslestir(meryem.Id, meryem.OneriSonucId!.Value));
        durum = await (await elif.PostAsync($"/api/sinavlar/{sinavId}/eslestirme/gozardi/{durum.BekleyenKagitlar.Single(k => k.OgrenciNo == "233302014").Id}", null))
            .Content.ReadFromJsonAsync<EslestirmeDurumu>();
        Assert.Equal(HttpStatusCode.BadRequest, (await elif.PostAsync($"/api/sinavlar/{sinavId}/eslestirme/tamamla", null)).StatusCode); // kitapçık boş
        var ece = durum!.Eslesenler.Single(e => e.Kitapcik is null);
        await elif.PostAsJsonAsync($"/api/sinavlar/{sinavId}/eslestirme/kitapcik", new KitapcikSec(ece.SonucId, 'B'));

        var tamam = await elif.PostAsync($"/api/sinavlar/{sinavId}/eslestirme/tamamla", null);
        Assert.True(tamam.IsSuccessStatusCode, await tamam.Content.ReadAsStringAsync());

        // Puanlar beklenen sonuçlarla karşılaştırılır
        var notlar = await elif.GetFromJsonAsync<NotListesi>($"/api/sinavlar/{sinavId}/notlar");
        var satir = notlar!.Satirlar.ToDictionary(s => s.OgrenciNo);
        var kontrol = 0;
        foreach (var p in File.ReadAllLines(Path.Combine(Ornek, "_dogrulama", "vize_beklenen_sonuclar.csv")).Skip(1).Select(l => l.Split(';')))
        {
            if (!satir.TryGetValue(p[0], out var s)) continue; // sınıf listesinde olmayan
            if (p[3].Length == 0) { Assert.False(s.SinavaGirdi, p[0]); continue; }
            Assert.True(s.SinavaGirdi, p[0]);
            Assert.Equal((int.Parse(p[3]), int.Parse(p[4]), int.Parse(p[5]), decimal.Parse(p[6], CultureInfo.InvariantCulture)),
                (s.Dogru, s.Yanlis, s.Bos, s.HamPuan));
            kontrol++;
        }
        Assert.Equal(115, kontrol);
        Assert.Equal((120, 115), (notlar.Ozet.Toplam, notlar.Ozet.Giren));

        // 4. adım: proje notları ve bonus
        var proje = new MultipartFormDataContent();
        var dosya = new ByteArrayContent(File.ReadAllBytes(Path.Combine(Ornek, "Proje_Notlari_3304701.xlsx")));
        dosya.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        proje.Add(dosya, "dosya", "Proje_Notlari_3304701.xlsx");
        var py = await (await elif.PostAsync($"/api/sinavlar/{sinavId}/notlar/proje", proje)).Content.ReadFromJsonAsync<ProjeYuklemeSonucu>();
        Assert.True(py!.Eklenen > 60);
        Assert.Empty(py.BulunamayanNumaralar);

        var hedef = notlar.Satirlar.First(s => s.SinavaGirdi && s.HamPuan < 80);
        var bonuslu = await (await elif.PostAsJsonAsync($"/api/sinavlar/{sinavId}/notlar/bonus", new BonusIstegi([hedef.SonucId], 5, "Ödev"))).Content.ReadFromJsonAsync<NotListesi>();
        var yeni = bonuslu!.Satirlar.Single(s => s.SonucId == hedef.SonucId);
        Assert.Equal(hedef.HamPuan + yeni.Eklemeler.Sum(e => e.Puan), yeni.DuzeltilmisPuan);
        Assert.Contains(yeni.Eklemeler, e => e.Tur == "Bonus" && e.Puan == 5);

        // Elle düzeltme ham puanın yerine geçer, null ile geri alınır
        var manuel = await (await elif.PutAsJsonAsync($"/api/sinavlar/{sinavId}/notlar/{hedef.SonucId}/manuel", new ManuelPuanIstegi(90))).Content.ReadFromJsonAsync<NotListesi>();
        Assert.Equal(Math.Min(100, 90 + yeni.Eklemeler.Sum(e => e.Puan)), manuel!.Satirlar.Single(s => s.SonucId == hedef.SonucId).DuzeltilmisPuan);
        Assert.Equal(HttpStatusCode.BadRequest, (await elif.PutAsJsonAsync($"/api/sinavlar/{sinavId}/notlar/{hedef.SonucId}/manuel", new ManuelPuanIstegi(150))).StatusCode);

        Assert.True((await elif.PostAsync($"/api/sinavlar/{sinavId}/notlar/tamamla", null)).IsSuccessStatusCode);
    }

    /// <summary>Örnek vize dosyalarını yükler ve optikteki anahtar satırlarıyla cevap anahtarını kaydeder.</summary>
    private async Task<int> VizeyiHazirla(HttpClient istemci)
    {
        var dersId = uygulama.Veritabani().Dersler.Single(d => d.Kod == "3304701").Id;
        var form = new MultipartFormDataContent { { new StringContent("CoktanSecmeli"), "format" } };
        foreach (var (alan, ad) in new[] { ("optik", "Optik_3304701_Vize.txt"), ("sinifListeleri", "OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx") })
        {
            var c = new ByteArrayContent(File.ReadAllBytes(Path.Combine(Ornek, ad)));
            c.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(c, alan, ad);
        }
        var sinav = await (await istemci.PostAsync($"/api/dersler/{dersId}/sinavlar/vize/dosyalar", form)).Content.ReadFromJsonAsync<SinavBilgisi>();

        var anahtar = await istemci.GetFromJsonAsync<CevapAnahtari>($"/api/sinavlar/{sinav!.Id}/cevap-anahtari");
        var kitapciklar = anahtar!.OptikSecenekleri.Where(o => o.CevapAnahtariMi).OrderBy(o => o.Kitapcik)
            .Select(o => new KitapcikCevaplari(o.Kitapcik!.Value, o.Cevaplar.Select(c => c.ToString()).ToList())).ToList();
        (await istemci.PutAsJsonAsync($"/api/sinavlar/{sinav.Id}/cevap-anahtari", new CevapAnahtariKaydi(25, 100, 2, true, false, [], kitapciklar)))
            .EnsureSuccessStatusCode();
        return sinav.Id;
    }
}
