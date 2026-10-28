using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Degerlendirme;

namespace Sds.Api.Controllers;

public record DonemGenelBilgi(
    string DersKodu, string DersAdi, string AkademikYil, string Donem, string OgretimTuru, int Sinif, List<string> Egitmenler,
    int VizeAgirligi, int FinalAgirligi, int FinalAltSiniri, int GecmeSiniri);

public record DonemOzeti(
    int Toplam, decimal? AgirlikliOrtalama, decimal? Medyan, decimal? StandartSapma, decimal? EnDusuk, decimal? EnYuksek,
    decimal? BasariOrani, int Basarili, int SartliGecen, int Basarisiz, decimal? OrtalamaKatsayi);

public record DonemSinavIstatistigi(
    string Sinav, string Format, DateOnly? Tarih, int SoruSayisi, int Toplam, int Giren,
    decimal? Ortalama, decimal? Medyan, decimal? StandartSapma, decimal? EnDusuk, decimal? EnYuksek, decimal? BasariOrani);

/// <summary>Bir öğrencinin dönem sonucu. Etkin sınav: bütünlemeye girdiyse bütünleme, girmediyse final.</summary>
public record DonemOgrencisi(
    string OgrenciNo, string AdSoyad, decimal? Vize, decimal? Final, decimal? Butunleme, bool ButunlemeListesinde,
    string? EtkinSinav, decimal Agirlikli, string Harf, string Durum, string? Aciklama);

public record CiktiSinavBasarisi(string Sinav, int SoruSayisi, decimal? Basari);
public record DonemCiktisi(int SiraNo, string Aciklama, List<CiktiSinavBasarisi> Sinavlar, decimal? DonemBasarisi);

public record DonemAnaliziYaniti(
    DonemGenelBilgi Genel, DonemOzeti Ozet, List<DonemSinavIstatistigi> Sinavlar, List<HarfSayisi> HarfDagilimi,
    List<DagilimAraligi> PuanDagilimi, List<DonemOgrencisi> Ogrenciler, List<DonemCiktisi> Ciktilar);

/// <summary>Dönem Analiz Raporu: ağırlıklı ortalama, harf notları ve 4 sayfalık Excel çıktısı.</summary>
[ApiController]
[Route("api/dersler/{id:int}/donem")]
[Authorize(Policy = Yetki.OgretimUyesi)]
public class DonemController(SdsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DonemAnaliziYaniti>> Getir(int id)
    {
        var (ders, hata) = await DersAsync(id);
        if (hata is not null) return hata;
        try { return await DonemAnalizServisi.HesaplaAsync(db, ders!); }
        catch (InvalidOperationException e) when (e.Message == DonemAnalizServisi.AgirlikHatasi) { return BadRequest(new { mesaj = e.Message }); }
    }

    [HttpGet("excel")]
    public async Task<IActionResult> Excel(int id)
    {
        var (ders, hata) = await DersAsync(id);
        if (hata is not null) return hata;
        try
        {
            var analiz = await DonemAnalizServisi.HesaplaAsync(db, ders!);
            var (icerik, ad) = DonemRaporUretici.Uret(analiz);
            return File(icerik, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad);
        }
        catch (InvalidOperationException e) when (e.Message == DonemAnalizServisi.AgirlikHatasi) { return BadRequest(new { mesaj = e.Message }); }
    }

    private async Task<(Domain.Ders?, ActionResult?)> DersAsync(int id)
    {
        var kullaniciId = User.KullaniciId();
        var ders = await db.Dersler.AsNoTracking().Include(d => d.Sinavlar)
            .FirstOrDefaultAsync(d => d.Id == id && d.Egitmenler.Any(e => e.KullaniciId == kullaniciId));
        if (ders is null) return (null, NotFound(new { mesaj = "Ders bulunamadı." }));
        if (!DerslerController.DonemDegerlendirmeAcikMi(ders.Sinavlar))
            return (null, BadRequest(new { mesaj = "Dönem değerlendirmesi için vize ile final veya bütünleme sınav girişlerinin tamamlanması gerekir." }));
        return (ders, null);
    }
}
