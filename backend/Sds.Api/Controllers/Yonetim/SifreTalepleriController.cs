using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Api.Controllers.Yonetim;

public record SifreTalebiSatiri(
    int Id, string AdSoyad, string Eposta, DateTime TalepTarihi, string Durum, DateTime? IslemTarihi, string? IsleyenKullanici);

/// <summary>Şifresini unutan öğretim üyelerinin taleplerini onaylama / reddetme.</summary>
[ApiController]
[Route("api/yonetim/sifre-talepleri")]
[Authorize(Policy = Yetki.Yonetici)]
public class SifreTalepleriController(SdsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<SifreTalebiSatiri>>> Listele([FromQuery] TalepDurumu durum = TalepDurumu.Bekliyor)
    {
        var talepler = await db.SifreSifirlamaTalepleri
            .Where(t => t.Durum == durum)
            .Include(t => t.Kullanici)
            .Include(t => t.IsleyenKullanici)
            .OrderByDescending(t => t.TalepTarihi)
            .ToListAsync();

        return talepler.Select(t => new SifreTalebiSatiri(
            t.Id, t.Kullanici.TamAd, t.Kullanici.Eposta, t.TalepTarihi, t.Durum.ToString(), t.IslemTarihi,
            t.IsleyenKullanici?.TamAd)).ToList();
    }

    /// <summary>Bekleyen talep sayısı (menüde rozet olarak gösterilir).</summary>
    [HttpGet("bekleyen-sayisi")]
    public async Task<int> BekleyenSayisi() =>
        await db.SifreSifirlamaTalepleri.CountAsync(t => t.Durum == TalepDurumu.Bekliyor);

    /// <summary>
    /// Onaylanınca şifre kurala göre yeniden üretilir (e-postanın @ öncesi + personel no + *)
    /// ve kullanıcı ilk girişte yeni şifre belirlemek zorunda kalır.
    /// </summary>
    [HttpPost("{id:int}/onayla")]
    public async Task<IActionResult> Onayla(int id)
    {
        var talep = await db.SifreSifirlamaTalepleri.Include(t => t.Kullanici).FirstOrDefaultAsync(t => t.Id == id);
        if (talep is null) return NotFound(new { mesaj = "Talep bulunamadı." });
        if (talep.Durum != TalepDurumu.Bekliyor) return BadRequest(new { mesaj = "Bu talep daha önce işlenmiş." });

        var k = talep.Kullanici;
        k.SifreHash = BCrypt.Net.BCrypt.HashPassword(BaslangicVerisi.VarsayilanSifre(k.Eposta, k.PersonelNo));
        k.SifreDegistirmeli = true;
        Isle(talep, TalepDurumu.Onaylandi);
        await db.SaveChangesAsync();

        return Ok(new { mesaj = $"{k.TamAd} kullanıcısının şifresi varsayılan şifreye döndürüldü." });
    }

    [HttpPost("{id:int}/reddet")]
    public async Task<IActionResult> Reddet(int id)
    {
        var talep = await db.SifreSifirlamaTalepleri.FindAsync(id);
        if (talep is null) return NotFound(new { mesaj = "Talep bulunamadı." });
        if (talep.Durum != TalepDurumu.Bekliyor) return BadRequest(new { mesaj = "Bu talep daha önce işlenmiş." });

        Isle(talep, TalepDurumu.Reddedildi);
        await db.SaveChangesAsync();
        return Ok(new { mesaj = "Talep reddedildi." });
    }

    private void Isle(SifreSifirlamaTalebi talep, TalepDurumu durum)
    {
        talep.Durum = durum;
        talep.IslemTarihi = DateTime.UtcNow;
        talep.IsleyenKullaniciId = User.KullaniciId();
    }
}
