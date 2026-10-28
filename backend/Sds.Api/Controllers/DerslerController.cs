using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Api.Controllers;

public record SinavOzeti(int Id, string Tur, string Format, string Durum, int TamamlananAdim, DateTime GuncellemeTarihi);

public record DersKarti(
    int Id, string Kod, string Ad, string AkademikYil, string Donem, string OgretimTuru, int Sinif,
    List<string> Egitmenler, List<SinavOzeti> Sinavlar);

public record DersDetayi(
    int Id, string Kod, string Ad, string AkademikYil, string Donem, string OgretimTuru, int Sinif,
    List<string> Egitmenler, List<SinavOzeti> Sinavlar, bool DonemDegerlendirmeAcik);

public record YeniDers(string Kod, string Ad, string AkademikYil, Donem Donem, OgretimTuru OgretimTuru, int Sinif);

public record AkademikYillar(string Guncel, List<string> Yillar);

/// <summary>
/// Öğretim üyesinin ders panosu ve ders detayı. Bir öğretim üyesi yalnızca eğitmen olarak atandığı derslere erişir.
/// </summary>
[ApiController]
[Route("api/dersler")]
[Authorize(Policy = Yetki.OgretimUyesi)]
public class DerslerController(SdsDbContext db) : ControllerBase
{
    /// <summary>Panodaki akademik yıl seçimi: içinde bulunulan yıl + öğretim üyesinin dersi olan yıllar.</summary>
    [HttpGet("akademik-yillar")]
    public async Task<AkademikYillar> Yillar()
    {
        var guncel = AkademikTakvim.AkademikYil(DateTime.Now);
        var yillar = await db.DersEgitmenleri
            .Where(e => e.KullaniciId == User.KullaniciId())
            .Select(e => e.Ders.AkademikYil)
            .Distinct()
            .ToListAsync();
        if (!yillar.Contains(guncel)) yillar.Add(guncel);
        return new AkademikYillar(guncel, yillar.OrderDescending().ToList());
    }

    [HttpGet]
    public async Task<List<DersKarti>> Listele([FromQuery] string? akademikYil)
    {
        var yil = akademikYil ?? AkademikTakvim.AkademikYil(DateTime.Now);
        var dersler = await DerslerimSorgusu()
            .Where(d => d.AkademikYil == yil)
            .Include(d => d.Egitmenler).ThenInclude(e => e.Kullanici)
            .Include(d => d.Sinavlar)
            .OrderBy(d => d.Donem).ThenBy(d => d.Sinif).ThenBy(d => d.Ad)
            .ToListAsync();

        return dersler.Select(d => new DersKarti(
            d.Id, d.Kod, d.Ad, d.AkademikYil, d.Donem.ToString(), d.OgretimTuru.ToString(), d.Sinif,
            Egitmenler(d), Sinavlar(d))).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DersDetayi>> Detay(int id)
    {
        var d = await DerslerimSorgusu()
            .Include(x => x.Egitmenler).ThenInclude(e => e.Kullanici)
            .Include(x => x.Sinavlar)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound(new { mesaj = "Ders bulunamadı veya bu derse erişim yetkiniz yok." });

        return new DersDetayi(
            d.Id, d.Kod, d.Ad, d.AkademikYil, d.Donem.ToString(), d.OgretimTuru.ToString(), d.Sinif,
            Egitmenler(d), Sinavlar(d), DonemDegerlendirmeAcikMi(d.Sinavlar));
    }

    /// <summary>"Yeni Ders Ekle": dersi oluşturan öğretim üyesi otomatik olarak eğitmeni olur.</summary>
    [HttpPost]
    public async Task<ActionResult<DersKarti>> Ekle(YeniDers istek)
    {
        var kod = (istek.Kod ?? "").Trim().ToUpperInvariant();
        var ad = (istek.Ad ?? "").Trim();
        if (kod.Length is < 2 or > 20 || !kod.All(char.IsLetterOrDigit))
            return BadRequest(new { mesaj = "Ders kodu 2-20 karakter olmalı ve yalnızca harf/rakam içermelidir." });
        if (ad.Length is < 2 or > 150)
            return BadRequest(new { mesaj = "Ders adı 2-150 karakter olmalıdır." });
        if (!AkademikTakvim.GecerliMi(istek.AkademikYil))
            return BadRequest(new { mesaj = "Akademik yıl \"2026-2027\" biçiminde olmalıdır." });
        if (istek.Sinif is < 1 or > 6)
            return BadRequest(new { mesaj = "Sınıf 1 ile 6 arasında olmalıdır." });
        if (!Enum.IsDefined(istek.Donem) || !Enum.IsDefined(istek.OgretimTuru))
            return BadRequest(new { mesaj = "Dönem veya öğretim türü geçersiz." });

        var mevcut = await db.Dersler.Include(d => d.Egitmenler).FirstOrDefaultAsync(d =>
            d.Kod == kod && d.AkademikYil == istek.AkademikYil && d.Donem == istek.Donem && d.OgretimTuru == istek.OgretimTuru);
        if (mevcut is not null)
        {
            var benimMi = mevcut.Egitmenler.Any(e => e.KullaniciId == User.KullaniciId());
            return Conflict(new
            {
                mesaj = benimMi
                    ? "Bu ders bu dönem için zaten panonuzda tanımlı."
                    : "Bu ders bu dönem için başka bir öğretim üyesi tarafından tanımlanmış. Eğitmen olarak eklenmek için sistem yöneticisine başvurun."
            });
        }

        var ders = new Ders
        {
            Kod = kod, Ad = ad, AkademikYil = istek.AkademikYil, Donem = istek.Donem,
            OgretimTuru = istek.OgretimTuru, Sinif = istek.Sinif,
        };
        ders.Egitmenler.Add(new DersEgitmen { KullaniciId = User.KullaniciId() });
        db.Dersler.Add(ders);
        await db.SaveChangesAsync();

        await db.Entry(ders).Collection(d => d.Egitmenler).Query().Include(e => e.Kullanici).LoadAsync();
        return new DersKarti(ders.Id, ders.Kod, ders.Ad, ders.AkademikYil, ders.Donem.ToString(),
            ders.OgretimTuru.ToString(), ders.Sinif, Egitmenler(ders), []);
    }

    /// <summary>Dersi tüm sınavları, sonuçları ve öğrenme çıktılarıyla birlikte siler (yalnızca dersin eğitmeni).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(int id)
    {
        var ders = await DerslerimSorgusu().FirstOrDefaultAsync(d => d.Id == id);
        if (ders is null) return NotFound(new { mesaj = "Ders bulunamadı." });

        var sinavIdleri = await db.Sinavlar.Where(s => s.DersId == id).Select(s => s.Id).ToListAsync();
        db.SinavSonuclari.RemoveRange(await db.SinavSonuclari.Where(s => sinavIdleri.Contains(s.SinavId))
            .Include(s => s.SoruCevaplari).Include(s => s.Eklemeler).ToListAsync());
        db.OptikKayitlari.RemoveRange(await db.OptikKayitlari.Where(s => sinavIdleri.Contains(s.SinavId)).ToListAsync());
        db.KitapcikSorulari.RemoveRange(await db.KitapcikSorulari.Where(s => sinavIdleri.Contains(s.SinavId)).ToListAsync());
        db.SinavSorulari.RemoveRange(await db.SinavSorulari.Where(s => sinavIdleri.Contains(s.SinavId)).ToListAsync());
        db.SoruOgrenmeCiktilari.RemoveRange(await db.SoruOgrenmeCiktilari.Where(s => sinavIdleri.Contains(s.SinavId)).ToListAsync());
        db.Sinavlar.RemoveRange(await db.Sinavlar.Where(s => s.DersId == id).ToListAsync());
        db.OgrenmeCiktilari.RemoveRange(await db.OgrenmeCiktilari.Where(c => c.DersId == id).ToListAsync());
        db.DersEgitmenleri.RemoveRange(await db.DersEgitmenleri.Where(e => e.DersId == id).ToListAsync());
        db.Dersler.Remove(ders);
        await db.SaveChangesAsync();
        return Ok(new { mesaj = "Ders silindi." });
    }

    /// <summary>Dönem değerlendirme en az iki sınav (vize + final veya vize + bütünleme) tamamlanınca açılır.</summary>
    public static bool DonemDegerlendirmeAcikMi(IEnumerable<Sinav> sinavlar)
    {
        var tamam = sinavlar.Where(s => s.Durum == SinavDurumu.Tamamlandi).Select(s => s.Tur).ToHashSet();
        return tamam.Contains(SinavTuru.Vize) && (tamam.Contains(SinavTuru.Final) || tamam.Contains(SinavTuru.Butunleme));
    }

    private IQueryable<Ders> DerslerimSorgusu()
    {
        var kullaniciId = User.KullaniciId();
        return db.Dersler.Where(d => d.Egitmenler.Any(e => e.KullaniciId == kullaniciId));
    }

    private static List<string> Egitmenler(Ders d) => d.Egitmenler.Select(e => e.Kullanici.TamAd).OrderBy(x => x).ToList();

    private static List<SinavOzeti> Sinavlar(Ders d) => d.Sinavlar
        .OrderBy(s => s.Tur)
        .Select(s => new SinavOzeti(s.Id, s.Tur.ToString(), s.Format.ToString(), s.Durum.ToString(), s.TamamlananAdim, s.GuncellemeTarihi))
        .ToList();
}
