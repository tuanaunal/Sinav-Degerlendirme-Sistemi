using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Sds.Api.Controllers;
using Sds.Api.Domain;

namespace Sds.Tests;

/// <summary>Açık uçlu sınav akışı (örnek final), Excel'e aktarma ve sınav silme.</summary>
public class AcikUcluTestleri(TestUygulamasi uygulama) : IClassFixture<TestUygulamasi>
{
    private static readonly string Ornek = Path.Combine(KokKlasor(), "ornek-veriler");

    private static string KokKlasor()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Sds.sln"))) d = d.Parent;
        return d!.FullName;
    }

    private static ByteArrayContent Dosya(string ad)
    {
        var c = new ByteArrayContent(File.ReadAllBytes(Path.Combine(Ornek, ad)));
        c.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return c;
    }

    private int DersId(string kod) => uygulama.Veritabani().Dersler.Single(d => d.Kod == kod && d.AkademikYil == "2026-2027").Id;

    private async Task<(HttpClient Istemci, int SinavId)> FinaliBaslat(string dersKodu)
    {
        var elif = await uygulama.GirisYapmisIstemciAsync("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*");
        var form = new MultipartFormDataContent { { new StringContent("AcikUclu"), "format" } };
        form.Add(Dosya("OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx"), "sinifListeleri", "liste.xlsx");
        var yanit = await elif.PostAsync($"/api/dersler/{DersId(dersKodu)}/sinavlar/final/dosyalar", form);
        Assert.True(yanit.IsSuccessStatusCode, await yanit.Content.ReadAsStringAsync());
        var s = await yanit.Content.ReadFromJsonAsync<SinavBilgisi>();
        Assert.Equal(("AcikUclu", 120, 0), (s!.Format, s.SinifMevcudu, s.OptikKagitSayisi));
        return (elif, s.Id);
    }

    [Fact]
    public async Task TamAkis_FinalPuanlariBeklenenSonuclarlaAyni()
    {
        var (elif, id) = await FinaliBaslat("3304701");

        // 2. adım: 10 soru × 10 puan
        var ayar = await elif.GetFromJsonAsync<SoruAyarlari>($"/api/sinavlar/{id}/acik-uclu/soru-ayarlari");
        Assert.Equal(10, ayar!.SoruSayisi);
        (await elif.PutAsJsonAsync($"/api/sinavlar/{id}/acik-uclu/soru-ayarlari", new SoruAyarlari(10, 100, true, []))).EnsureSuccessStatusCode();

        // Şablon: sınıf listesinden S1…S10 sütunlu boş Excel
        var sablon = await elif.GetAsync($"/api/sinavlar/{id}/acik-uclu/sablon");
        using (var wb = new XLWorkbook(await sablon.Content.ReadAsStreamAsync()))
        {
            var ws = wb.Worksheet(1);
            Assert.Equal(("OGRENCINO", "S10"), (ws.Cell(1, 1).GetString(), ws.Cell(1, 12).GetString()));
            Assert.Equal(121, ws.LastRowUsed()!.RowNumber());
        }

        // 3. adım: örnek final notları
        var form = new MultipartFormDataContent { { Dosya("Final_Acik_Uclu_Notlar_3304701.xlsx"), "dosya", "final.xlsx" } };
        var yukleme = await elif.PostAsync($"/api/sinavlar/{id}/acik-uclu/notlar", form);
        Assert.True(yukleme.IsSuccessStatusCode, await yukleme.Content.ReadAsStringAsync());
        var sonuc = await yukleme.Content.ReadFromJsonAsync<NotYuklemeSonucu>();
        Assert.Equal((116, 4), (sonuc!.Giren, sonuc.Girmeyen));
        Assert.Empty(sonuc.ListedeOlmayanlar);

        // 4. adım: toplamlar doğrulama dosyasıyla aynı, harf notu dolu
        var notlar = await elif.GetFromJsonAsync<NotListesi>($"/api/sinavlar/{id}/notlar");
        var beklenen = File.ReadAllLines(Path.Combine(Ornek, "_dogrulama", "final_beklenen_sonuclar.csv")).Skip(1)
            .Select(l => l.Split(';')).ToDictionary(p => p[0], p => p[^1]);
        foreach (var s in notlar!.Satirlar)
        {
            if (!char.IsDigit(beklenen[s.OgrenciNo].FirstOrDefault())) { Assert.False(s.SinavaGirdi); continue; }
            Assert.Equal(decimal.Parse(beklenen[s.OgrenciNo], CultureInfo.InvariantCulture), s.HamPuan);
            Assert.NotNull(s.HarfNotu);
        }

        // Excel'e aktar
        var excel = await elif.GetAsync($"/api/sinavlar/{id}/notlar/excel");
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excel.Content.Headers.ContentType!.MediaType);
        using (var wb = new XLWorkbook(await excel.Content.ReadAsStreamAsync()))
            Assert.Equal(121, wb.Worksheet("SinavSonuclari").LastRowUsed()!.RowNumber());

        // 4-5. adımlar ortak: tamamla, öğrenme çıktısı, analiz
        (await elif.PostAsync($"/api/sinavlar/{id}/notlar/tamamla", null)).EnsureSuccessStatusCode();
        var cikti = await (await elif.PostAsync($"/api/sinavlar/{id}/ogrenme-ciktilari/getir", null)).Content.ReadFromJsonAsync<CiktiDurumu>();
        var son = await elif.PutAsJsonAsync($"/api/sinavlar/{id}/ogrenme-ciktilari/eslestirme",
            new CiktiEslestirmeKaydi([new(cikti!.Ciktilar[0].Id, [1, 2, 3])], true));
        Assert.True(son.IsSuccessStatusCode, await son.Content.ReadAsStringAsync());
        Assert.Equal(SinavDurumu.Tamamlandi, uygulama.Veritabani().Sinavlar.Single(s => s.Id == id).Durum);

        // Açık uçlu analiz: soru başarısı = ortalama puan / soru puanı, şık analizi yok
        var analiz = await elif.GetFromJsonAsync<SinavAnaliziYaniti>($"/api/sinavlar/{id}/analiz");
        Assert.Equal(116, analiz!.HarfDagilimi.Sum(h => h.Sayi));
        Assert.Equal(10, analiz.Sorular.Count);
        Assert.All(analiz.Sorular, q => { Assert.InRange(q.BasariOrani, 0, 100); Assert.Empty(q.SikOranlari); });
        // Eklemesiz sınavda soru ortalamalarının toplamı sınav ortalamasıdır (soru ortalamaları 2 basamağa yuvarlanır)
        Assert.InRange(Math.Abs(analiz.Sorular.Sum(q => q.OrtalamaPuan) - analiz.Ozet.Ortalama!.Value), 0, 0.1m);

        // Düzenleme modunda yeni sınıf listesi yüklemek sınavı taslağa döndürür, soru ayarları korunur
        var yeniden = new MultipartFormDataContent { { new StringContent("AcikUclu"), "format" } };
        yeniden.Add(Dosya("OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx"), "sinifListeleri", "liste.xlsx");
        var y = await (await elif.PostAsync($"/api/dersler/{DersId("3304701")}/sinavlar/final/dosyalar", yeniden)).Content.ReadFromJsonAsync<SinavBilgisi>();
        Assert.Equal(("Taslak", 1, 10), (y!.Durum, y.TamamlananAdim, y.SoruSayisi));
    }

    [Fact]
    public async Task NotYukleme_HataliDosyalarReddedilir()
    {
        var (elif, id) = await FinaliBaslat("3301104");

        // Soru sayısı kaydedilmeden not yüklenemez
        var once = new MultipartFormDataContent { { Dosya("Final_Acik_Uclu_Notlar_3304701.xlsx"), "dosya", "f.xlsx" } };
        Assert.Equal(HttpStatusCode.BadRequest, (await elif.PostAsync($"/api/sinavlar/{id}/acik-uclu/notlar", once)).StatusCode);

        // 12 soru ayarlıysa dosyada S11-S12 eksik
        (await elif.PutAsJsonAsync($"/api/sinavlar/{id}/acik-uclu/soru-ayarlari", new SoruAyarlari(12, 120, true, []))).EnsureSuccessStatusCode();
        var eksik = await elif.PostAsync($"/api/sinavlar/{id}/acik-uclu/notlar", new MultipartFormDataContent { { Dosya("Final_Acik_Uclu_Notlar_3304701.xlsx"), "dosya", "f.xlsx" } });
        Assert.Contains("S11", await eksik.Content.ReadAsStringAsync());

        // 10 soru × 5 puan: dosyadaki 10'luk puanlar soru puanını aşar
        (await elif.PutAsJsonAsync($"/api/sinavlar/{id}/acik-uclu/soru-ayarlari", new SoruAyarlari(10, 50, true, []))).EnsureSuccessStatusCode();
        var asan = await elif.PostAsync($"/api/sinavlar/{id}/acik-uclu/notlar", new MultipartFormDataContent { { Dosya("Final_Acik_Uclu_Notlar_3304701.xlsx"), "dosya", "f.xlsx" } });
        Assert.Equal(HttpStatusCode.BadRequest, asan.StatusCode);
        Assert.Contains("aşan", await asan.Content.ReadAsStringAsync());

        // El ile puan dağılımı toplamı tutmalı
        var puanlar = Enumerable.Range(1, 3).Select(i => new SoruPuani(i, 30)).ToList();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await elif.PutAsJsonAsync($"/api/sinavlar/{id}/acik-uclu/soru-ayarlari", new SoruAyarlari(3, 100, false, puanlar))).StatusCode);
    }

    [Fact]
    public async Task SinaviSil_TumVerileriKaldirir_VeYenidenBaslatilabilir()
    {
        var (elif, id) = await FinaliBaslat("3301104");

        Assert.True((await elif.DeleteAsync($"/api/sinavlar/{id}")).IsSuccessStatusCode);
        var db = uygulama.Veritabani();
        Assert.False(db.Sinavlar.Any(s => s.Id == id));
        Assert.False(db.SinavSonuclari.Any(s => s.SinavId == id));
        Assert.Equal(HttpStatusCode.NotFound, (await elif.GetAsync($"/api/dersler/{DersId("3301104")}/sinavlar/final")).StatusCode);

        // Başkası silemez
        var mehmet = await uygulama.GirisYapmisIstemciAsync("mehmet.kaya@selcuk.edu.tr", "mehmet.kaya20533*");
        var (_, yeniId) = await FinaliBaslat("3301104");
        Assert.Equal(HttpStatusCode.NotFound, (await mehmet.DeleteAsync($"/api/sinavlar/{yeniId}")).StatusCode);
    }
}
