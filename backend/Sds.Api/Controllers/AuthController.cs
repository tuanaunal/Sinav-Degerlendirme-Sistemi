using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Api.Controllers;

public record GirisIstegi(string Eposta, string Sifre);
public record SifreDegistirIstegi(string MevcutSifre, string YeniSifre, string YeniSifreTekrar);
public record SifremiUnuttumIstegi(string Eposta);

public record KullaniciBilgisi(
    int Id, string TamAd, string Unvan, string Ad, string Soyad, string Eposta,
    string Rol, bool YoneticiYetkisi, bool SifreDegistirmeli);

public record OturumYaniti(string Token, DateTime GecerlilikSonu, KullaniciBilgisi Kullanici);

[ApiController]
[Route("api/auth")]
public class AuthController(SdsDbContext db, TokenServisi tokenServisi) : ControllerBase
{
    /// <summary>E-posta ve şifre ile giriş. Rolüne göre arayüz farklı sayfaya yönlendirir.</summary>
    [HttpPost("giris")]
    [AllowAnonymous]
    public async Task<ActionResult<OturumYaniti>> Giris(GirisIstegi istek)
    {
        var eposta = (istek.Eposta ?? "").Trim().ToLowerInvariant();
        var k = await db.Kullanicilar.FirstOrDefaultAsync(x => x.Eposta == eposta);

        if (k is null || !BCrypt.Net.BCrypt.Verify(istek.Sifre ?? "", k.SifreHash))
            return Unauthorized(new { mesaj = "E-posta adresi veya şifre hatalı." });
        if (!k.Aktif)
            return StatusCode(403, new { mesaj = "Hesabınız pasif durumdadır. Lütfen sistem yöneticisiyle görüşün." });

        return Oturum(k);
    }

    /// <summary>Oturumdaki kullanıcının güncel bilgileri.</summary>
    [HttpGet("ben")]
    [Authorize(Policy = Yetki.GirisYapmis)]
    public async Task<ActionResult<KullaniciBilgisi>> Ben()
    {
        var k = await db.Kullanicilar.FindAsync(User.KullaniciId());
        return k is null ? Unauthorized() : Bilgi(k);
    }

    /// <summary>
    /// Şifre değiştirme. İlk girişte veya şifre sıfırlandıktan sonra zorunludur;
    /// başarılı olunca yeni token döner ve kullanıcı sisteme devam eder.
    /// </summary>
    [HttpPost("sifre-degistir")]
    [Authorize(Policy = Yetki.GirisYapmis)]
    public async Task<ActionResult<OturumYaniti>> SifreDegistir(SifreDegistirIstegi istek)
    {
        var k = await db.Kullanicilar.FindAsync(User.KullaniciId());
        if (k is null) return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(istek.MevcutSifre ?? "", k.SifreHash))
            return BadRequest(new { mesaj = "Mevcut şifrenizi yanlış girdiniz." });
        if (istek.YeniSifre != istek.YeniSifreTekrar)
            return BadRequest(new { mesaj = "Yeni şifreler birbiriyle uyuşmuyor." });
        if (SifreKurali.Dogrula(istek.YeniSifre) is { } hata)
            return BadRequest(new { mesaj = hata });
        if (BCrypt.Net.BCrypt.Verify(istek.YeniSifre, k.SifreHash))
            return BadRequest(new { mesaj = "Yeni şifre mevcut şifrenizle aynı olamaz." });

        k.SifreHash = BCrypt.Net.BCrypt.HashPassword(istek.YeniSifre);
        k.SifreDegistirmeli = false;
        await db.SaveChangesAsync();

        return Oturum(k);
    }

    /// <summary>
    /// Şifre sıfırlama talebi oluşturur; yönetici onaylayınca şifre varsayılan kurala döner.
    /// Hesabın var olup olmadığını belli etmemek için her durumda aynı mesaj döner.
    /// </summary>
    [HttpPost("sifremi-unuttum")]
    [AllowAnonymous]
    public async Task<IActionResult> SifremiUnuttum(SifremiUnuttumIstegi istek)
    {
        var eposta = (istek.Eposta ?? "").Trim().ToLowerInvariant();
        var k = await db.Kullanicilar.FirstOrDefaultAsync(x => x.Eposta == eposta && x.Aktif);

        if (k is not null && !await db.SifreSifirlamaTalepleri
                .AnyAsync(t => t.KullaniciId == k.Id && t.Durum == TalepDurumu.Bekliyor))
        {
            db.SifreSifirlamaTalepleri.Add(new SifreSifirlamaTalebi { KullaniciId = k.Id });
            await db.SaveChangesAsync();
        }

        return Ok(new
        {
            mesaj = "Talebiniz alındı. Sistem yöneticisi onayladığında şifreniz varsayılan şifreye dönecek " +
                    "ve ilk girişte yeni şifre belirlemeniz istenecektir."
        });
    }

    private OturumYaniti Oturum(Kullanici k)
    {
        var (token, bitis) = tokenServisi.Olustur(k);
        return new OturumYaniti(token, bitis, Bilgi(k));
    }

    private static KullaniciBilgisi Bilgi(Kullanici k) => new(
        k.Id, k.TamAd, k.Unvan, k.Ad, k.Soyad, k.Eposta, k.Rol.ToString(), k.YoneticiYetkisi, k.SifreDegistirmeli);
}
