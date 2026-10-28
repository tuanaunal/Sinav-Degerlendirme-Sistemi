using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
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

    // ------------------------------------------------------------------ 5. adım

    [Fact]
    public async Task Adim5_KitapcikEslestirmeVeOgrenmeCiktilari_SinaviTamamlar()
    {
        var elif = await uygulama.GirisYapmisIstemciAsync("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*");
        var sinavId = await VizeyiHazirla(elif, "3301104"); // Ayrık Matematik: diğer testlerden bağımsız
        await EslestirmeyiTamamla(elif, sinavId);
        (await elif.PostAsync($"/api/sinavlar/{sinavId}/notlar/tamamla", null)).EnsureSuccessStatusCode();

        // Kitapçık eşleştirmesi kaydedilmeden öğrenme çıktısı ilişkilendirilemez
        Assert.Equal(HttpStatusCode.BadRequest,
            (await elif.PutAsJsonAsync($"/api/sinavlar/{sinavId}/ogrenme-ciktilari/eslestirme", new CiktiEslestirmeKaydi([], true))).StatusCode);

        // Doğru eşleştirme _dogrulama dosyasında: "B1  → A7"
        var bdenAya = File.ReadAllLines(Path.Combine(Ornek, "_dogrulama", "vize_cevap_anahtari.txt"))
            .Where(l => l.StartsWith('B') && l.Contains('→'))
            .Select(l => int.Parse(l.Split('→')[1].Trim()[1..]))
            .Select(x => (int?)x).ToList();
        Assert.Equal(25, bdenAya.Count);

        // Cevapları farklı iki soruyu yer değiştirmek kaydı engeller
        var anahtar = await elif.GetFromJsonAsync<CevapAnahtari>($"/api/sinavlar/{sinavId}/cevap-anahtari");
        var bAnahtar = anahtar!.Kitapciklar.Single(k => k.Kitapcik == 'B').Cevaplar;
        var j = Enumerable.Range(1, 24).First(i => bAnahtar[i] != bAnahtar[0]);
        var yanlis = bdenAya.ToList();
        (yanlis[0], yanlis[j]) = (yanlis[j], yanlis[0]);
        var red = await elif.PutAsJsonAsync($"/api/sinavlar/{sinavId}/kitapcik-eslestirme", new KitapcikEslestirmeKaydi([new('B', yanlis)]));
        Assert.Equal(HttpStatusCode.BadRequest, red.StatusCode);
        Assert.Contains("uyuşmuyor", await red.Content.ReadAsStringAsync());

        var kayit = await elif.PutAsJsonAsync($"/api/sinavlar/{sinavId}/kitapcik-eslestirme", new KitapcikEslestirmeKaydi([new('B', bdenAya)]));
        Assert.True(kayit.IsSuccessStatusCode, await kayit.Content.ReadAsStringAsync());

        // A numarasına çevrilmiş soru cevapları: bilerek eklenen kolay / zor / şüpheli sorular
        var db = uygulama.Veritabani();
        var cevaplar = db.SoruCevaplari.Where(c => c.SinavSonucu.SinavId == sinavId).ToList();
        double Oran(int soru) => cevaplar.Where(c => c.SoruNo == soru).Average(c => c.DogruMu == true ? 1.0 : 0.0);
        Assert.True(Oran(3) > 0.9, $"S3 {Oran(3):P0}");
        Assert.True(Oran(10) < 0.1, $"S10 {Oran(10):P0}");
        Assert.True(Oran(17) < 0.1, $"S17 {Oran(17):P0}");
        Assert.Equal(115 * 25, cevaplar.Count);

        // Öğrenme çıktıları: geçen yılki dönemden getir, sorulara bağla, tamamla
        var durum = await elif.GetFromJsonAsync<CiktiDurumu>($"/api/sinavlar/{sinavId}/ogrenme-ciktilari");
        Assert.Equal(0, durum!.GetirilebilecekSayisi); // Ayrık Matematik'in geçmiş dönemi yok
        durum = await (await elif.PostAsJsonAsync($"/api/sinavlar/{sinavId}/ogrenme-ciktilari", new YeniCikti("Önerme mantığını ve ispat yöntemlerini kullanır.")))
            .Content.ReadFromJsonAsync<CiktiDurumu>();
        durum = await (await elif.PostAsJsonAsync($"/api/sinavlar/{sinavId}/ogrenme-ciktilari", new YeniCikti("Kümeler, bağıntılar ve fonksiyonlarla ilgili problemleri çözer.")))
            .Content.ReadFromJsonAsync<CiktiDurumu>();
        Assert.Equal(2, durum!.Ciktilar.Count);
        Assert.Equal(HttpStatusCode.Conflict,
            (await elif.PostAsJsonAsync($"/api/sinavlar/{sinavId}/ogrenme-ciktilari", new YeniCikti("Önerme mantığını ve ispat yöntemlerini kullanır."))).StatusCode);

        var bitti = await elif.PutAsJsonAsync($"/api/sinavlar/{sinavId}/ogrenme-ciktilari/eslestirme", new CiktiEslestirmeKaydi(
            [new(durum.Ciktilar[0].Id, [.. Enumerable.Range(1, 12)]), new(durum.Ciktilar[1].Id, [.. Enumerable.Range(13, 13)])], true));
        Assert.True(bitti.IsSuccessStatusCode, await bitti.Content.ReadAsStringAsync());
        var son = await bitti.Content.ReadFromJsonAsync<CiktiDurumu>();
        Assert.Equal(12, son!.Ciktilar[0].Sorular.Count);

        var sinav = uygulama.Veritabani().Sinavlar.Single(x => x.Id == sinavId);
        Assert.Equal((SinavDurumu.Tamamlandi, 5), (sinav.Durum, sinav.TamamlananAdim));

        // Analiz raporu: özet, dağılım, şık analizi (17. soruda anahtar hatası şüphesi), çıktı başarısı
        var analiz = await elif.GetFromJsonAsync<SinavAnaliziYaniti>($"/api/sinavlar/{sinavId}/analiz");
        Assert.Equal((120, 115), (analiz!.Ozet.Toplam, analiz.Ozet.Giren));
        Assert.Equal(115, analiz.PuanDagilimi.Sum(d => d.Sayi));
        Assert.Equal(115, analiz.HarfDagilimi.Sum(h => h.Sayi));
        Assert.Equal(50, analiz.GecmeSiniri);
        var s17 = analiz.Sorular.Single(q => q.SoruNo == 17);
        Assert.Equal("D", s17.DogruCevap);
        Assert.True(s17.SikOranlari["B"] > 60, $"S17 B %{s17.SikOranlari["B"]}");
        Assert.Equal(100m, Math.Round(s17.SikOranlari.Values.Sum(), 0));
        Assert.Equal(2, analiz.Ciktilar.Count);
        Assert.All(analiz.Ciktilar, c => Assert.InRange(c.BasariOrani!.Value, 0, 100));

        var ece = analiz.Ogrenciler.Single(o => o.AdSoyad == "ECE ŞEN");
        var detay = await elif.GetFromJsonAsync<OgrenciAnalizi>($"/api/sinavlar/{sinavId}/analiz/ogrenci/{ece.SonucId}");
        Assert.Equal(('B', 84m, 25), (detay!.Kitapcik!.Value, detay.HamPuan, detay.Sorular.Count));
        Assert.Equal(21, detay.Sorular.Count(q => q.DogruMu == true));

        // Sınavın değerlendirmesi: kayıt yokken otomatik metin önerilir, S17'deki anahtar şüphesi metinde geçer
        var degerlendirme = await elif.GetFromJsonAsync<DegerlendirmeMetni>($"/api/sinavlar/{sinavId}/degerlendirme");
        Assert.Null(degerlendirme!.Kayitli);
        Assert.Contains("S17 sorusunda", degerlendirme.Otomatik);
        Assert.Contains("115 öğrenci katılmış", degerlendirme.Otomatik);

        // Resmi form, otomatik metinle: üç sayfa, aralık sayıları giren öğrenci sayısını verir
        var form = await elif.GetAsync($"/api/sinavlar/{sinavId}/resmi-form");
        Assert.True(form.IsSuccessStatusCode, await form.Content.ReadAsStringAsync());
        Assert.Equal("3301104_Vize_Sinav_Basarisi_Analiz_Formu.xlsx", form.Content.Headers.ContentDisposition!.FileNameStar);
        using (var wb = new XLWorkbook(await form.Content.ReadAsStreamAsync()))
        {
            Assert.Equal(["Analiz", "Liste", "İÖK Kağıt"], wb.Worksheets.Select(w => w.Name));
            var ws = wb.Worksheet("Analiz");
            Assert.Equal("SELÇUK ÜNİVERSİTESİ", ws.Cell(2, 2).GetString());
            Assert.Single(ws.Pictures);
            var aralik = ws.CellsUsed(c => c.GetString() == "ARALIK").Single().Address.RowNumber;
            Assert.Equal("88 - 100", ws.Cell(aralik + 1, 1).GetString());
            Assert.Equal("0 - 15", ws.Cell(aralik + 9, 1).GetString());
            Assert.Equal(115, Enumerable.Range(aralik + 1, 9).Sum(r => ws.Cell(r, 3).GetValue<int>()));
            Assert.Contains("S17 sorusunda", ws.CellsUsed(c => c.GetString().StartsWith("Sınava kayıtlı")).Single().GetString());

            var liste = wb.Worksheet("Liste");
            Assert.Equal(("S1", "S25"), (liste.Cell(10, 5).GetString(), liste.Cell(10, 29).GetString()));
            Assert.Equal(130, liste.LastRowUsed()!.RowNumber());
            Assert.Equal(5, liste.Column(4).CellsUsed(c => c.GetString() == "GİRMEDİ").Count());
            // İÖK: en yüksek, orta ve en düşük puanı alan öğrencilerin tamamı
            var wi = wb.Worksheet("İÖK Kağıt");
            var gruplar = wi.CellsUsed(c => c.GetString().StartsWith("DERSTE ")).Select(c => c.GetString()).ToList();
            Assert.Equal(3, gruplar.Count);
            Assert.Contains($"({(int)Math.Round(analiz.Ozet.EnYuksek!.Value, MidpointRounding.AwayFromZero)} Puan)", gruplar[0]);
            Assert.True(wi.Column(2).CellsUsed(c => c.GetString().Length == 9 && c.GetString().All(char.IsDigit)).Count() >= 3);
            Assert.Equal("TF-BM-SBA-02", wi.Cell(1, wi.CellsUsed(c => c.GetString() == "Doküman No").Single().Address.ColumnNumber + 1).GetString());

            // Soru - öğrenme çıktısı matrisi: her ilişkilendirme bir "X"
            Assert.Equal(analiz.Ciktilar.Sum(c => c.Sorular.Count), ws.CellsUsed(c => c.GetString() == "X").Count());
        }

        // Öğretim üyesinin metni kaydedilir ve forma o yazılır
        (await elif.PutAsJsonAsync($"/api/sinavlar/{sinavId}/degerlendirme", new DegerlendirmeKaydi("  Sınav genel olarak başarılı geçmiştir.  "))).EnsureSuccessStatusCode();
        Assert.Equal("Sınav genel olarak başarılı geçmiştir.", (await elif.GetFromJsonAsync<DegerlendirmeMetni>($"/api/sinavlar/{sinavId}/degerlendirme"))!.Kayitli);
        using (var wb = new XLWorkbook(await (await elif.GetAsync($"/api/sinavlar/{sinavId}/resmi-form")).Content.ReadAsStreamAsync()))
            Assert.Single(wb.Worksheet("Analiz").CellsUsed(c => c.GetString() == "Sınav genel olarak başarılı geçmiştir."));
    }

    [Fact]
    public async Task OgrenmeCiktilari_GecenDonemdenGetirilir()
    {
        var elif = await uygulama.GirisYapmisIstemciAsync("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*");
        var sinavId = await VizeyiHazirla(elif);

        var durum = await elif.GetFromJsonAsync<CiktiDurumu>($"/api/sinavlar/{sinavId}/ogrenme-ciktilari");
        Assert.Equal((5, "2025-2026 Güz"), (durum!.GetirilebilecekSayisi, durum.GetirilecekDonem));

        durum = await (await elif.PostAsync($"/api/sinavlar/{sinavId}/ogrenme-ciktilari/getir", null)).Content.ReadFromJsonAsync<CiktiDurumu>();
        Assert.Equal(5, durum!.Ciktilar.Count);
        Assert.Equal(0, durum.GetirilebilecekSayisi);
        Assert.Equal([1, 2, 3, 4, 5], durum.Ciktilar.Select(c => c.SiraNo));
    }

    private static async Task EslestirmeyiTamamla(HttpClient istemci, int sinavId)
    {
        var d = await istemci.GetFromJsonAsync<EslestirmeDurumu>($"/api/sinavlar/{sinavId}/eslestirme");
        foreach (var k in d!.BekleyenKagitlar)
            if (k.OneriSonucId is { } oneri) await istemci.PostAsJsonAsync($"/api/sinavlar/{sinavId}/eslestirme/eslestir", new ElleEslestir(k.Id, oneri));
            else await istemci.PostAsync($"/api/sinavlar/{sinavId}/eslestirme/gozardi/{k.Id}", null);
        d = await istemci.GetFromJsonAsync<EslestirmeDurumu>($"/api/sinavlar/{sinavId}/eslestirme");
        foreach (var e in d!.Eslesenler.Where(e => e.Kitapcik is null))
            await istemci.PostAsJsonAsync($"/api/sinavlar/{sinavId}/eslestirme/kitapcik", new KitapcikSec(e.SonucId, 'B'));
        (await istemci.PostAsync($"/api/sinavlar/{sinavId}/eslestirme/tamamla", null)).EnsureSuccessStatusCode();
    }

    /// <summary>Örnek vize dosyalarını yükler ve optikteki anahtar satırlarıyla cevap anahtarını kaydeder.</summary>
    private async Task<int> VizeyiHazirla(HttpClient istemci, string dersKodu = "3304701")
    {
        var dersId = uygulama.Veritabani().Dersler.Single(d => d.Kod == dersKodu && d.AkademikYil == "2026-2027").Id;
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
