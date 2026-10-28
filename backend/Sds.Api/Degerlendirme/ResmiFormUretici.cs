using System.Globalization;
using System.Reflection;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Controllers;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Api.Degerlendirme;

/// <summary>
/// AMÜDEK / MÜDEK süreçleri için "Sınav Başarısı Analiz ve Değerlendirme Formu" (Excel).
/// Sayfalar: Analiz (ders ve sınav bilgileri, puan aralıkları, öğrenme çıktıları, değerlendirme),
/// Liste (öğrenci bazlı soru puanları) ve İÖK Kağıt (arşivlenecek iyi / orta / kötü örnek kağıtlar).
/// </summary>
public static class ResmiFormUretici
{
    private static readonly CultureInfo Tr = new("tr-TR");
    private static readonly XLColor Lacivert = XLColor.FromHtml("#0D2D5E");
    private static readonly XLColor BaslikZemin = XLColor.FromHtml("#D9E1F2");
    private static readonly XLColor AltinZemin = XLColor.FromHtml("#FFF2CC");

    public static async Task<(byte[] Icerik, string DosyaAdi)> UretAsync(SdsDbContext db, Sinav sinav)
    {
        var analiz = await SinavAnalizServisi.HesaplaAsync(db, sinav);
        var ders = await db.Dersler.AsNoTracking().Include(d => d.Egitmenler).ThenInclude(e => e.Kullanici).FirstAsync(d => d.Id == sinav.DersId);
        var sorular = await db.SinavSorulari.AsNoTracking().Where(q => q.SinavId == sinav.Id).OrderBy(q => q.SoruNo).ToListAsync();
        var harfler = await db.HarfNotuAraliklari.AsNoTracking().OrderBy(h => h.Sira).ToListAsync();
        var sonuclar = await db.SinavSonuclari.AsNoTracking().Include(s => s.Ogrenci).Include(s => s.Eklemeler).Include(s => s.SoruCevaplari)
            .Where(s => s.SinavId == sinav.Id).OrderBy(s => s.Ogrenci.OgrenciNo).ToListAsync();

        decimal Not(SinavSonucu s) => Math.Round(s.DuzeltilmisPuan(sinav.MaksimumPuan) * 100 / sinav.MaksimumPuan, 2);
        var girenler = sonuclar.Where(s => s.SinavaGirdi).ToList();
        var turAdi = sinav.Tur switch { SinavTuru.Vize => "Vize", SinavTuru.Final => "Final", _ => "Bütünleme" };
        var ogretimTuru = ders.OgretimTuru == OgretimTuru.NormalOgretim ? "NÖ" : "İÖ";
        var baslik = $"{ders.Ad.ToUpper(Tr)} ({ogretimTuru}) DERSİ";

        using var wb = new XLWorkbook();
        wb.Style.Font.FontName = "Cambria";
        wb.Style.Font.FontSize = 10;

        // ---------------------------------------------------------------- Analiz
        var ws = wb.AddWorksheet("Analiz");
        UstBilgi(ws, baslik, "1/3", 10);

        var r = 8;
        Bolum(ws, r, 1, 4, "DERS BİLGİLERİ");
        Bolum(ws, r, 6, 10, "SINAV BİLGİLERİ");
        var dersBilgi = new (string, string)[]
        {
            ("Adı", ders.Ad), ("Öğr. Türü", ders.OgretimTuru == OgretimTuru.NormalOgretim ? "NORMAL ÖĞRETİM" : "İKİNCİ ÖĞRETİM"),
            ("Kodu", ders.Kod), ("Öğr. Yılı", ders.AkademikYil), ("Sınıfı", ders.Sinif.ToString()),
            ("Dönemi", ders.Donem == Donem.Guz ? "GÜZ" : "BAHAR"),
            ("Öğr. Elm.", string.Join(", ", ders.Egitmenler.Select(e => e.Kullanici.TamAd))), ("Öğr. Sayısı", sonuclar.Count.ToString()),
        };
        var yuzlukler = girenler.Select(Not).ToList();
        var ort15Haric = yuzlukler.Where(p => p > 15).DefaultIfEmpty().Average();
        var sinavBilgi = new (string, string)[]
        {
            ("Türü", turAdi), ("Tarihi", sinav.Tarih?.ToString("dd.MM.yyyy") ?? ""),
            ("Grup Sayısı", sinav.KitapcikSayisi.ToString()), ("Soru Sayısı", sinav.SoruSayisi.ToString()),
            ("Öğr. Sayısı", girenler.Count.ToString()), ("Girmeyen Öğr. Sayısı", (sonuclar.Count - girenler.Count).ToString()),
            ("Std. Sapması", Sayi(analiz.Ozet.StandartSapma)), ("Ort (0-15 Dahil)", Sayi(analiz.Ozet.Ortalama)),
            ("Ort (0-15 Hariç)", Sayi(yuzlukler.Any(p => p > 15) ? Math.Round(ort15Haric, 2) : null)),
        };
        for (var i = 0; i < Math.Max(dersBilgi.Length, sinavBilgi.Length); i++)
        {
            if (i < dersBilgi.Length) Satir(ws, r + 1 + i, 1, dersBilgi[i].Item1, 2, 4, dersBilgi[i].Item2);
            if (i < sinavBilgi.Length) Satir(ws, r + 1 + i, 6, sinavBilgi[i].Item1, 7, 8, sinavBilgi[i].Item2);
        }

        // Puan aralıkları (harf tablosundan; FF aralığı 0-15 ve 16-49 olarak ikiye ayrılır)
        var araliklar = new List<(string Etiket, int Alt, int Ust)>();
        foreach (var h in harfler)
        {
            if (h.AltSinir == 0 && h.UstSinir > 15)
            {
                araliklar.Add(($"16 - {h.UstSinir}", 16, h.UstSinir));
                araliklar.Add(("0 - 15", 0, 15));
            }
            else araliklar.Add(($"{h.AltSinir} - {h.UstSinir}", h.AltSinir, h.UstSinir));
        }
        var ar = r + 11;
        Bolum(ws, ar, 1, 4, "PUAN ARALIKLARINA GÖRE DAĞILIM");
        Basliklar(ws, ar + 1, 1, ["ARALIK", "", "ÖĞ. SAY.", "YÜZDE"]);
        ws.Range(ar + 1, 1, ar + 1, 2).Merge();
        for (var i = 0; i < araliklar.Count; i++)
        {
            var (etiket, alt, ust) = araliklar[i];
            var sayi = yuzlukler.Count(p => { var y = (int)Math.Round(p, MidpointRounding.AwayFromZero); return y >= alt && y <= ust; });
            var satir = ar + 2 + i;
            ws.Range(satir, 1, satir, 2).Merge().Value = etiket;
            ws.Cell(satir, 3).Value = sayi;
            ws.Cell(satir, 4).Value = $"% {SinavAnalizServisi.Yuzde(sayi, girenler.Count).ToString("0.#", Tr)}";
            ws.Range(satir, 1, satir, 4).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        Cerceve(ws.Range(ar + 1, 1, ar + 1 + araliklar.Count, 4));

        // Başarı özeti (aralık tablosunun yanında)
        Bolum(ws, ar, 6, 10, "BAŞARI ÖZETİ");
        var basari = new (string, string)[]
        {
            ("Geçme Sınırı", $"{analiz.GecmeSiniri} puan"), ("Başarı Oranı", analiz.BasariOrani is { } bo ? $"% {bo.ToString("0.#", Tr)}" : "-"),
            ("Medyan", Sayi(analiz.Ozet.Medyan)), ("En Düşük Puan", Sayi(analiz.Ozet.EnDusuk)), ("En Yüksek Puan", Sayi(analiz.Ozet.EnYuksek)),
        };
        for (var i = 0; i < basari.Length; i++) Satir(ws, ar + 1 + i, 6, basari[i].Item1, 7, 8, basari[i].Item2);

        // Öğrenme çıktıları (tam genişlik)
        var cr = ar + 3 + araliklar.Count;
        Bolum(ws, cr, 1, 10, "ÖĞRENME ÇIKTILARI");
        Basliklar(ws, cr + 1, 1, ["No", "Öğrenme Çıktısı", "", "", "", "İlişkili Sorular", "", "", "Başarı", ""]);
        ws.Range(cr + 1, 2, cr + 1, 5).Merge(); ws.Range(cr + 1, 6, cr + 1, 8).Merge(); ws.Range(cr + 1, 9, cr + 1, 10).Merge();
        var ci = cr + 2;
        foreach (var c in analiz.Ciktilar)
        {
            ws.Cell(ci, 1).Value = $"ÖÇ{c.SiraNo}";
            var ac = ws.Range(ci, 2, ci, 5).Merge();
            ac.Value = c.Aciklama;
            ac.Style.Alignment.SetWrapText();
            ws.Range(ci, 6, ci, 8).Merge().Value = string.Join(", ", c.Sorular.Select(n => $"S{n}"));
            ws.Range(ci, 6, ci, 8).Style.Alignment.SetWrapText();
            ws.Range(ci, 9, ci, 10).Merge().Value = c.BasariOrani is { } b ? $"% {b.ToString("0.#", Tr)}" : "-";
            ws.Range(ci, 1, ci, 10).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            ws.Cell(ci, 1).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Range(ci, 9, ci, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Row(ci).Height = Math.Max(16, 14 * Math.Ceiling(c.Aciklama.Length / 70.0));
            ci++;
        }
        if (analiz.Ciktilar.Count == 0) ws.Range(ci, 2, ci++, 10).Merge().Value = "Öğrenme çıktısı ilişkilendirilmedi.";
        Cerceve(ws.Range(cr + 1, 1, ci - 1, 10));

        // Sınavın değerlendirmesi
        var dr = ci + 1;
        Bolum(ws, dr, 1, 10, "SINAVIN DEĞERLENDİRMESİ");
        var metin = string.IsNullOrWhiteSpace(sinav.DegerlendirmeMetni) ? OtomatikDegerlendirme(analiz) : sinav.DegerlendirmeMetni;
        var dm = ws.Range(dr + 1, 1, dr + 10, 10).Merge();
        dm.Value = metin;
        dm.Style.Alignment.SetWrapText().Alignment.SetVertical(XLAlignmentVerticalValues.Top);
        Cerceve(dm);

        var ir = dr + 12;
        ws.Range(ir, 6, ir, 10).Merge().Value = "Dersin Öğretim Elemanı";
        ws.Range(ir + 1, 6, ir + 1, 10).Merge().Value = string.Join(", ", ders.Egitmenler.Select(e => e.Kullanici.TamAd));
        ws.Range(ir + 3, 6, ir + 3, 10).Merge().Value = "İmza: ..............................";
        ws.Range(ir, 6, ir + 3, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Font.SetBold();

        ws.Column(1).Width = 11; ws.Column(2).Width = 34; ws.Column(3).Width = 13; ws.Column(4).Width = 13; ws.Column(5).Width = 2;
        ws.Column(6).Width = 12; ws.Column(7).Width = 10; ws.Column(8).Width = 14; ws.Column(9).Width = 8; ws.Column(10).Width = 8;
        Sayfa(ws, XLPageOrientation.Portrait);

        // ---------------------------------------------------------------- Liste
        var wl = wb.AddWorksheet("Liste");
        var sonSutun = 4 + sorular.Count;
        UstBilgi(wl, baslik, "2/3", sonSutun);
        wl.Range(8, 1, 8, 4).Merge().Value = "SORULARA GÖRE ORT. PUANLAR";
        wl.Range(8, 1, 8, 4).Style.Font.SetBold().Fill.SetBackgroundColor(BaslikZemin).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
        for (var i = 0; i < sorular.Count; i++)
        {
            var q = analiz.Sorular[i];
            wl.Cell(8, 5 + i).Value = Math.Round(q.OrtalamaPuan, 2);
            wl.Cell(9, 5 + i).Value = $"(%{q.BasariOrani.ToString("0", Tr)})";
        }
        wl.Range(8, 5, 9, sonSutun).Style.Font.SetFontSize(8).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Fill.SetBackgroundColor(AltinZemin);
        Basliklar(wl, 10, 1, ["S.N", "ÖĞR. NO", "ÖĞRENCİ ADI SOYADI", "PUAN", .. sorular.Select(q => $"S{q.SoruNo}")]);
        var lr = 11;
        var sira = 1;
        foreach (var s in girenler.Concat(sonuclar.Where(x => !x.SinavaGirdi)))
        {
            wl.Cell(lr, 1).Value = sira++;
            wl.Cell(lr, 2).Value = s.Ogrenci.OgrenciNo;
            wl.Cell(lr, 3).Value = s.Ogrenci.AdSoyad;
            if (s.SinavaGirdi)
            {
                wl.Cell(lr, 4).Value = Not(s);
                foreach (var c in s.SoruCevaplari) wl.Cell(lr, 4 + c.SoruNo).Value = c.Puan;
            }
            else
            {
                wl.Cell(lr, 4).Value = "GİRMEDİ";
                wl.Range(lr, 1, lr, sonSutun).Style.Font.SetFontColor(XLColor.Gray);
            }
            lr++;
        }
        Cerceve(wl.Range(10, 1, lr - 1, sonSutun));
        wl.Range(11, 4, lr - 1, sonSutun).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        wl.Column(1).Width = 5; wl.Column(2).Width = 12; wl.Column(3).Width = 28; wl.Column(4).Width = 9;
        for (var i = 0; i < sorular.Count; i++) wl.Column(5 + i).Width = 5.5;
        wl.SheetView.FreezeRows(10);
        Sayfa(wl, XLPageOrientation.Landscape);
        wl.PageSetup.SetRowsToRepeatAtTop(10, 10);

        // ---------------------------------------------------------------- İÖK Kağıt
        var wi = wb.AddWorksheet("İÖK Kağıt");
        UstBilgi(wi, baslik, "3/3", sonSutun);
        wi.Range(8, 1, 8, sonSutun).Merge().Value =
            "İYİ - ORTA - KÖTÜ ÖRNEK KAĞITLAR: Aşağıdaki öğrencilerin sınav kağıtları ders dosyasında örnek olarak arşivlenmelidir.";
        wi.Range(8, 1, 8, sonSutun).Style.Font.SetItalic().Alignment.SetWrapText();
        wi.Row(8).Height = 28;
        var sirali = girenler.OrderByDescending(Not).ThenBy(s => s.Ogrenci.OgrenciNo).ToList();
        var medyan = analiz.Ozet.Medyan ?? 0;
        var iyi = sirali.Take(3).ToList();
        var kotu = sirali.TakeLast(3).Except(iyi).ToList();
        var orta = sirali.Except(iyi).Except(kotu).OrderBy(s => Math.Abs(Not(s) - medyan)).Take(3).OrderByDescending(Not).ToList();
        var ir2 = 10;
        foreach (var (grup, liste) in new[] { ("İYİ", iyi), ("ORTA", orta), ("KÖTÜ", kotu) })
        {
            wi.Range(ir2, 1, ir2, sonSutun).Merge().Value = $"{grup} ÖRNEK KAĞITLAR";
            wi.Range(ir2, 1, ir2, sonSutun).Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(Lacivert);
            Basliklar(wi, ir2 + 1, 1, ["S.N", "ÖĞR. NO", "ÖĞRENCİ ADI SOYADI", "PUAN", .. sorular.Select(q => $"S{q.SoruNo}")]);
            var k = ir2 + 2;
            var n = 1;
            foreach (var s in liste)
            {
                wi.Cell(k, 1).Value = n++;
                wi.Cell(k, 2).Value = s.Ogrenci.OgrenciNo;
                wi.Cell(k, 3).Value = s.Ogrenci.AdSoyad;
                wi.Cell(k, 4).Value = Not(s);
                foreach (var c in s.SoruCevaplari) wi.Cell(k, 4 + c.SoruNo).Value = c.Puan;
                k++;
            }
            Cerceve(wi.Range(ir2 + 1, 1, Math.Max(k - 1, ir2 + 1), sonSutun));
            wi.Range(ir2 + 2, 4, Math.Max(k - 1, ir2 + 2), sonSutun).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ir2 = k + 1;
        }
        wi.Column(1).Width = 5; wi.Column(2).Width = 12; wi.Column(3).Width = 28; wi.Column(4).Width = 9;
        for (var i = 0; i < sorular.Count; i++) wi.Column(5 + i).Width = 5.5;
        Sayfa(wi, XLPageOrientation.Landscape);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return (ms.ToArray(), $"{ders.Kod}_{Ascii(turAdi)}_Sinav_Basarisi_Analiz_Formu.xlsx");
    }

    /// <summary>
    /// Öğretim üyesi metin girmediyse forma yazılan, sayılara dayalı değerlendirme taslağı.
    /// (Final tesliminde bu metin AI ile zenginleştirilecek.)
    /// </summary>
    public static string OtomatikDegerlendirme(SinavAnaliziYaniti a)
    {
        var o = a.Ozet;
        if (o.Giren == 0) return "Sınava giren öğrenci bulunmamaktadır.";
        var acikUclu = a.Genel.Format == nameof(SinavFormati.AcikUclu);
        var sb = new System.Text.StringBuilder();
        sb.Append($"Sınava kayıtlı {o.Toplam} öğrenciden {o.Giren} öğrenci katılmış, {o.Toplam - o.Giren} öğrenci sınava girmemiştir. ");
        sb.Append($"Sınav ortalaması {Sayi(o.Ortalama)}, medyan {Sayi(o.Medyan)}, standart sapma {Sayi(o.StandartSapma)} olarak hesaplanmıştır; ");
        sb.Append($"{a.GecmeSiniri} ve üzeri puan alan öğrenci oranı %{Sayi(a.BasariOrani)} olarak gerçekleşmiştir.\n\n");

        var zor = a.Sorular.OrderBy(q => q.BasariOrani).Take(3).ToList();
        var kolay = a.Sorular.OrderByDescending(q => q.BasariOrani).Take(3).ToList();
        sb.Append($"Sınav sorularında en düşük oranda {(acikUclu ? "puan alınan" : "cevaplanan")} sorular: {string.Join(", ", zor.Select(q => $"S{q.SoruNo} (%{Sayi(q.BasariOrani)})"))}. ");
        sb.Append($"En yüksek oranda {(acikUclu ? "puan alınan" : "cevaplanan")} sorular: {string.Join(", ", kolay.Select(q => $"S{q.SoruNo} (%{Sayi(q.BasariOrani)})"))}.");

        // Doğru oranı çok düşükken tek bir yanlış şıkta toplanma: anahtar / soru ifadesi kontrol edilmeli
        foreach (var q in a.Sorular.Where(q => q.BasariOrani < 20 && q.SikOranlari.Count > 0))
        {
            var celdirici = q.SikOranlari.Where(s => s.Key.Length == 1 && q.DogruCevap?.Contains(s.Key) != true).OrderByDescending(s => s.Value).First();
            if (celdirici.Value >= 50)
                sb.Append($"\nS{q.SoruNo} sorusunda öğrencilerin büyük bölümü (%{Sayi(celdirici.Value)}) {celdirici.Key} şıkkını işaretlemiştir (cevap anahtarı: {q.DogruCevap}); sorunun ifadesi ve cevap anahtarı gözden geçirilmelidir.");
        }

        var ciktilar = a.Ciktilar.Where(c => c.BasariOrani.HasValue).ToList();
        if (ciktilar.Count > 0)
        {
            var yuksek = ciktilar.MaxBy(c => c.BasariOrani)!;
            var dusuk = ciktilar.MinBy(c => c.BasariOrani)!;
            sb.Append($"\n\nÖğrenme çıktıları arasında en yüksek başarı ÖÇ{yuksek.SiraNo} (%{Sayi(yuksek.BasariOrani)}), en düşük başarı ÖÇ{dusuk.SiraNo} (%{Sayi(dusuk.BasariOrani)}) olarak gerçekleşmiştir.");
            var yetersiz = ciktilar.Where(c => c.BasariOrani < 50).Select(c => $"ÖÇ{c.SiraNo}").ToList();
            if (yetersiz.Count > 0) sb.Append($" {string.Join(", ", yetersiz)} ile ilgili konuların sonraki dönemde pekiştirilmesi önerilir.");
        }
        return sb.ToString();
    }

    private static void UstBilgi(IXLWorksheet ws, string dersBasligi, string sayfaNo, int sonSutun)
    {
        using (var logo = Assembly.GetExecutingAssembly().GetManifestResourceStream("selcuk-logo.png"))
            if (logo is not null) ws.AddPicture(logo).MoveTo(ws.Cell(1, 1), 4, 2).WithSize(78, 78);

        var orta = Math.Max(3, sonSutun - 3);
        string[] satirlar = ["T.C.", "SELÇUK ÜNİVERSİTESİ", "BİLGİSAYAR MÜHENDİSLİĞİ BÖLÜMÜ", dersBasligi, "SINAV BAŞARISI ANALİZ VE DEĞERLENDİRME FORMU"];
        for (var i = 0; i < satirlar.Length; i++)
        {
            var rg = ws.Range(1 + i, 2, 1 + i, orta).Merge();
            rg.Value = satirlar[i];
            rg.Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        ws.Range(5, 2, 5, orta).Style.Font.SetFontColor(Lacivert).Font.SetFontSize(11);

        // Doküman künyesi: bölüm kalite birimi tarafından doldurulur
        string[] kunye = ["Doküman No", "Yayın Tarihi", "Revizyon No", "Sayfa No"];
        for (var i = 0; i < kunye.Length; i++)
        {
            ws.Cell(1 + i, orta + 1).Value = kunye[i];
            ws.Cell(1 + i, orta + 1).Style.Font.SetFontSize(8);
            var deger = ws.Range(1 + i, orta + 2, 1 + i, sonSutun).Merge();
            deger.Value = i == 3 ? sayfaNo : "";
            deger.Style.Font.SetFontSize(8);
        }
        Cerceve(ws.Range(1, orta + 1, 4, sonSutun));
        for (var i = 1; i <= 5; i++) ws.Row(i).Height = 16;
    }

    private static void Bolum(IXLWorksheet ws, int satir, int bas, int son, string ad)
    {
        var rg = ws.Range(satir, bas, satir, son).Merge();
        rg.Value = ad;
        rg.Style.Font.SetBold().Font.SetFontColor(XLColor.White).Fill.SetBackgroundColor(Lacivert).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void Satir(IXLWorksheet ws, int satir, int etiketSutun, string etiket, int degerBas, int degerSon, string deger)
    {
        ws.Cell(satir, etiketSutun).Value = $"{etiket}:";
        ws.Cell(satir, etiketSutun).Style.Font.SetBold();
        var rg = ws.Range(satir, degerBas, satir, degerSon).Merge();
        rg.Value = deger;
        rg.Style.Alignment.SetShrinkToFit();
        Cerceve(ws.Range(satir, etiketSutun, satir, degerSon));
    }

    private static void Basliklar(IXLWorksheet ws, int satir, int bas, IReadOnlyList<string> basliklar)
    {
        for (var i = 0; i < basliklar.Count; i++) ws.Cell(satir, bas + i).Value = basliklar[i];
        ws.Range(satir, bas, satir, bas + basliklar.Count - 1).Style.Font.SetBold().Fill.SetBackgroundColor(BaslikZemin)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
    }

    private static void Cerceve(IXLRange rg) =>
        rg.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);

    private static void Sayfa(IXLWorksheet ws, XLPageOrientation yon)
    {
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.PageOrientation = yon;
        ws.PageSetup.FitToPages(1, 0);
        ws.PageSetup.Margins.SetLeft(0.4).SetRight(0.4).SetTop(0.5).SetBottom(0.5);
    }

    private static string Sayi(decimal? d) => d is null ? "-" : d.Value.ToString("0.##", Tr);

    private static string Ascii(string s) => s.Replace('ü', 'u').Replace('Ü', 'U').Replace('ı', 'i').Replace('İ', 'I');
}
