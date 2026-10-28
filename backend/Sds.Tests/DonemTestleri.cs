using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Sds.Api.Controllers;
using Sds.Api.Domain;

namespace Sds.Tests;

/// <summary>Dönem notu kuralları: %40/%60 ağırlık, bütünleme finalin yerine geçer, final alt sınırı, girmeyenler ve yuvarlama.</summary>
public class DonemTestleri(TestUygulamasi uygulama) : IClassFixture<TestUygulamasi>
{
    // Öğrenci: vize, final, bütünleme (null: girmedi; listede olmayan sınav için ayrı bayrak)
    private static readonly (string No, decimal? Vize, decimal? Final, bool ButListesinde, decimal? But)[] Ogrenciler =
    [
        ("990000001", 80, 90, false, null),   // 86 → BA
        ("990000002", 100, 25, true, null),   // finalden 30 altı, büte girmedi → FF (ağırlıklı 55)
        ("990000003", 50, 40, true, 70),      // büt finalin yerine: 20 + 42 = 62 → CC
        ("990000004", 60, null, true, null),  // final ve büte girmedi → FF
        ("990000005", null, 90, false, null), // vizeye girmedi: 0 + 54 → DD
        ("990000006", 49, 50, false, null),   // 19,6 + 30 = 49,6 → 50'ye yuvarlanır → DD
        ("990000007", 70, 20, true, 35),      // büt düşük de olsa etkin sınav: 28 + 21 = 49 → FF
    ];

    private async Task<(HttpClient Elif, int DersId)> DersHazirla(string kod, bool butunlemeli = true, bool finalTamam = true)
    {
        var db = uygulama.Veritabani();
        var elifId = db.Kullanicilar.Single(k => k.Eposta == "elif.yildirim@selcuk.edu.tr").Id;
        var ders = new Ders { Kod = kod, Ad = "Dönem Test Dersi", AkademikYil = "2026-2027", Donem = Donem.Guz, Sinif = 4 };
        ders.Egitmenler.Add(new DersEgitmen { KullaniciId = elifId });
        ders.OgrenmeCiktilari.Add(new OgrenmeCiktisi { SiraNo = 1, Aciklama = "Test çıktısı" });
        db.Dersler.Add(ders);
        var ogrenciler = Ogrenciler.Select(o => db.Ogrenciler.FirstOrDefault(x => x.OgrenciNo == o.No) ?? new Ogrenci { OgrenciNo = o.No, AdSoyad = $"ÖĞRENCİ {o.No[^1]}" }).ToList();

        Sinav Sinav(SinavTuru tur, Func<(string No, decimal? Vize, decimal? Final, bool ButListesinde, decimal? But), (bool Listede, decimal? Puan)> puan, bool tamam = true)
        {
            var s = new Sinav { Ders = ders, Tur = tur, Format = SinavFormati.AcikUclu, SoruSayisi = 1, Durum = tamam ? SinavDurumu.Tamamlandi : SinavDurumu.Taslak, TamamlananAdim = tamam ? 5 : 3 };
            for (var i = 0; i < Ogrenciler.Length; i++)
            {
                var (listede, p) = puan(Ogrenciler[i]);
                if (!listede) continue;
                s.Sonuclar.Add(new SinavSonucu { Ogrenci = ogrenciler[i], Eslesme = p is null ? EslesmeDurumu.SinavaGirmedi : EslesmeDurumu.NotDosyasindan, HamPuan = p ?? 0 });
            }
            return s;
        }
        db.Sinavlar.Add(Sinav(SinavTuru.Vize, o => (true, o.Vize)));
        db.Sinavlar.Add(Sinav(SinavTuru.Final, o => (true, o.Final), finalTamam));
        if (butunlemeli) db.Sinavlar.Add(Sinav(SinavTuru.Butunleme, o => (o.ButListesinde, o.But)));
        await db.SaveChangesAsync();
        return (await uygulama.GirisYapmisIstemciAsync("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*"), ders.Id);
    }

    [Fact]
    public async Task DonemNotu_YonetmelikKurallarinaGoreHesaplanir()
    {
        var (elif, dersId) = await DersHazirla("9990001");
        var a = await elif.GetFromJsonAsync<DonemAnaliziYaniti>($"/api/dersler/{dersId}/donem");
        var s = a!.Ogrenciler.ToDictionary(x => x.OgrenciNo);

        Assert.Equal((86m, "BA", "Final"), (s["990000001"].Agirlikli, s["990000001"].Harf, s["990000001"].EtkinSinav));
        Assert.Equal((55m, "FF"), (s["990000002"].Agirlikli, s["990000002"].Harf));
        Assert.Contains("alt sınırın (30) altında", s["990000002"].Aciklama);
        Assert.Equal((62m, "CC", "Bütünleme"), (s["990000003"].Agirlikli, s["990000003"].Harf, s["990000003"].EtkinSinav));
        Assert.Equal(("FF", "Final ve bütünlemeye girmedi"), (s["990000004"].Harf, s["990000004"].Aciklama));
        Assert.Equal((54m, "DD", "Vizeye girmedi"), (s["990000005"].Agirlikli, s["990000005"].Harf, s["990000005"].Aciklama));
        Assert.Equal((49.6m, "DD", "Şartlı Geçer"), (s["990000006"].Agirlikli, s["990000006"].Harf, s["990000006"].Durum));
        Assert.Equal((49m, "FF", "Bütünleme"), (s["990000007"].Agirlikli, s["990000007"].Harf, s["990000007"].EtkinSinav));
        Assert.False(s["990000001"].ButunlemeListesinde);
        Assert.Null(s["990000004"].Butunleme);

        Assert.Equal((7, 2, 2, 3), (a.Ozet.Toplam, a.Ozet.Basarili, a.Ozet.SartliGecen, a.Ozet.Basarisiz));
        Assert.Equal(57.1m, a.Ozet.BasariOrani);
        Assert.Equal(7, a.HarfDagilimi.Sum(h => h.Sayi));
        Assert.Equal(7, a.PuanDagilimi.Sum(d => d.Sayi));
        Assert.Equal(["Vize", "Final", "Bütünleme"], a.Sinavlar.Select(x => x.Sinav));
        Assert.Equal((40, 60, 30), (a.Genel.VizeAgirligi, a.Genel.FinalAgirligi, a.Genel.FinalAltSiniri));
        Assert.Equal(1, a.Ciktilar.Single().SiraNo);

        // Excel: 4 sayfa, öğrenci satırları ve harfler API ile aynı
        var yanit = await elif.GetAsync($"/api/dersler/{dersId}/donem/excel");
        Assert.True(yanit.IsSuccessStatusCode, await yanit.Content.ReadAsStringAsync());
        Assert.Equal("9990001_2026-2027_Donem_Analiz_Raporu.xlsx", yanit.Content.Headers.ContentDisposition!.FileNameStar);
        using var wb = new XLWorkbook(await yanit.Content.ReadAsStreamAsync());
        Assert.Equal(["Kurs Özeti", "Öğrenci Puanları", "Sınav İstatistikleri", "Öğrenme Çıktıları"], wb.Worksheets.Select(w => w.Name));
        var wo = wb.Worksheet("Öğrenci Puanları");
        Assert.Equal(("AĞIRLIKLI ORT.", "HARF"), (wo.Cell(8, 8).GetString(), wo.Cell(8, 9).GetString()));
        Assert.Equal(15, wo.LastRowUsed()!.RowNumber());
        var harfler = Enumerable.Range(9, 7).ToDictionary(r => wo.Cell(r, 2).GetString(), r => wo.Cell(r, 9).GetString());
        Assert.All(a.Ogrenciler, o => Assert.Equal(o.Harf, harfler[o.OgrenciNo]));
        Assert.Equal("G", wo.Cell(12, 5).GetString());   // 4. öğrenci finale girmedi
        Assert.Equal("", wo.Cell(9, 6).GetString());     // 1. öğrenci bütünleme listesinde değil
        Assert.Equal("DÖNEM ANALİZ RAPORU", wb.Worksheet("Kurs Özeti").Cell(5, 2).GetString());
        var wsi = wb.Worksheet("Sınav İstatistikleri");
        Assert.Equal(["Vize", "Final", "Bütünleme"], Enumerable.Range(9, 3).Select(r => wsi.Cell(r, 1).GetString()));
    }

    [Fact]
    public async Task ButunlemesizDonem_FinalEtkinSinavdir()
    {
        var (elif, dersId) = await DersHazirla("9990002", butunlemeli: false);
        var a = await elif.GetFromJsonAsync<DonemAnaliziYaniti>($"/api/dersler/{dersId}/donem");
        var s3 = a!.Ogrenciler.Single(x => x.OgrenciNo == "990000003");
        Assert.Equal((44m, "FF", "Final"), (s3.Agirlikli, s3.Harf, s3.EtkinSinav));
        Assert.Equal("Finale girmedi", a.Ogrenciler.Single(x => x.OgrenciNo == "990000004").Aciklama);
        Assert.Equal(2, a.Sinavlar.Count);
    }

    [Fact]
    public async Task FinalTamamlanmadan_DonemRaporuAcilmaz_BaskasiGoremez()
    {
        var (elif, dersId) = await DersHazirla("9990003", butunlemeli: false, finalTamam: false);
        var yanit = await elif.GetAsync($"/api/dersler/{dersId}/donem");
        Assert.Equal(HttpStatusCode.BadRequest, yanit.StatusCode);
        Assert.Contains("tamamlanması gerekir", await yanit.Content.ReadAsStringAsync());

        var (_, acikDers) = await DersHazirla("9990004");
        var mehmet = await uygulama.GirisYapmisIstemciAsync("mehmet.kaya@selcuk.edu.tr", "mehmet.kaya20533*");
        Assert.Equal(HttpStatusCode.NotFound, (await mehmet.GetAsync($"/api/dersler/{acikDers}/donem")).StatusCode);
    }
}
