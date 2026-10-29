using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ClosedXML.Excel;
using Sds.Api.Controllers;
using Sds.Api.Domain;
using Sds.Api.Dosyalar;

namespace Sds.Tests;

/// <summary>Okuyucuları ornek-veriler klasöründeki gerçekçi dosyalar ve _dogrulama sonuçlarıyla sınar.</summary>
public class DosyaOkumaTestleri(TestUygulamasi uygulama) : IClassFixture<TestUygulamasi>
{
    private static readonly string OrnekKlasor = Path.Combine(KokKlasor(), "ornek-veriler");

    private static readonly List<OptikFormAlani> VarsayilanDuzen =
    [
        new() { Alan = OptikAlan.OgrenciNo, Baslangic = 0, Bitis = 8 },
        new() { Alan = OptikAlan.SinavTuru, Baslangic = 9, Bitis = 9 },
        new() { Alan = OptikAlan.AdSoyad, Baslangic = 10, Bitis = 30 },
        new() { Alan = OptikAlan.KitapcikTuru, Baslangic = 31, Bitis = 31 },
        new() { Alan = OptikAlan.Cevaplar, Baslangic = 32, Bitis = null },
    ];

    private static string KokKlasor()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Sds.sln"))) d = d.Parent;
        return d!.FullName;
    }

    private static byte[] Dosya(string ad) => File.ReadAllBytes(Path.Combine(OrnekKlasor, ad));

    // ------------------------------------------------------------------ Optik

    [Fact]
    public void Optik_Vize_Windows1254VeTurkceKarakterlerDogruOkunur()
    {
        var s = OptikOkuyucu.Oku(Dosya("Optik_3304701_Vize.txt"), VarsayilanDuzen);

        Assert.Equal("Windows-1254 (Türkçe)", s.Kodlama);
        Assert.Equal(2, s.CevapAnahtarlari.Count());
        Assert.Equal(116, s.Ogrenciler.Count());
        Assert.Equal(25, s.TahminiSoruSayisi);
        Assert.Equal(['A', 'B'], s.CevapAnahtarlari.Select(a => a.Kitapcik!.Value));
        Assert.All(s.Satirlar, x => Assert.Equal('1', x.SinavTuru));

        // Bilerek eklenen kodlama hataları
        Assert.Contains(s.Ogrenciler, o => o.OgrenciNo == "133301032" && o.AdSoyad == "AYŞE NAZ YALÇIN");
        Assert.Contains(s.Ogrenciler, o => o.OgrenciNo == "" && o.AdSoyad == "MERYEM YAMAN");
        Assert.Contains(s.Ogrenciler, o => o.AdSoyad == "ECE ŞEN" && o.Kitapcik is null);
        Assert.Contains(s.Uyarilar, u => u.Contains("numarası kodlanmamış"));
        Assert.Contains(s.Uyarilar, u => u.Contains("kitapçık türü boş"));
    }

    [Fact]
    public void Optik_Vize_TumCevaplarBeklenenDogruSayilariniVerir()
    {
        var s = OptikOkuyucu.Oku(Dosya("Optik_3304701_Vize.txt"), VarsayilanDuzen);
        var anahtar = s.CevapAnahtarlari.ToDictionary(a => a.Kitapcik!.Value, a => a.Cevaplar);
        var optik = s.Ogrenciler.Where(o => o.OgrenciNo.Length > 0).ToDictionary(o => o.OgrenciNo);

        var kontrolEdilen = 0;
        foreach (var satir in File.ReadAllLines(Path.Combine(OrnekKlasor, "_dogrulama", "vize_beklenen_sonuclar.csv")).Skip(1))
        {
            var p = satir.Split(';');
            if (p[3].Length == 0 || !optik.TryGetValue(p[0], out var o)) continue; // girmeyen / hatalı kodlanan
            var kitapcik = p[2][0];
            var dogru = o.Cevaplar.Where((c, i) => c == anahtar[kitapcik][i]).Count();
            Assert.True(int.Parse(p[3]) == dogru, $"{p[0]}: beklenen {p[3]} doğru, okunan {dogru}");
            kontrolEdilen++;
        }
        Assert.True(kontrolEdilen >= 110);
    }

    [Fact]
    public void Optik_Utf8KaydedilmisDosyaDaAyniSonucuVerir()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var ham = Dosya("Optik_3304701_Vize.txt");
        var utf8 = Encoding.UTF8.GetBytes(Encoding.GetEncoding(1254).GetString(ham));

        var a = OptikOkuyucu.Oku(ham, VarsayilanDuzen);
        var b = OptikOkuyucu.Oku(utf8, VarsayilanDuzen);

        Assert.Equal("UTF-8", b.Kodlama);
        Assert.Equal(a.Satirlar, b.Satirlar);
    }

    [Fact]
    public void Optik_FarkliFormDuzeniAyarlardanOkunur()
    {
        // Başka bir okuyucu: önce ad soyad (0-19), sonra numara (20-28), kitapçık (29), tür (30), cevaplar (31-)
        List<OptikFormAlani> duzen =
        [
            new() { Alan = OptikAlan.AdSoyad, Baslangic = 0, Bitis = 19 },
            new() { Alan = OptikAlan.OgrenciNo, Baslangic = 20, Bitis = 28 },
            new() { Alan = OptikAlan.KitapcikTuru, Baslangic = 29, Bitis = 29 },
            new() { Alan = OptikAlan.SinavTuru, Baslangic = 30, Bitis = 30 },
            new() { Alan = OptikAlan.Cevaplar, Baslangic = 31, Bitis = null },
        ];
        var satir = "İLKNUR ÇAĞLAR".PadRight(20) + "243301007" + "B" + "2" + "ABCDE ABCD*";

        var s = OptikOkuyucu.Oku(Encoding.UTF8.GetBytes(satir + "\r\n"), duzen);

        var o = Assert.Single(s.Ogrenciler);
        Assert.Equal(("243301007", "İLKNUR ÇAĞLAR", 'B', '2', "ABCDE ABCD*"), (o.OgrenciNo, o.AdSoyad, o.Kitapcik, o.SinavTuru, o.Cevaplar));
    }

    [Fact]
    public void Optik_GecersizDosyalarAnlasilirHataVerir()
    {
        Assert.Throws<DosyaOkumaHatasi>(() => OptikOkuyucu.Oku([], VarsayilanDuzen));
        Assert.Throws<DosyaOkumaHatasi>(() => OptikOkuyucu.Oku([0x50, 0x4B, 0x03, 0x04, 0x00, 0x00], VarsayilanDuzen)); // xlsx/zip
        var sadeceAnahtar = Encoding.ASCII.GetBytes("0000000001CEVAP ANAHTARI       AABCDE\r\n");
        var hata = Assert.Throws<DosyaOkumaHatasi>(() => OptikOkuyucu.Oku(sadeceAnahtar, VarsayilanDuzen));
        Assert.Contains("öğrenci satırı", hata.Message);
    }

    // ------------------------------------------------------------------ Excel

    [Fact]
    public void SinifListesi_ObisSablonuOkunur()
    {
        using var f = File.OpenRead(Path.Combine(OrnekKlasor, "OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx"));
        var s = ExcelOkuyucu.SinifListesiOku(f);

        Assert.Equal(120, s.Ogrenciler.Count);
        Assert.Empty(s.Uyarilar);
        Assert.Equal(("213301026", "ESRA TURAN"), (s.Ogrenciler[0].OgrenciNo, s.Ogrenciler[0].AdSoyad));
        Assert.Contains(s.Ogrenciler, o => o.OgrenciNo == "233301032" && o.AdSoyad == "AYŞE NAZ YALÇIN");
        Assert.Equal("3304701", s.DersKodu); // üst bilgideki "Ders: 3304701 - ..." satırı

        using var b = File.OpenRead(Path.Combine(OrnekKlasor, "OBIS_Sinif_Listesi_3304701_2026-2027_Guz_Butunleme.xlsx"));
        Assert.Equal(61, ExcelOkuyucu.SinifListesiOku(b).Ogrenciler.Count);
    }

    [Fact]
    public void SinifListesi_BasitBicimVeSayiOlarakSaklanmisNumaralarOkunur()
    {
        using var ms = Excel(ws =>
        {
            ws.Cell(1, 1).Value = "OGRENCINO";
            ws.Cell(1, 2).Value = "ADSOYAD";
            ws.Cell(2, 1).Value = 233301001; // sayı olarak
            ws.Cell(2, 2).Value = "  ali   veli ";
            ws.Cell(3, 1).Value = "233301002";
            ws.Cell(3, 2).Value = "İpek Işık";
            ws.Cell(4, 1).Value = "233301001";
            ws.Cell(4, 2).Value = "Tekrar Satır";
        });

        var s = ExcelOkuyucu.SinifListesiOku(ms);

        Assert.Equal([("233301001", "ALİ VELİ"), ("233301002", "İPEK IŞIK")], s.Ogrenciler.Select(o => (o.OgrenciNo, o.AdSoyad)));
        Assert.Null(s.DersKodu);
        Assert.Contains(s.Uyarilar, u => u.Contains("birden fazla"));
    }

    [Fact]
    public void SinifListesi_BaslikYoksaHataVerir()
    {
        using var ms = Excel(ws => ws.Cell(1, 1).Value = "Bu bir sınıf listesi değil");
        var hata = Assert.Throws<DosyaOkumaHatasi>(() => ExcelOkuyucu.SinifListesiOku(ms));
        Assert.Contains("Öğrenci No", hata.Message);

        Assert.Throws<DosyaOkumaHatasi>(() => ExcelOkuyucu.SinifListesiOku(new MemoryStream("düz metin"u8.ToArray())));
    }

    [Fact]
    public void NotTablosu_AcikUcluFinalBeklenenToplamlariVerir()
    {
        using var f = File.OpenRead(Path.Combine(OrnekKlasor, "Final_Acik_Uclu_Notlar_3304701.xlsx"));
        var s = ExcelOkuyucu.NotTablosuOku(f);

        Assert.Equal(Enumerable.Range(1, 10).Select(i => $"S{i}"), s.Sutunlar);
        Assert.Equal(120, s.Satirlar.Count);
        Assert.Equal(4, s.Satirlar.Count(x => x.Degerler.Values.All(v => v is null))); // sınava girmeyenler

        var beklenen = File.ReadAllLines(Path.Combine(OrnekKlasor, "_dogrulama", "final_beklenen_sonuclar.csv")).Skip(1)
            .Select(l => l.Split(';')).Where(p => p[^1].Length > 0 && char.IsDigit(p[^1][0]))
            .ToDictionary(p => p[0], p => decimal.Parse(p[^1], System.Globalization.CultureInfo.InvariantCulture));
        foreach (var satir in s.Satirlar.Where(x => x.Degerler.Values.Any(v => v is not null)))
            Assert.Equal(beklenen[satir.OgrenciNo], satir.Degerler.Values.Sum(v => v ?? 0));
    }

    [Fact]
    public void NotTablosu_ProjeNotlariOkunur()
    {
        using var f = File.OpenRead(Path.Combine(OrnekKlasor, "Proje_Notlari_3304701.xlsx"));
        var s = ExcelOkuyucu.NotTablosuOku(f);

        Assert.Equal(["SINAVNOTU"], s.Sutunlar);
        Assert.All(s.Satirlar, x => Assert.InRange(x.Degerler["SINAVNOTU"]!.Value, 1, 5));
    }

    // ------------------------------------------------------------------ Önizleme API

    [Fact]
    public async Task OnizlemeApi_OptikVeSinifListesiniOkur()
    {
        var elif = await uygulama.GirisYapmisIstemciAsync("elif.yildirim@selcuk.edu.tr", "elif.yildirim20417*");

        var optik = await elif.PostAsync("/api/dosyalar/optik/onizle", Form("Optik_3304701_Vize.txt"));
        Assert.Equal(HttpStatusCode.OK, optik.StatusCode);
        var o = await optik.Content.ReadFromJsonAsync<OptikOnizleme>();
        Assert.Equal((116, 2, 25), (o!.OgrenciSayisi, o.CevapAnahtariSayisi, o.TahminiSoruSayisi));
        Assert.Equal(1, o.KitapcikDagilimi["Boş"]);

        var liste = await elif.PostAsync("/api/dosyalar/sinif-listesi/onizle", Form("OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx"));
        Assert.Equal(120, (await liste.Content.ReadFromJsonAsync<SinifListesiOnizleme>())!.OgrenciSayisi);

        // Yanlış alana yanlış dosya
        var yanlis = await elif.PostAsync("/api/dosyalar/optik/onizle", Form("OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx"));
        Assert.Equal(HttpStatusCode.BadRequest, yanlis.StatusCode);
    }

    [Fact]
    public async Task OnizlemeApi_AdminKullanamaz()
    {
        var admin = await uygulama.GirisYapmisIstemciAsync("admin@selcuk.edu.tr", "admin10001*");
        var yanit = await admin.PostAsync("/api/dosyalar/optik/onizle", Form("Optik_3304701_Vize.txt"));
        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
    }

    private static MultipartFormDataContent Form(string dosyaAdi)
    {
        var icerik = new ByteArrayContent(Dosya(dosyaAdi));
        icerik.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { icerik, "dosya", dosyaAdi } };
    }

    private static MemoryStream Excel(Action<IXLWorksheet> doldur)
    {
        using var wb = new XLWorkbook();
        doldur(wb.AddWorksheet("Liste"));
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }
}
