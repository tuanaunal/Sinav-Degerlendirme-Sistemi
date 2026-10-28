using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Degerlendirme;
using Sds.Api.Domain;
using Sds.Api.Dosyalar;

namespace Sds.Api.Controllers;

public record SoruAyarlari(int SoruSayisi, decimal MaksimumPuan, bool PuanlariDinamikDagit, List<SoruPuani> Sorular);
public record NotYuklemeSonucu(int Giren, int Girmeyen, List<string> ListedeOlmayanlar, List<string> Uyarilar);

/// <summary>
/// Açık uçlu sınav: 2. adım soru sayısı ve soru puanları, 3. adım soru bazlı puanların Excel'den yüklenmesi.
/// 1. adım (sınıf listesi) ve 4-5. adımlar çoktan seçmeli sınavla ortaktır.
/// </summary>
[ApiController]
[Route("api/sinavlar/{id:int}/acik-uclu")]
[Authorize(Policy = Yetki.OgretimUyesi)]
public class AcikUcluController(SdsDbContext db) : ControllerBase
{
    [HttpGet("soru-ayarlari")]
    public async Task<ActionResult<SoruAyarlari>> AyarGetir(int id)
    {
        var sinav = await AcikUcluSinav(id);
        if (sinav is null) return NotFound(new { mesaj = "Açık uçlu sınav bulunamadı." });

        var sorular = await db.SinavSorulari.Where(s => s.SinavId == id).OrderBy(s => s.SoruNo).ToListAsync();
        var n = sinav.SoruSayisi > 0 ? sinav.SoruSayisi : 10;
        var puanlar = sorular.Count == n
            ? sorular.Select(s => new SoruPuani(s.SoruNo, s.Puan)).ToList()
            : PuanDagitimi.EsitDagit(sinav.MaksimumPuan, n).Select((p, i) => new SoruPuani(i + 1, p)).ToList();
        return new SoruAyarlari(n, sinav.MaksimumPuan, sinav.PuanlariDinamikDagit, puanlar);
    }

    [HttpPut("soru-ayarlari")]
    public async Task<ActionResult<SinavBilgisi>> AyarKaydet(int id, SoruAyarlari istek)
    {
        var sinav = await AcikUcluSinav(id);
        if (sinav is null) return NotFound(new { mesaj = "Açık uçlu sınav bulunamadı." });
        if (sinav.TamamlananAdim < 1) return BadRequest(new { mesaj = "Önce sınıf listesini yükleyin." });
        if (istek.SoruSayisi is < 1 or > 50) return BadRequest(new { mesaj = "Soru sayısı 1 ile 50 arasında olmalıdır." });
        if (istek.MaksimumPuan is <= 0 or > 1000) return BadRequest(new { mesaj = "Maksimum puan 0'dan büyük ve en fazla 1000 olmalıdır." });

        decimal[] puanlar;
        if (istek.PuanlariDinamikDagit) puanlar = PuanDagitimi.EsitDagit(istek.MaksimumPuan, istek.SoruSayisi);
        else
        {
            if (istek.Sorular is null || istek.Sorular.Count != istek.SoruSayisi || istek.Sorular.Any(s => s.Puan <= 0))
                return BadRequest(new { mesaj = "Her sorunun puanı 0'dan büyük olmalıdır." });
            puanlar = istek.Sorular.OrderBy(s => s.SoruNo).Select(s => s.Puan).ToArray();
            if (Math.Abs(puanlar.Sum() - istek.MaksimumPuan) > 0.01m)
                return BadRequest(new { mesaj = $"Soru puanlarının toplamı ({puanlar.Sum():0.##}) maksimum puana ({istek.MaksimumPuan:0.##}) eşit olmalıdır." });
        }

        var soruSayisiDegisti = sinav.SoruSayisi != istek.SoruSayisi;
        db.SinavSorulari.RemoveRange(await db.SinavSorulari.Where(s => s.SinavId == id).ToListAsync());
        for (var i = 0; i < puanlar.Length; i++)
            db.SinavSorulari.Add(new SinavSorusu { SinavId = id, SoruNo = i + 1, Puan = puanlar[i] });

        sinav.SoruSayisi = istek.SoruSayisi;
        sinav.MaksimumPuan = istek.MaksimumPuan;
        sinav.PuanlariDinamikDagit = istek.PuanlariDinamikDagit;
        sinav.KitapcikSayisi = 1;
        sinav.KitapcikEslestirmeKaydedildi = true;
        // Soru sayısı değişirse yüklenmiş notlar geçersizdir; not yükleme adımı tekrar yapılmalı
        sinav.TamamlananAdim = soruSayisiDegisti ? 2 : Math.Max(sinav.TamamlananAdim, 2);
        sinav.GuncellemeTarihi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await Bilgi(sinav);
    }

    /// <summary>Sınıf listesinden üretilmiş boş not şablonu: OGRENCINO, ADSOYAD, S1 … Sn.</summary>
    [HttpGet("sablon")]
    public async Task<IActionResult> Sablon(int id)
    {
        var sinav = await AcikUcluSinav(id);
        if (sinav is null) return NotFound(new { mesaj = "Açık uçlu sınav bulunamadı." });
        var sorular = await db.SinavSorulari.Where(s => s.SinavId == id).OrderBy(s => s.SoruNo).ToListAsync();
        var ogrenciler = await db.SinavSonuclari.Where(s => s.SinavId == id).Select(s => s.Ogrenci)
            .OrderBy(o => o.OgrenciNo).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Notlar");
        ws.Cell(1, 1).Value = "OGRENCINO";
        ws.Cell(1, 2).Value = "ADSOYAD";
        for (var i = 0; i < sorular.Count; i++)
        {
            ws.Cell(1, i + 3).Value = $"S{i + 1}";
            ws.Cell(1, i + 3).CreateComment().AddText($"En fazla {sorular[i].Puan.ToString("0.##", CultureInfo.InvariantCulture)} puan");
        }
        ws.Row(1).Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#FFF2CC"));
        for (var r = 0; r < ogrenciler.Count; r++)
        {
            ws.Cell(r + 2, 1).Value = ogrenciler[r].OgrenciNo;
            ws.Cell(r + 2, 1).Style.NumberFormat.Format = "@";
            ws.Cell(r + 2, 2).Value = ogrenciler[r].AdSoyad;
        }
        ws.Columns().AdjustToContents();
        ws.SheetView.FreezeRows(1);

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Acik_Uclu_Not_Sablonu_{sinav.Id}.xlsx");
    }

    /// <summary>
    /// 3. adım: soru bazlı puanlar (S1 … Sn). Satırı tamamen boş olan veya dosyada bulunmayan öğrenci sınava girmemiş sayılır.
    /// Her puan 0 ile o sorunun puanı arasında olmalıdır.
    /// </summary>
    [HttpPost("notlar")]
    [RequestSizeLimit(DosyalarController.EnBuyukBoyut)]
    public async Task<ActionResult<NotYuklemeSonucu>> NotYukle(int id, IFormFile dosya)
    {
        var sinav = await AcikUcluSinav(id);
        if (sinav is null) return NotFound(new { mesaj = "Açık uçlu sınav bulunamadı." });
        if (sinav.TamamlananAdim < 2) return BadRequest(new { mesaj = "Önce soru sayısını kaydedin." });
        if (dosya is null || dosya.Length == 0 || !dosya.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { mesaj = "OGRENCINO, ADSOYAD ve S1…Sn sütunlarını içeren bir .xlsx dosyası yükleyin." });

        NotTablosuSonucu tablo;
        try
        {
            using var akis = dosya.OpenReadStream();
            tablo = ExcelOkuyucu.NotTablosuOku(akis);
        }
        catch (DosyaOkumaHatasi e)
        {
            return BadRequest(new { mesaj = e.Message });
        }

        var sorular = await db.SinavSorulari.Where(s => s.SinavId == id).OrderBy(s => s.SoruNo).ToListAsync();
        // S1, Soru1, 1 … biçimlerindeki sütunları soru numarasına çevir
        var sutunlar = new Dictionary<int, string>();
        foreach (var b in tablo.Sutunlar)
        {
            var rakam = new string(Metin.Anahtar(b).Where(char.IsDigit).ToArray());
            if (int.TryParse(rakam, out var no) && no >= 1 && no <= sorular.Count) sutunlar.TryAdd(no, b);
        }
        var eksik = Enumerable.Range(1, sorular.Count).Where(no => !sutunlar.ContainsKey(no)).ToList();
        if (eksik.Count > 0)
            return BadRequest(new { mesaj = $"Dosyada şu soruların sütunu yok: {string.Join(", ", eksik.Select(n => $"S{n}"))}. Soru sayısı {sorular.Count} olarak ayarlı." });

        var hatalar = new List<string>();
        foreach (var satir in tablo.Satirlar)
            foreach (var q in sorular)
                if (satir.Degerler[sutunlar[q.SoruNo]] is { } p && (p < 0 || p > q.Puan))
                    hatalar.Add($"{satir.OgrenciNo} S{q.SoruNo}: {p:0.##} (en fazla {q.Puan:0.##})");
        if (hatalar.Count > 0)
            return BadRequest(new { mesaj = $"Soru puanını aşan veya negatif değerler var: {string.Join("; ", hatalar.Take(5))}{(hatalar.Count > 5 ? $" ve {hatalar.Count - 5} tane daha" : "")}." });

        var satirlar = tablo.Satirlar.GroupBy(s => s.OgrenciNo).ToDictionary(g => g.Key, g => g.First());
        var sonuclar = await db.SinavSonuclari.Include(s => s.Ogrenci).Include(s => s.SoruCevaplari)
            .Where(s => s.SinavId == id).ToListAsync();

        int giren = 0, girmeyen = 0;
        foreach (var s in sonuclar)
        {
            db.SoruCevaplari.RemoveRange(s.SoruCevaplari);
            s.SoruCevaplari.Clear();
            if (!satirlar.TryGetValue(s.Ogrenci.OgrenciNo, out var satir) || satir.Degerler.Values.All(v => v is null))
            {
                (s.Eslesme, s.HamPuan, s.DogruSayisi, s.YanlisSayisi, s.BosSayisi) = (EslesmeDurumu.SinavaGirmedi, 0, 0, 0, 0);
                girmeyen++;
                continue;
            }

            s.Eslesme = EslesmeDurumu.NotDosyasindan;
            s.Kitapcik = 'A';
            int tam = 0, kismi = 0, bos = 0;
            foreach (var q in sorular)
            {
                var p = satir.Degerler[sutunlar[q.SoruNo]];
                if (p is null) bos++;
                else if (p == q.Puan) tam++;
                else kismi++;
                // Açık uçluda "doğru" = tam puan alınmış soru; analizlerde başarı oranı puan / soru puanı ile hesaplanır
                s.SoruCevaplari.Add(new SoruCevabi { SoruNo = q.SoruNo, Puan = p ?? 0, DogruMu = p is null ? null : p == q.Puan });
            }
            (s.DogruSayisi, s.YanlisSayisi, s.BosSayisi) = (tam, kismi, bos);
            s.HamPuan = s.SoruCevaplari.Sum(c => c.Puan);
            giren++;
        }

        var listeNumaralari = sonuclar.Select(s => s.Ogrenci.OgrenciNo).ToHashSet();
        sinav.TamamlananAdim = Math.Max(sinav.TamamlananAdim, 3);
        sinav.GuncellemeTarihi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new NotYuklemeSonucu(giren, girmeyen,
            satirlar.Keys.Where(no => !listeNumaralari.Contains(no)).Order().ToList(), tablo.Uyarilar);
    }

    private async Task<Sinav?> AcikUcluSinav(int id)
    {
        var s = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        return s is { Format: SinavFormati.AcikUclu } ? s : null;
    }

    private async Task<SinavBilgisi> Bilgi(Sinav s)
    {
        var mevcut = await db.SinavSonuclari.CountAsync(x => x.SinavId == s.Id);
        return new SinavBilgisi(s.Id, s.DersId, s.Tur.ToString(), s.Format.ToString(), s.Durum.ToString(), s.TamamlananAdim,
            s.SoruSayisi, s.MaksimumPuan, s.KitapcikSayisi, s.PuanlariDinamikDagit, s.BirdenFazlaDogruKabul,
            s.OptikDosyaAdi, s.SinifListesiDosyaAdi, mevcut, 0, []);
    }
}
