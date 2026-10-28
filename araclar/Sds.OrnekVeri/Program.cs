using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Sds.OrnekVeri;

// Gerçekçi örnek veri üretici. Sabit tohum kullanır: her çalıştırmada aynı dosyalar üretilir.
// Kullanım: dotnet run --project araclar/Sds.OrnekVeri
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var kok = KokKlasor();
var cikti = Path.Combine(kok, "ornek-veriler");
var dogrulama = Path.Combine(cikti, "_dogrulama");
Directory.CreateDirectory(dogrulama);

var r = new Random(2026);
const string DersKodu = "3304701";
const string DersAdi = "Bilgisayar Mühendisliği Uygulamaları";

// ---------------------------------------------------------------- 1. Sınıf listesi (120 öğrenci)
var ogrenciler = SinifOlustur(r, 120);
SinifListesiYaz(Path.Combine(cikti, $"OBIS_Sinif_Listesi_{DersKodu}_2026-2027_Guz.xlsx"), ogrenciler, "Vize / Final");

// ---------------------------------------------------------------- 2. Vize: 25 soruluk, A/B kitapçıklı optik
var vize = SinavOlustur(r, "Vize", '1', ozelSorular: true);
var vizeGirmeyen = ogrenciler.OrderBy(_ => r.Next()).Take(5).ToHashSet();
var vizeKagitlari = KagitlariOlustur(r, vize, ogrenciler.Where(o => !vizeGirmeyen.Contains(o)).ToList(), yetenekFarki: 0);
vizeKagitlari = HataliKodlamalarEkle(r, vizeKagitlari, vize);
OptikYaz(Path.Combine(cikti, $"Optik_{DersKodu}_Vize.txt"), vize, vizeKagitlari);
var vizePuan = BeklenenYaz(Path.Combine(dogrulama, "vize_beklenen_sonuclar.csv"), vize, vizeKagitlari, ogrenciler);
AnahtarYaz(Path.Combine(dogrulama, "vize_cevap_anahtari.txt"), vize);

// ---------------------------------------------------------------- 3. Final: 10 soruluk açık uçlu
var finalPuan = AcikUcluYaz(r, Path.Combine(cikti, $"Final_Acik_Uclu_Notlar_{DersKodu}.xlsx"),
    Path.Combine(dogrulama, "final_beklenen_sonuclar.csv"), ogrenciler, girmeyenSayisi: 4);

// ---------------------------------------------------------------- 4. Proje / ek puanlar
ProjeNotlariYaz(r, Path.Combine(cikti, $"Proje_Notlari_{DersKodu}.xlsx"), ogrenciler);

// ---------------------------------------------------------------- 5. Bütünleme: yalnızca hakkı olanlar
// Dönem notu = vize %40 + final %60. Notu 60'ın altında (DC dahil) veya finalden 30 altı alan / girmeyen bütünlemeye girebilir.
var butHakki = ogrenciler.Where(o =>
{
    var v = vizePuan.GetValueOrDefault(o.No);
    var f = finalPuan.GetValueOrDefault(o.No, -1);
    return f < 30 || 0.4m * v + 0.6m * f < 60;
}).ToList();
SinifListesiYaz(Path.Combine(cikti, $"OBIS_Sinif_Listesi_{DersKodu}_2026-2027_Guz_Butunleme.xlsx"), butHakki, "Bütünleme");
var but = SinavOlustur(r, "Bütünleme", '3', ozelSorular: false);
var butGirenler = butHakki.Where(_ => r.NextDouble() < 0.75).ToList();
var butKagitlari = KagitlariOlustur(r, but, butGirenler, yetenekFarki: 0.35);
OptikYaz(Path.Combine(cikti, $"Optik_{DersKodu}_Butunleme.txt"), but, butKagitlari);
BeklenenYaz(Path.Combine(dogrulama, "butunleme_beklenen_sonuclar.csv"), but, butKagitlari, butHakki);
AnahtarYaz(Path.Combine(dogrulama, "butunleme_cevap_anahtari.txt"), but);

// ---------------------------------------------------------------- Özet
Console.WriteLine($"Çıktı klasörü: {cikti}");
Console.WriteLine($"Sınıf: {ogrenciler.Count} öğrenci ({ogrenciler.Count(o => o.Tekrar)} dersi tekrar alan)");
Ozet("Vize", vizePuan.Values);
Ozet("Final", finalPuan.Values);
Console.WriteLine($"Bütünleme hakkı: {butHakki.Count}, giren: {butGirenler.Count}");
return;

// ================================================================== Yardımcılar

static string KokKlasor()
{
    var d = new DirectoryInfo(AppContext.BaseDirectory);
    while (d is not null && !File.Exists(Path.Combine(d.FullName, "Sds.sln"))) d = d.Parent;
    return d?.FullName ?? throw new InvalidOperationException("Sds.sln bulunamadı");
}

static double Normal(Random r, double ort = 0, double ss = 1) =>
    ort + ss * Math.Sqrt(-2 * Math.Log(1 - r.NextDouble())) * Math.Cos(2 * Math.PI * r.NextDouble());

static double Sigmoid(double x) => 1 / (1 + Math.Exp(-x));

/// <summary>
/// Selçuk numara biçimi: giriş yılı (2) + bölüm kodu (4) + sıra (3). 4. sınıf dersi olduğu için çoğunluk 2023 girişli,
/// bir kısmı dersi tekrar alan 2022 / 2021 girişli öğrencilerdir.
/// </summary>
static List<Ogrenci> SinifOlustur(Random r, int sayi)
{
    var kullanilan = new HashSet<string>();
    var liste = new List<Ogrenci>();
    var gruplar = new (string yil, int adet, bool tekrar)[] { ("23", sayi - 16, false), ("22", 10, true), ("21", 6, true) };

    foreach (var (yil, adet, tekrar) in gruplar)
    {
        var siralar = Enumerable.Range(1, 130).OrderBy(_ => r.Next()).Take(adet).Order();
        foreach (var sira in siralar)
        {
            string ad, soyad;
            do
            {
                var kadin = r.NextDouble() < 0.38; // mühendislikte yaklaşık oran
                ad = (kadin ? Isimler.Kadin : Isimler.Erkek)[r.Next(50)];
                if (r.NextDouble() < 0.12)
                {
                    var ikinci = kadin ? Isimler.IkinciAdKadin : Isimler.IkinciAdErkek;
                    ad += " " + ikinci[r.Next(ikinci.Length)];
                }
                soyad = Isimler.Soyad[r.Next(Isimler.Soyad.Length)];
            } while (!kullanilan.Add(ad + soyad));

            // Tekrar alanların ortalama başarısı biraz daha düşük
            liste.Add(new Ogrenci($"{yil}3301{sira:000}", ad, soyad, Normal(r, tekrar ? -0.2 : 0.45, 0.95), tekrar));
        }
    }
    return liste.OrderBy(o => o.No).ToList();
}

/// <summary>OBİS'ten indirilen sınav şablonu biçiminde sınıf listesi.</summary>
static void SinifListesiYaz(string yol, List<Ogrenci> ogrenciler, string sinavlar)
{
    using var wb = new XLWorkbook();
    var ws = wb.AddWorksheet("Sınıf Listesi");
    ws.Cell("A1").Value = "T.C. SELÇUK ÜNİVERSİTESİ";
    ws.Cell("A2").Value = "ÖĞRENCİ BİLGİ SİSTEMİ - DERS SINIF LİSTESİ";
    ws.Range("A1:F1").Merge().Style.Font.SetBold().Font.SetFontSize(13).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    ws.Range("A2:F2").Merge().Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

    string[,] bilgi =
    {
        { "Birim", "Teknoloji Fakültesi / Bilgisayar Mühendisliği" },
        { "Ders", $"{DersKodu} - {Isimler.Buyuk(DersAdi)}" },
        { "Akademik Yıl / Dönem", "2026-2027 / GÜZ" },
        { "Öğretim Elemanı", "Dr. Öğr. Üyesi Elif YILDIRIM" },
        { "Şube", "1 - Normal Öğretim" },
        { "Sınav", sinavlar },
    };
    for (var i = 0; i < bilgi.GetLength(0); i++)
    {
        ws.Cell(4 + i, 1).Value = bilgi[i, 0];
        ws.Cell(4 + i, 1).Style.Font.SetBold();
        ws.Cell(4 + i, 2).Value = bilgi[i, 1];
    }

    var baslikSatiri = 4 + bilgi.GetLength(0) + 1;
    string[] basliklar = ["Sıra No", "Öğrenci No", "Adı", "Soyadı", "Sınıf", "Durum"];
    for (var c = 0; c < basliklar.Length; c++) ws.Cell(baslikSatiri, c + 1).Value = basliklar[c];
    ws.Range(baslikSatiri, 1, baslikSatiri, basliklar.Length).Style
        .Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#D9E1F2")).Border.SetOutsideBorder(XLBorderStyleValues.Thin);

    for (var i = 0; i < ogrenciler.Count; i++)
    {
        var o = ogrenciler[i];
        var s = baslikSatiri + 1 + i;
        ws.Cell(s, 1).Value = i + 1;
        ws.Cell(s, 2).Value = o.No; // OBİS numarayı metin olarak verir
        ws.Cell(s, 2).Style.NumberFormat.Format = "@";
        ws.Cell(s, 3).Value = Isimler.Buyuk(o.Ad);
        ws.Cell(s, 4).Value = Isimler.Buyuk(o.Soyad);
        ws.Cell(s, 5).Value = 4;
        ws.Cell(s, 6).Value = o.Tekrar ? "Tekrar" : "Kayıtlı";
    }
    ws.Cell(baslikSatiri + ogrenciler.Count + 2, 1).Value = $"Toplam öğrenci sayısı: {ogrenciler.Count}";
    ws.Columns().AdjustToContents();
    wb.SaveAs(yol);
}

/// <summary>
/// 25 soru. Özel sorular analiz ekranında fark edilecek durumları üretir:
/// 3. soru çok kolay, 10. soru neredeyse hiç kimsenin yapamadığı kadar zor,
/// 17. soruda başarılı öğrenciler bile ağırlıklı olarak B'yi seçiyor (cevap anahtarı hatası şüphesi).
/// </summary>
static CoktanSecmeliSinav SinavOlustur(Random r, string ad, char turKodu, bool ozelSorular)
{
    var sorular = new List<Soru>();
    for (var i = 1; i <= 25; i++)
    {
        var dogru = "ABCDE"[r.Next(5)];
        var celdirici = "ABCDE".Where(c => c != dogru).ElementAt(r.Next(4));
        var zorluk = Normal(r, -0.9 + i * 0.07, 0.6); // sona doğru zorlaşır
        var egilim = 0.35 + r.NextDouble() * 0.3;

        if (ozelSorular && i == 3) zorluk = -2.6;
        if (ozelSorular && i == 10) zorluk = 3.4;
        if (ozelSorular && i == 17) { dogru = 'D'; celdirici = 'B'; zorluk = 2.6; egilim = 0.9; }
        sorular.Add(new Soru(i, dogru, zorluk, celdirici, egilim));
    }

    // B kitapçığı: aynı sorular farklı sırada
    var bdenAya = Enumerable.Range(1, 25).OrderBy(_ => r.Next()).ToArray();
    return new CoktanSecmeliSinav { Ad = ad, TurKodu = turKodu, Sorular = sorular, BdenAya = bdenAya };
}

static List<OptikKagit> KagitlariOlustur(Random r, CoktanSecmeliSinav sinav, List<Ogrenci> girenler, double yetenekFarki)
{
    var kagitlar = new List<OptikKagit>();
    foreach (var o in girenler)
    {
        var kitapcik = r.NextDouble() < 0.5 ? 'A' : 'B';
        var yetenek = o.Yetenek + yetenekFarki;
        var cevaplar = new char[25];
        for (var k = 1; k <= 25; k++)
        {
            var soru = sinav.Sorular[(kitapcik == 'B' ? sinav.BdenAya[k - 1] : k) - 1];
            var pDogru = Sigmoid(1.7 * (yetenek - soru.Zorluk));
            // Zor sorular ve sondaki sorular daha sık boş bırakılır
            var pBos = Math.Clamp(0.02 + 0.10 * (1 - pDogru) + (k > 20 ? 0.04 : 0), 0, 0.25);
            var x = r.NextDouble();
            char c;
            if (x < pDogru) c = soru.Dogru;
            else if (x < pDogru + pBos) c = ' ';
            else if (r.NextDouble() < soru.CeldiriciEgilimi) c = soru.GucluCeldirici;
            else c = "ABCDE".Where(h => h != soru.Dogru).ElementAt(r.Next(4));

            if (c != ' ' && r.NextDouble() < 0.006) c = '*'; // birden fazla şık işaretlenmiş
            cevaplar[k - 1] = c;
        }
        kagitlar.Add(new OptikKagit(o, kitapcik, new string(cevaplar)));
    }
    return kagitlar;
}

/// <summary>
/// Gerçek hayatta sık görülen kodlama hataları (3. adım - veri eşleştirme ekranında çözülür):
/// numarasının ilk hanesini yanlış kodlayan, bir hanesini yanlış kodlayan, numarasını hiç kodlamayan,
/// kitapçık türünü boş bırakan öğrenci ve sınıf listesinde olmayan (başka şubeden) bir öğrenci.
/// </summary>
static List<OptikKagit> HataliKodlamalarEkle(Random r, List<OptikKagit> kagitlar, CoktanSecmeliSinav sinav)
{
    var sonuc = new List<OptikKagit>(kagitlar);
    var adaylar = kagitlar.Where(k => !k.Ogrenci.Tekrar).OrderBy(_ => r.Next()).Take(4).ToList();

    Degistir(adaylar[0], k => k with { YazilanNo = "1" + k.Ogrenci.No[1..], Senaryo = "İlk hane yanlış kodlanmış" });
    Degistir(adaylar[1], k => k with
    {
        YazilanNo = k.Ogrenci.No[..5] + (k.Ogrenci.No[5] == '1' ? '7' : '1') + k.Ogrenci.No[6..],
        Senaryo = "Bir hane yanlış kodlanmış",
    });
    Degistir(adaylar[2], k => k with { YazilanNo = "", Senaryo = "Numara kodlanmamış (isimden elle eşleştirilecek)" });
    Degistir(adaylar[3], k => k with { YazilanKitapcik = ' ', Senaryo = "Kitapçık türü boş bırakılmış" });

    // Sınıf listesinde olmayan öğrenci: ikinci öğretim şubesinden yanlışlıkla bu salonda sınava girmiş
    var misafir = new Ogrenci("233302014", "Burak", "Şahin", 0.4, false);
    var misafirKagit = KagitlariOlustur(r, sinav, [misafir], 0)[0] with { Senaryo = "Sınıf listesinde yok (başka şube)" };
    sonuc.Insert(r.Next(sonuc.Count), misafirKagit);
    return sonuc;

    void Degistir(OptikKagit eski, Func<OptikKagit, OptikKagit> f) => sonuc[sonuc.IndexOf(eski)] = f(eski);
}

/// <summary>
/// Optik okuyucu çıktısı: sabit genişlikli, Windows-1254 kodlamalı, CRLF satır sonlu metin.
/// [0-8] öğrenci no, [9] sınav türü, [10-30] ad soyad, [31] kitapçık, [32-56] cevaplar.
/// İlk satırlar öğretim üyesinin optiğe okuttuğu cevap anahtarı kağıtlarıdır.
/// </summary>
static void OptikYaz(string yol, CoktanSecmeliSinav sinav, List<OptikKagit> kagitlar)
{
    var sb = new StringBuilder();
    foreach (var kitapcik in new[] { 'A', 'B' })
    {
        var anahtar = new string(Enumerable.Range(1, 25).Select(k => sinav.DogruCevap(kitapcik, k)).ToArray());
        sb.Append(Satir("000000000", sinav.TurKodu, "CEVAP ANAHTARI", kitapcik, anahtar));
    }
    foreach (var k in kagitlar)
        sb.Append(Satir(k.YazilanNo, sinav.TurKodu, k.YazilanAdSoyad, k.YazilanKitapcik, k.Cevaplar));

    File.WriteAllText(yol, sb.ToString(), Encoding.GetEncoding(1254));

    static string Satir(string no, char tur, string adSoyad, char kitapcik, string cevaplar) =>
        no.PadRight(9)[..9] + tur + adSoyad.PadRight(21)[..21] + kitapcik + cevaplar + "\r\n";
}

/// <summary>Sistemin hesaplaması gereken sonuçlar (testlerde ve elle kontrolde kullanılır).</summary>
static Dictionary<string, decimal> BeklenenYaz(string yol, CoktanSecmeliSinav sinav, List<OptikKagit> kagitlar, List<Ogrenci> sinif)
{
    var puanlar = new Dictionary<string, decimal>();
    var sb = new StringBuilder("OgrenciNo;AdSoyad;Kitapcik;Dogru;Yanlis;Bos;HamPuan;Senaryo\n");
    foreach (var o in sinif)
    {
        var k = kagitlar.FirstOrDefault(x => x.Ogrenci.No == o.No);
        if (k is null)
        {
            sb.Append($"{o.No};{o.AdSoyad};;;;;;Sınava girmedi\n");
            continue;
        }
        int d = 0, y = 0, b = 0;
        for (var i = 1; i <= 25; i++)
        {
            var c = k.Cevaplar[i - 1];
            if (c == ' ') b++;
            else if (c == sinav.DogruCevap(k.Kitapcik, i)) d++;
            else y++;
        }
        var puan = d * sinav.SoruPuani;
        puanlar[o.No] = puan;
        sb.Append($"{o.No};{o.AdSoyad};{k.Kitapcik};{d};{y};{b};{puan:0.##};{k.Senaryo}\n");
    }
    foreach (var k in kagitlar.Where(k => sinif.All(o => o.No != k.Ogrenci.No)))
        sb.Append($"{k.YazilanNo};{k.YazilanAdSoyad};{k.Kitapcik};;;;;{k.Senaryo}\n");

    File.WriteAllText(yol, sb.ToString(), new UTF8Encoding(true));
    return puanlar;
}

static void AnahtarYaz(string yol, CoktanSecmeliSinav sinav)
{
    var sb = new StringBuilder($"{sinav.Ad} cevap anahtarı ({sinav.Sorular.Count} soru, her soru {sinav.SoruPuani:0.##} puan)\n\n");
    sb.Append("A kitapçığı: ").Append(new string(sinav.Sorular.Select(s => s.Dogru).ToArray())).Append('\n');
    sb.Append("B kitapçığı: ").Append(new string(Enumerable.Range(1, 25).Select(k => sinav.DogruCevap('B', k)).ToArray())).Append("\n\n");
    sb.Append("Kitapçık soru eşleştirmesi (B kitapçığındaki soru → A kitapçığındaki karşılığı):\n");
    for (var i = 0; i < sinav.BdenAya.Length; i++) sb.Append($"B{i + 1,-3}→ A{sinav.BdenAya[i]}\n");
    File.WriteAllText(yol, sb.ToString(), new UTF8Encoding(true));
}

/// <summary>Açık uçlu final: 10 soru × 10 puan; öğretim üyesinin doldurduğu Excel biçiminde.</summary>
static Dictionary<string, decimal> AcikUcluYaz(Random r, string yol, string beklenenYol, List<Ogrenci> sinif, int girmeyenSayisi)
{
    var zorluklar = Enumerable.Range(1, 10).Select(i => Normal(r, -0.6 + i * 0.12, 0.4)).ToArray();
    var girmeyen = sinif.OrderBy(_ => r.Next()).Take(girmeyenSayisi).ToHashSet();
    var puanlar = new Dictionary<string, decimal>();

    using var wb = new XLWorkbook();
    var ws = wb.AddWorksheet("Notlar");
    string[] basliklar = ["OGRENCINO", "ADSOYAD", .. Enumerable.Range(1, 10).Select(i => $"S{i}")];
    for (var c = 0; c < basliklar.Length; c++) ws.Cell(1, c + 1).Value = basliklar[c];
    ws.Row(1).Style.Font.SetBold();

    var beklenen = new StringBuilder("OgrenciNo;AdSoyad;" + string.Join(';', basliklar[2..]) + ";Toplam\n");
    for (var i = 0; i < sinif.Count; i++)
    {
        var o = sinif[i];
        ws.Cell(i + 2, 1).Value = o.No;
        ws.Cell(i + 2, 1).Style.NumberFormat.Format = "@";
        ws.Cell(i + 2, 2).Value = Isimler.Buyuk(o.AdSoyad);
        if (girmeyen.Contains(o))
        {
            beklenen.Append($"{o.No};{o.AdSoyad};;;;;;;;;;;Sınava girmedi\n");
            continue;
        }

        var sorular = new decimal[10];
        for (var s = 0; s < 10; s++)
        {
            var oran = Sigmoid(1.5 * (o.Yetenek + 0.15 - zorluklar[s]) + Normal(r, 0, 0.5));
            sorular[s] = Math.Round((decimal)(oran * 10) * 2, MidpointRounding.AwayFromZero) / 2; // yarım puanlık
            ws.Cell(i + 2, s + 3).Value = sorular[s];
        }
        puanlar[o.No] = sorular.Sum();
        beklenen.Append($"{o.No};{o.AdSoyad};{string.Join(';', sorular.Select(p => p.ToString("0.#")))};{sorular.Sum():0.#}\n");
    }
    ws.Columns().AdjustToContents();
    wb.SaveAs(yol);
    File.WriteAllText(beklenenYol, beklenen.ToString(), new UTF8Encoding(true));
    return puanlar;
}

/// <summary>Proje / ödev ek puanları: notlara eklenecek 1-5 arası puanlar (öğrencilerin bir kısmı için).</summary>
static void ProjeNotlariYaz(Random r, string yol, List<Ogrenci> sinif)
{
    using var wb = new XLWorkbook();
    var ws = wb.AddWorksheet("Proje");
    ws.Cell(1, 1).Value = "OGRENCINO";
    ws.Cell(1, 2).Value = "ADSOYAD";
    ws.Cell(1, 3).Value = "SINAVNOTU";
    ws.Row(1).Style.Font.SetBold();
    var satir = 2;
    foreach (var o in sinif.Where(_ => r.NextDouble() < 0.6))
    {
        ws.Cell(satir, 1).Value = o.No;
        ws.Cell(satir, 1).Style.NumberFormat.Format = "@";
        ws.Cell(satir, 2).Value = Isimler.Buyuk(o.AdSoyad);
        ws.Cell(satir, 3).Value = Math.Clamp((int)Math.Round(3 + o.Yetenek * 1.2 + Normal(r, 0, 0.8)), 1, 5);
        satir++;
    }
    ws.Columns().AdjustToContents();
    wb.SaveAs(yol);
}

static void Ozet(string ad, IEnumerable<decimal> puanlar)
{
    var l = puanlar.Select(p => (double)p).Order().ToList();
    var ort = l.Average();
    var ss = Math.Sqrt(l.Sum(x => (x - ort) * (x - ort)) / l.Count);
    Console.WriteLine($"{ad}: {l.Count} giren · ort {ort:0.0} · medyan {l[l.Count / 2]:0.0} · ss {ss:0.0} · min {l[0]:0} · maks {l[^1]:0}");
}
