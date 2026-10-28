using ClosedXML.Excel;
using Sds.Api.Controllers;
using static Sds.Api.Degerlendirme.ResmiFormUretici;

namespace Sds.Api.Degerlendirme;

/// <summary>Dönem Analiz Raporu Excel çıktısı: Kurs Özeti, Öğrenci Puanları, Sınav İstatistikleri, Öğrenme Çıktıları.</summary>
public static class DonemRaporUretici
{
    private const string FormAdi = "DÖNEM ANALİZ RAPORU";

    public static (byte[] Icerik, string DosyaAdi) Uret(DonemAnaliziYaniti a)
    {
        var g = a.Genel;
        var o = a.Ozet;
        var ogretimTuru = g.OgretimTuru == "NormalOgretim" ? "NÖ" : "İÖ";
        var baslik = $"{g.DersAdi.ToUpper(Tr)} ({ogretimTuru}) DERSİ";
        string Yuzde(decimal? d) => d is { } v ? $"% {v.ToString("0.#", Tr)}" : "-";

        using var wb = new XLWorkbook();
        wb.Style.Font.FontName = "Cambria";
        wb.Style.Font.FontSize = 10;

        // ---------------------------------------------------------------- Kurs Özeti
        var ws = wb.AddWorksheet("Kurs Özeti");
        UstBilgi(ws, baslik, "1/4", 10, FormAdi);
        Bolum(ws, 8, 1, 4, "DERS BİLGİLERİ");
        Bolum(ws, 8, 6, 10, "DEĞERLENDİRME ESASLARI");
        var dersBilgi = new (string, string)[]
        {
            ("Adı", g.DersAdi), ("Kodu", g.DersKodu), ("Öğr. Yılı", g.AkademikYil), ("Dönemi", g.Donem == "Guz" ? "GÜZ" : "BAHAR"),
            ("Öğr. Türü", g.OgretimTuru == "NormalOgretim" ? "NORMAL ÖĞRETİM" : "İKİNCİ ÖĞRETİM"), ("Sınıfı", g.Sinif.ToString()),
            ("Öğr. Elm.", string.Join(", ", g.Egitmenler)),
        };
        var esaslar = new (string, string)[]
        {
            ("Vize Ağırlığı", $"% {g.VizeAgirligi}"), ("Final Ağırlığı", $"% {g.FinalAgirligi}"),
            ("Bütünleme", "Finalin yerine geçer"), ("Final Alt Sınırı", $"{g.FinalAltSiniri} puan"), ("Geçme Notu", $"{g.GecmeSiniri} (DD)"),
            ("Harf Notu", "Mutlak değerlendirme"),
        };
        for (var i = 0; i < dersBilgi.Length; i++) Satir(ws, 9 + i, 1, dersBilgi[i].Item1, 2, 4, dersBilgi[i].Item2);
        for (var i = 0; i < esaslar.Length; i++) Satir(ws, 9 + i, 6, esaslar[i].Item1, 7, 10, esaslar[i].Item2);

        Bolum(ws, 17, 1, 4, "DÖNEM BAŞARI ÖZETİ");
        var ozet = new (string, string)[]
        {
            ("Toplam Öğrenci", o.Toplam.ToString()), ("Ağırlıklı Ortalama", Sayi(o.AgirlikliOrtalama)), ("Medyan", Sayi(o.Medyan)),
            ("Std. Sapma", Sayi(o.StandartSapma)), ("En Düşük / En Yüksek", $"{Sayi(o.EnDusuk)} / {Sayi(o.EnYuksek)}"),
            ("Başarı Oranı", Yuzde(o.BasariOrani)),
            ("Başarılı", $"{o.Basarili} ({Yuzde(Oran(o.Basarili, o.Toplam))})"),
            ("Şartlı Geçen", $"{o.SartliGecen} ({Yuzde(Oran(o.SartliGecen, o.Toplam))})"),
            ("Başarısız", $"{o.Basarisiz} ({Yuzde(Oran(o.Basarisiz, o.Toplam))})"),
            ("Ort. Katsayı", Sayi(o.OrtalamaKatsayi)),
        };
        for (var i = 0; i < ozet.Length; i++) Satir(ws, 18 + i, 1, ozet[i].Item1, 2, 4, ozet[i].Item2);

        Bolum(ws, 17, 6, 10, "HARF NOTU DAĞILIMI");
        Basliklar(ws, 18, 6, ["HARF", "DURUM", "", "SAYI", "YÜZDE"]);
        ws.Range(18, 7, 18, 8).Merge();
        for (var i = 0; i < a.HarfDagilimi.Count; i++)
        {
            var h = a.HarfDagilimi[i];
            var r = 19 + i;
            ws.Cell(r, 6).Value = h.Harf;
            ws.Range(r, 7, r, 8).Merge().Value = h.Durum;
            ws.Cell(r, 9).Value = h.Sayi;
            ws.Cell(r, 10).Value = Yuzde(Oran(h.Sayi, o.Toplam));
            ws.Range(r, 6, r, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        Cerceve(ws.Range(18, 6, 18 + a.HarfDagilimi.Count, 10));

        var kr = 19 + Math.Max(ozet.Length, a.HarfDagilimi.Count + 1) + 1;
        Bolum(ws, kr, 1, 10, "SINAV ORTALAMALARI KARŞILAŞTIRMASI");
        Basliklar(ws, kr + 1, 1, ["SINAV", "", "GİREN", "ORTALAMA", "", "MEDYAN", "STD. SAPMA", "BAŞARI ORANI", "", ""]);
        ws.Range(kr + 1, 1, kr + 1, 2).Merge(); ws.Range(kr + 1, 4, kr + 1, 5).Merge(); ws.Range(kr + 1, 8, kr + 1, 10).Merge();
        var satirlar = a.Sinavlar.Select(s => (s.Sinav, $"{s.Giren} / {s.Toplam}", s.Ortalama, s.Medyan, s.StandartSapma, s.BasariOrani))
            .Append(("Ağırlıklı Ortalama (Dönem)", o.Toplam.ToString(), o.AgirlikliOrtalama, o.Medyan, o.StandartSapma, o.BasariOrani)).ToList();
        for (var i = 0; i < satirlar.Count; i++)
        {
            var (ad, giren, ort, med, ss, bo) = satirlar[i];
            var r = kr + 2 + i;
            ws.Range(r, 1, r, 2).Merge().Value = ad;
            ws.Cell(r, 3).Value = giren;
            ws.Range(r, 4, r, 5).Merge().Value = Sayi(ort);
            ws.Cell(r, 6).Value = Sayi(med);
            ws.Cell(r, 7).Value = Sayi(ss);
            ws.Range(r, 8, r, 10).Merge().Value = Yuzde(bo);
            ws.Range(r, 3, r, 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }
        ws.Range(kr + 1 + satirlar.Count, 1, kr + 1 + satirlar.Count, 10).Style.Font.SetBold();
        Cerceve(ws.Range(kr + 1, 1, kr + 1 + satirlar.Count, 10));

        ws.Column(1).Width = 18; ws.Column(2).Width = 22; ws.Column(3).Width = 12; ws.Column(4).Width = 12; ws.Column(5).Width = 2;
        ws.Column(6).Width = 15; ws.Column(7).Width = 11; ws.Column(8).Width = 11; ws.Column(9).Width = 9; ws.Column(10).Width = 10;
        Sayfa(ws, XLPageOrientation.Portrait);

        // ---------------------------------------------------------------- Öğrenci Puanları
        var wo = wb.AddWorksheet("Öğrenci Puanları");
        UstBilgi(wo, baslik, "2/4", 11, FormAdi);
        Basliklar(wo, 8, 1, ["S.N", "ÖĞR. NO", "ÖĞRENCİ ADI SOYADI", "VİZE", "FİNAL", "BÜTÜNLEME", "ETKİN SINAV", "AĞIRLIKLI ORT.", "HARF", "DURUM", "AÇIKLAMA"]);
        var or = 9;
        foreach (var s in a.Ogrenciler)
        {
            wo.Cell(or, 1).Value = or - 8;
            wo.Cell(or, 2).Value = s.OgrenciNo;
            wo.Cell(or, 3).Value = s.AdSoyad;
            Puan(wo.Cell(or, 4), s.Vize, true);
            Puan(wo.Cell(or, 5), s.Final, true);
            Puan(wo.Cell(or, 6), s.Butunleme, s.ButunlemeListesinde);
            wo.Cell(or, 7).Value = s.EtkinSinav ?? "-";
            wo.Cell(or, 8).Value = s.Agirlikli;
            wo.Cell(or, 8).Style.NumberFormat.Format = "0.00";
            wo.Cell(or, 9).Value = s.Harf;
            wo.Cell(or, 10).Value = s.Durum;
            wo.Cell(or, 11).Value = s.Aciklama ?? "";
            wo.Cell(or, 9).Style.Font.SetBold();
            if (s.Durum == "Başarısız") wo.Range(or, 9, or, 10).Style.Font.SetFontColor(XLColor.FromHtml("#B91C1C"));
            else if (s.Durum == "Şartlı Geçer") wo.Range(or, 9, or, 10).Style.Font.SetFontColor(XLColor.FromHtml("#B45309"));
            or++;
        }
        Cerceve(wo.Range(8, 1, Math.Max(or - 1, 8), 11));
        wo.Range(9, 4, Math.Max(or - 1, 9), 10).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        if (a.Ogrenciler.Count > 0) wo.Range(8, 1, or - 1, 11).SetAutoFilter();
        wo.SheetView.FreezeRows(8);
        wo.Column(1).Width = 5; wo.Column(2).Width = 12; wo.Column(3).Width = 26;
        for (var c = 4; c <= 8; c++) wo.Column(c).Width = 11;
        wo.Column(6).Width = 12; wo.Column(9).Width = 7; wo.Column(10).Width = 13; wo.Column(11).Width = 36;
        Sayfa(wo, XLPageOrientation.Landscape);
        wo.PageSetup.SetRowsToRepeatAtTop(8, 8);

        // ---------------------------------------------------------------- Sınav İstatistikleri
        var wsi = wb.AddWorksheet("Sınav İstatistikleri");
        UstBilgi(wsi, baslik, "3/4", 12, FormAdi);
        Basliklar(wsi, 8, 1, ["SINAV", "FORMAT", "TARİH", "SORU SAYISI", "KAYITLI", "GİREN", "GİRMEYEN", "ORTALAMA", "MEDYAN", "STD. SAPMA", "MİN / MAKS", "BAŞARI ORANI"]);
        var sr = 9;
        foreach (var s in a.Sinavlar)
        {
            wsi.Cell(sr, 1).Value = s.Sinav;
            wsi.Cell(sr, 2).Value = s.Format == "AcikUclu" ? "Açık Uçlu" : "Çoktan Seçmeli";
            wsi.Cell(sr, 3).Value = s.Tarih?.ToString("dd.MM.yyyy") ?? "-";
            wsi.Cell(sr, 4).Value = s.SoruSayisi;
            wsi.Cell(sr, 5).Value = s.Toplam;
            wsi.Cell(sr, 6).Value = s.Giren;
            wsi.Cell(sr, 7).Value = s.Toplam - s.Giren;
            wsi.Cell(sr, 8).Value = Sayi(s.Ortalama);
            wsi.Cell(sr, 9).Value = Sayi(s.Medyan);
            wsi.Cell(sr, 10).Value = Sayi(s.StandartSapma);
            wsi.Cell(sr, 11).Value = $"{Sayi(s.EnDusuk)} / {Sayi(s.EnYuksek)}";
            wsi.Cell(sr, 12).Value = Yuzde(s.BasariOrani);
            sr++;
        }
        Cerceve(wsi.Range(8, 1, Math.Max(sr - 1, 8), 12));
        wsi.Range(9, 2, Math.Max(sr - 1, 9), 12).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        wsi.Cell(sr + 1, 1).Value = $"Başarı oranı: 100 üzerinden {g.GecmeSiniri} ve üzeri puan alan öğrencilerin sınava girenlere oranıdır.";
        wsi.Cell(sr + 1, 1).Style.Font.SetItalic().Font.SetFontSize(9);
        wsi.Column(1).Width = 13; wsi.Column(2).Width = 15;
        for (var c = 3; c <= 12; c++) wsi.Column(c).Width = 11;
        wsi.Column(11).Width = 13; wsi.Column(12).Width = 13;
        Sayfa(wsi, XLPageOrientation.Landscape);

        // ---------------------------------------------------------------- Öğrenme Çıktıları
        var wc = wb.AddWorksheet("Öğrenme Çıktıları");
        var sinavAdlari = a.Sinavlar.Select(s => s.Sinav).ToList();
        var son = 3 + sinavAdlari.Count;
        UstBilgi(wc, baslik, "4/4", Math.Max(son, 6), FormAdi);
        Basliklar(wc, 8, 1, ["NO", "ÖĞRENME ÇIKTISI", .. sinavAdlari.Select(s => $"{s.ToUpper(Tr)} BAŞARISI"), "DÖNEM BAŞARISI"]);
        var cr = 9;
        foreach (var c in a.Ciktilar)
        {
            wc.Cell(cr, 1).Value = $"ÖÇ{c.SiraNo}";
            wc.Cell(cr, 2).Value = c.Aciklama;
            wc.Cell(cr, 2).Style.Alignment.SetWrapText();
            for (var i = 0; i < sinavAdlari.Count; i++)
            {
                var b = c.Sinavlar.FirstOrDefault(x => x.Sinav == sinavAdlari[i]);
                wc.Cell(cr, 3 + i).Value = b is null ? "-" : $"{Yuzde(b.Basari)} ({b.SoruSayisi} soru)";
            }
            wc.Cell(cr, son).Value = Yuzde(c.DonemBasarisi);
            wc.Cell(cr, son).Style.Font.SetBold();
            if (c.DonemBasarisi < 50) wc.Cell(cr, son).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FDE2E2"));
            wc.Row(cr).Height = Math.Max(16, 14 * Math.Ceiling(c.Aciklama.Length / 55.0));
            cr++;
        }
        if (a.Ciktilar.Count == 0) wc.Cell(cr++, 2).Value = "Bu ders için öğrenme çıktısı tanımlanmadı.";
        Cerceve(wc.Range(8, 1, cr - 1, son));
        wc.Range(9, 1, cr - 1, son).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        wc.Range(9, 3, cr - 1, son).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        wc.Cell(9, 1).WorksheetColumn().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        wc.Range(8, 1, 8, son).Style.Alignment.SetWrapText();
        wc.Row(8).Height = 28;
        wc.Cell(cr + 1, 1).Value = "Dönem başarısı, çıktının ölçüldüğü sınavlardaki başarı oranlarının ortalamasıdır. %50'nin altındaki çıktılar işaretlenmiştir.";
        wc.Cell(cr + 1, 1).Style.Font.SetItalic().Font.SetFontSize(9);
        wc.Column(1).Width = 7; wc.Column(2).Width = 48;
        for (var c = 3; c <= son; c++) wc.Column(c).Width = 16;
        Sayfa(wc, XLPageOrientation.Landscape);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return (ms.ToArray(), $"{g.DersKodu}_{g.AkademikYil}_Donem_Analiz_Raporu.xlsx");
    }

    private static decimal? Oran(int sayi, int toplam) => toplam == 0 ? null : SinavAnalizServisi.Yuzde(sayi, toplam);

    /// <summary>Girmeyen öğrenci "G" ile, sınav listesinde olmayan öğrenci boş bırakılır.</summary>
    private static void Puan(IXLCell hucre, decimal? puan, bool listede)
    {
        if (puan is { } p) { hucre.Value = p; hucre.Style.NumberFormat.Format = "0.##"; }
        else hucre.Value = listede ? "G" : "";
    }
}
