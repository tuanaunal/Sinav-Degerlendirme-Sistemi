using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Api.Controllers.Yonetim;

public record HarfAraligi(string Harf, decimal Katsayi, int AltSinir, int UstSinir, string Durum);

public record NotlandirmaBilgisi(
    string SemaAdi, int VizeAgirligi, int FinalAgirligi, int ButunlemeAgirligi, int FinalAltSiniri, List<HarfAraligi> Harfler);

public record NotlandirmaGuncelleme(int VizeAgirligi, int FinalAgirligi, int FinalAltSiniri, List<HarfAraligi> Harfler);

public record OptikAlanBilgisi(string Alan, int Baslangic, int? Bitis);

/// <summary>Notlandırma yönetmeliği ve optik form ayarları. Okuma tüm aktif kullanıcılara açıktır (sınav girişinde gerekir).</summary>
[ApiController]
[Route("api/ayarlar")]
[Authorize]
public class AyarlarController(SdsDbContext db) : ControllerBase
{
    [HttpGet("notlandirma")]
    public async Task<NotlandirmaBilgisi> Notlandirma()
    {
        var ayar = await db.NotlandirmaAyarlari.FirstAsync();
        var harfler = await db.HarfNotuAraliklari.OrderBy(h => h.Sira).ToListAsync();
        return new NotlandirmaBilgisi(
            string.Join(" – ", harfler.Select(h => h.Harf)),
            ayar.VizeAgirligi, ayar.FinalAgirligi, ayar.FinalAgirligi, ayar.FinalAltSiniri,
            harfler.Select(h => new HarfAraligi(h.Harf, h.Katsayi, h.AltSinir, h.UstSinir, h.Durum)).ToList());
    }

    /// <summary>
    /// Ağırlıkları, final alt sınırını ve harf aralıklarının puan sınırlarını günceller.
    /// Harfler, katsayılar ve durumlar yönetmelikle sabittir; yalnızca aralıklar değiştirilebilir.
    /// </summary>
    [HttpPut("notlandirma")]
    [Authorize(Policy = Yetki.Yonetici)]
    public async Task<IActionResult> NotlandirmaGuncelle(NotlandirmaGuncelleme istek)
    {
        if (istek.VizeAgirligi is < 0 or > 100 || istek.FinalAgirligi is < 0 or > 100)
            return BadRequest(new { mesaj = "Ağırlıklar 0 ile 100 arasında olmalıdır." });
        if (istek.VizeAgirligi + istek.FinalAgirligi != 100)
            return BadRequest(new { mesaj = $"Vize ve final ağırlıklarının toplamı %100 olmalıdır (şu an %{istek.VizeAgirligi + istek.FinalAgirligi})." });
        if (istek.FinalAltSiniri is < 0 or > 100)
            return BadRequest(new { mesaj = "Final alt sınırı 0 ile 100 arasında olmalıdır." });

        var harfler = await db.HarfNotuAraliklari.OrderBy(h => h.Sira).ToListAsync();
        if (HarfAraliklariniDogrula(harfler.Select(h => h.Harf).ToList(), istek.Harfler) is { } hata)
            return BadRequest(new { mesaj = hata });

        foreach (var h in harfler)
        {
            var yeni = istek.Harfler.Single(x => x.Harf == h.Harf);
            h.AltSinir = yeni.AltSinir;
            h.UstSinir = yeni.UstSinir;
        }

        var ayar = await db.NotlandirmaAyarlari.FirstAsync();
        ayar.VizeAgirligi = istek.VizeAgirligi;
        ayar.FinalAgirligi = istek.FinalAgirligi;
        ayar.FinalAltSiniri = istek.FinalAltSiniri;
        ayar.GuncellemeTarihi = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { mesaj = "Notlandırma yönetmeliği kaydedildi." });
    }

    /// <summary>Aralıklar 100'den 0'a boşluksuz ve çakışmasız inmelidir (AA: 88-100, BA: 80-87, ...).</summary>
    public static string? HarfAraliklariniDogrula(List<string> sira, List<HarfAraligi>? gelen)
    {
        if (gelen is null || gelen.Count != sira.Count || sira.Any(h => gelen.Count(g => g.Harf == h) != 1))
            return "Tüm harf notları (" + string.Join(", ", sira) + ") birer kez gönderilmelidir.";

        var beklenenUst = 100;
        foreach (var harf in sira)
        {
            var a = gelen.Single(g => g.Harf == harf);
            if (a.UstSinir != beklenenUst)
                return $"{harf} notunun üst sınırı {beklenenUst} olmalıdır; aralıklar arasında boşluk veya çakışma olamaz.";
            if (a.AltSinir > a.UstSinir)
                return $"{harf} notunun alt sınırı üst sınırından büyük olamaz.";
            beklenenUst = a.AltSinir - 1;
        }
        return beklenenUst == -1 ? null : $"{sira[^1]} notunun alt sınırı 0 olmalıdır.";
    }

    [HttpGet("optik-form")]
    public async Task<List<OptikAlanBilgisi>> OptikForm() =>
        (await db.OptikFormAlanlari.OrderBy(a => a.Baslangic).ToListAsync())
        .Select(a => new OptikAlanBilgisi(a.Alan.ToString(), a.Baslangic, a.Bitis)).ToList();

    [HttpPut("optik-form")]
    [Authorize(Policy = Yetki.Yonetici)]
    public async Task<IActionResult> OptikFormGuncelle(List<OptikAlanBilgisi> istek)
    {
        if (OptikAlanlariniDogrula(istek) is { } hata) return BadRequest(new { mesaj = hata });

        var alanlar = await db.OptikFormAlanlari.ToListAsync();
        foreach (var a in alanlar)
        {
            var yeni = istek.Single(x => x.Alan == a.Alan.ToString());
            a.Baslangic = yeni.Baslangic;
            a.Bitis = a.Alan == OptikAlan.Cevaplar ? null : yeni.Bitis;
        }
        await db.SaveChangesAsync();
        return Ok(new { mesaj = "Optik form ayarları kaydedildi." });
    }

    public static string? OptikAlanlariniDogrula(List<OptikAlanBilgisi>? alanlar)
    {
        var tumu = Enum.GetNames<OptikAlan>();
        if (alanlar is null || alanlar.Count != tumu.Length || tumu.Any(t => alanlar.Count(a => a.Alan == t) != 1))
            return "Tüm optik alanlar birer kez gönderilmelidir.";

        foreach (var a in alanlar)
        {
            if (a.Baslangic < 0) return "Başlangıç indisleri 0 veya daha büyük olmalıdır.";
            if (a.Alan != nameof(OptikAlan.Cevaplar) && (a.Bitis is null || a.Bitis < a.Baslangic))
                return "Her alanın bitiş indisi başlangıç indisine eşit veya ondan büyük olmalıdır.";
        }

        // Cevaplar satırın sonuna kadar uzanır; diğer alanlar ondan önce ve birbirinden ayrı olmalı
        var sabit = alanlar.Where(a => a.Alan != nameof(OptikAlan.Cevaplar)).OrderBy(a => a.Baslangic).ToList();
        for (var i = 1; i < sabit.Count; i++)
            if (sabit[i].Baslangic <= sabit[i - 1].Bitis)
                return "Optik alanlar birbiriyle çakışamaz.";
        var cevaplar = alanlar.Single(a => a.Alan == nameof(OptikAlan.Cevaplar));
        if (sabit.Any(a => a.Bitis >= cevaplar.Baslangic))
            return "Cevaplar alanı diğer tüm alanlardan sonra başlamalıdır.";

        return null;
    }
}
