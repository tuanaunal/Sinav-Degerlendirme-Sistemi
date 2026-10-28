using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Api.Controllers.Yonetim;

public record DersOzeti(int Id, string Kod, string Ad, string AkademikYil, string Donem);

public record KullaniciSatiri(
    int Id, string Unvan, string Ad, string Soyad, string TamAd, string Eposta, string PersonelNo,
    string Rol, bool YoneticiYetkisi, bool Aktif, bool SifreDegistirmeli, List<DersOzeti> Dersler);

public record KullaniciKaydi(string Unvan, string Ad, string Soyad, string Eposta, string PersonelNo, bool YoneticiYetkisi);
public record KullaniciGuncelleme(string Unvan, string Ad, string Soyad, string PersonelNo, bool YoneticiYetkisi, bool Aktif);

public record DersSatiri(
    int Id, string Kod, string Ad, string AkademikYil, string Donem, string OgretimTuru, int Sinif,
    List<EgitmenOzeti> Egitmenler);
public record EgitmenOzeti(int Id, string TamAd);
public record EgitmenAtama(List<int> KullaniciIdleri);

/// <summary>Öğretim üyesi ekleme, yetki verme ve derslere eğitmen atama.</summary>
[ApiController]
[Route("api/yonetim")]
[Authorize(Policy = Yetki.Yonetici)]
public class KullanicilarController(SdsDbContext db) : ControllerBase
{
    /// <summary>Yönetici yetkisi verme/alma yalnızca Admin rolündedir.</summary>
    private bool AdminMi => User.IsInRole(nameof(KullaniciRolu.Admin));

    [HttpGet("kullanicilar")]
    public async Task<List<KullaniciSatiri>> Listele()
    {
        var kullanicilar = await db.Kullanicilar
            .Include(k => k.Dersleri).ThenInclude(de => de.Ders)
            .OrderBy(k => k.Rol).ThenBy(k => k.Ad).ThenBy(k => k.Soyad)
            .ToListAsync();
        return kullanicilar.Select(Satir).ToList();
    }

    /// <summary>
    /// Yeni öğretim üyesi. Varsayılan şifre kurala göre üretilir ve ilk girişte değiştirilmesi zorunludur.
    /// </summary>
    [HttpPost("kullanicilar")]
    public async Task<ActionResult<KullaniciSatiri>> Ekle(KullaniciKaydi istek)
    {
        var eposta = (istek.Eposta ?? "").Trim().ToLowerInvariant();
        if (Dogrula(istek.Ad, istek.Soyad, istek.PersonelNo) is { } hata) return BadRequest(new { mesaj = hata });
        if (!MailAddress.TryCreate(eposta, out var adres) || adres.Address != eposta)
            return BadRequest(new { mesaj = "Geçerli bir e-posta adresi girin." });
        if (await db.Kullanicilar.AnyAsync(k => k.Eposta == eposta))
            return Conflict(new { mesaj = "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var." });
        if (istek.YoneticiYetkisi && !AdminMi)
            return StatusCode(403, new { mesaj = "Yönetici yetkisini yalnızca sistem yöneticisi (Admin) verebilir." });

        var k = new Kullanici
        {
            Unvan = (istek.Unvan ?? "").Trim(),
            Ad = istek.Ad.Trim(),
            Soyad = istek.Soyad.Trim(),
            Eposta = eposta,
            PersonelNo = istek.PersonelNo.Trim(),
            Rol = KullaniciRolu.OgretimUyesi,
            YoneticiYetkisi = istek.YoneticiYetkisi,
            SifreDegistirmeli = true,
        };
        k.SifreHash = BCrypt.Net.BCrypt.HashPassword(BaslangicVerisi.VarsayilanSifre(k.Eposta, k.PersonelNo));
        db.Kullanicilar.Add(k);
        await db.SaveChangesAsync();

        return Satir(k);
    }

    [HttpPut("kullanicilar/{id:int}")]
    public async Task<ActionResult<KullaniciSatiri>> Guncelle(int id, KullaniciGuncelleme istek)
    {
        var k = await db.Kullanicilar.Include(x => x.Dersleri).ThenInclude(de => de.Ders).FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound(new { mesaj = "Kullanıcı bulunamadı." });
        if (k.Rol == KullaniciRolu.Admin)
            return BadRequest(new { mesaj = "Sistem yöneticisi hesabı bu ekrandan düzenlenemez." });
        if (Dogrula(istek.Ad, istek.Soyad, istek.PersonelNo) is { } hata) return BadRequest(new { mesaj = hata });
        if (istek.YoneticiYetkisi != k.YoneticiYetkisi && !AdminMi)
            return StatusCode(403, new { mesaj = "Yönetici yetkisini yalnızca sistem yöneticisi (Admin) verebilir veya alabilir." });
        if (k.Id == User.KullaniciId() && !istek.Aktif)
            return BadRequest(new { mesaj = "Kendi hesabınızı pasif yapamazsınız." });

        k.Unvan = (istek.Unvan ?? "").Trim();
        k.Ad = istek.Ad.Trim();
        k.Soyad = istek.Soyad.Trim();
        k.PersonelNo = istek.PersonelNo.Trim();
        k.YoneticiYetkisi = istek.YoneticiYetkisi;
        k.Aktif = istek.Aktif;
        await db.SaveChangesAsync();

        return Satir(k);
    }

    /// <summary>Tüm dersler ve eğitmenleri (eğitmen atama tablosu için).</summary>
    [HttpGet("dersler")]
    public async Task<List<DersSatiri>> Dersler()
    {
        var dersler = await db.Dersler
            .Include(d => d.Egitmenler).ThenInclude(e => e.Kullanici)
            .OrderByDescending(d => d.AkademikYil).ThenBy(d => d.Donem).ThenBy(d => d.Sinif).ThenBy(d => d.Kod)
            .ToListAsync();

        return dersler.Select(d => new DersSatiri(
            d.Id, d.Kod, d.Ad, d.AkademikYil, d.Donem.ToString(), d.OgretimTuru.ToString(), d.Sinif,
            d.Egitmenler.Select(e => new EgitmenOzeti(e.KullaniciId, e.Kullanici.TamAd)).OrderBy(e => e.TamAd).ToList()
        )).ToList();
    }

    /// <summary>Dersin eğitmen listesini verilen öğretim üyeleriyle değiştirir.</summary>
    [HttpPut("dersler/{id:int}/egitmenler")]
    public async Task<IActionResult> EgitmenAta(int id, EgitmenAtama istek)
    {
        var ders = await db.Dersler.Include(d => d.Egitmenler).FirstOrDefaultAsync(d => d.Id == id);
        if (ders is null) return NotFound(new { mesaj = "Ders bulunamadı." });

        var idler = (istek.KullaniciIdleri ?? []).Distinct().ToList();
        if (idler.Count == 0) return BadRequest(new { mesaj = "Derse en az bir eğitmen atanmalıdır." });

        var gecerli = await db.Kullanicilar
            .Where(k => idler.Contains(k.Id) && k.Rol == KullaniciRolu.OgretimUyesi)
            .Select(k => k.Id).ToListAsync();
        if (gecerli.Count != idler.Count)
            return BadRequest(new { mesaj = "Derslere yalnızca öğretim üyeleri eğitmen olarak atanabilir." });

        ders.Egitmenler.RemoveAll(e => !idler.Contains(e.KullaniciId));
        foreach (var kid in idler.Where(kid => ders.Egitmenler.All(e => e.KullaniciId != kid)))
            ders.Egitmenler.Add(new DersEgitmen { DersId = ders.Id, KullaniciId = kid });
        await db.SaveChangesAsync();

        return Ok(new { mesaj = "Eğitmen ataması kaydedildi." });
    }

    private static string? Dogrula(string ad, string soyad, string personelNo)
    {
        if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(soyad)) return "Ad ve soyad zorunludur.";
        if (string.IsNullOrWhiteSpace(personelNo) || !personelNo.Trim().All(char.IsDigit))
            return "Personel numarası yalnızca rakamlardan oluşmalıdır.";
        return null;
    }

    private static KullaniciSatiri Satir(Kullanici k) => new(
        k.Id, k.Unvan, k.Ad, k.Soyad, k.TamAd, k.Eposta, k.PersonelNo, k.Rol.ToString(), k.YoneticiYetkisi, k.Aktif,
        k.SifreDegistirmeli,
        k.Dersleri.Select(de => new DersOzeti(de.Ders.Id, de.Ders.Kod, de.Ders.Ad, de.Ders.AkademikYil, de.Ders.Donem.ToString()))
            .OrderBy(d => d.Kod).ToList());
}
