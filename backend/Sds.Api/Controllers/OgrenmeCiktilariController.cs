using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Degerlendirme;
using Sds.Api.Domain;

namespace Sds.Api.Controllers;

public record KitapcikSiralamasi(char Kitapcik, List<int?> AnaSoruNolari);
public record KitapcikEslestirmeBilgisi(int SoruSayisi, bool Gerekli, bool Kaydedildi, List<KitapcikSiralamasi> Kitapciklar);
public record KitapcikEslestirmeKaydi(List<KitapcikSiralamasi> Kitapciklar);

public record CiktiBilgisi(int Id, int SiraNo, string Aciklama, List<int> Sorular);
public record CiktiDurumu(
    int SoruSayisi, bool KitapcikEslestirmeGerekli, bool KitapcikEslestirmeKaydedildi,
    List<CiktiBilgisi> Ciktilar, int GetirilebilecekSayisi, string? GetirilecekDonem);
public record YeniCikti(string Aciklama);
public record CiktiSorulari(int CiktiId, List<int> Sorular);
public record CiktiEslestirmeKaydi(List<CiktiSorulari> Eslestirmeler, bool Tamamla);

/// <summary>
/// 5. adım: B, C… kitapçıklarının sorularının A kitapçığındaki karşılıkları ve
/// dersin öğrenme çıktılarının hangi sorularla ölçüldüğü. Tamamlanınca sınav analize hazır olur.
/// </summary>
[ApiController]
[Route("api/sinavlar/{id:int}")]
[Authorize(Policy = Yetki.OgretimUyesi)]
public class OgrenmeCiktilariController(SdsDbContext db) : ControllerBase
{
    // ------------------------------------------------------------------ Kitapçık soru eşleştirme

    [HttpGet("kitapcik-eslestirme")]
    public async Task<ActionResult<KitapcikEslestirmeBilgisi>> KitapcikGetir(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });

        var sorular = await db.KitapcikSorulari.Where(k => k.SinavId == id && k.Kitapcik != 'A').ToListAsync();
        var kitapciklar = Enumerable.Range(1, sinav.KitapcikSayisi - 1).Select(i => (char)('A' + i))
            .Select(h => new KitapcikSiralamasi(h, Enumerable.Range(1, sinav.SoruSayisi)
                .Select(no => sorular.FirstOrDefault(k => k.Kitapcik == h && k.SoruNo == no)?.AnaSoruNo).ToList()))
            .ToList();
        return new KitapcikEslestirmeBilgisi(sinav.SoruSayisi, sinav.KitapcikSayisi > 1, sinav.KitapcikEslestirmeKaydedildi, kitapciklar);
    }

    /// <summary>
    /// Her kitapçık için: kitapçıktaki i. sorunun A kitapçığındaki numarası. Her A sorusu tam bir kez kullanılmalı ve
    /// iki kitapçıktaki doğru cevaplar aynı olmalıdır (farklıysa eşleştirme yanlış girilmiştir).
    /// </summary>
    [HttpPut("kitapcik-eslestirme")]
    public async Task<ActionResult<KitapcikEslestirmeBilgisi>> KitapcikKaydet(int id, KitapcikEslestirmeKaydi istek)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (sinav.KitapcikSayisi < 2) return BadRequest(new { mesaj = "Bu sınavda tek kitapçık var; eşleştirme gerekmez." });

        var anahtar = await db.KitapcikSorulari.Where(k => k.SinavId == id).ToListAsync();
        var aAnahtari = anahtar.Where(k => k.Kitapcik == 'A').ToDictionary(k => k.SoruNo, k => k.DogruCevap);
        if (KitapcikEslestirmesiniDogrula(sinav.SoruSayisi, sinav.KitapcikSayisi, aAnahtari,
                anahtar.Where(k => k.Kitapcik != 'A').ToDictionary(k => (k.Kitapcik, k.SoruNo), k => k.DogruCevap), istek.Kitapciklar) is { } hata)
            return BadRequest(new { mesaj = hata });

        foreach (var kit in istek.Kitapciklar)
            for (var i = 0; i < sinav.SoruSayisi; i++)
                anahtar.Single(k => k.Kitapcik == kit.Kitapcik && k.SoruNo == i + 1).AnaSoruNo = kit.AnaSoruNolari[i];

        sinav.KitapcikEslestirmeKaydedildi = true;
        await db.SaveChangesAsync();

        // Soru bazlı cevaplar A numarasına göre yeniden yazılır (analizler bu sırayı kullanır)
        if (sinav.TamamlananAdim >= 3)
        {
            await SinavPuanlama.YenidenHesaplaAsync(db, sinav);
            await db.SaveChangesAsync();
        }
        return await KitapcikGetir(id);
    }

    public static string? KitapcikEslestirmesiniDogrula(
        int n, int kitapcikSayisi, IReadOnlyDictionary<int, string> aAnahtari,
        IReadOnlyDictionary<(char, int), string> digerAnahtar, List<KitapcikSiralamasi>? kitapciklar)
    {
        var beklenen = Enumerable.Range(1, kitapcikSayisi - 1).Select(i => (char)('A' + i)).ToList();
        if (kitapciklar is null || !kitapciklar.Select(k => k.Kitapcik).Order().SequenceEqual(beklenen))
            return $"{string.Join(", ", beklenen)} kitapçıklarının her biri için eşleştirme gönderilmelidir.";

        foreach (var kit in kitapciklar.OrderBy(k => k.Kitapcik))
        {
            if (kit.AnaSoruNolari is null || kit.AnaSoruNolari.Count != n || kit.AnaSoruNolari.Any(x => x is null))
                return $"{kit.Kitapcik} kitapçığının tüm soruları için A kitapçığındaki karşılığı seçilmelidir.";
            var tekrar = kit.AnaSoruNolari.GroupBy(x => x!.Value).FirstOrDefault(g => g.Count() > 1);
            if (tekrar is not null)
                return $"A kitapçığının {tekrar.Key}. sorusu {kit.Kitapcik} kitapçığında birden fazla soruyla eşleştirilmiş.";
            if (kit.AnaSoruNolari.Any(x => x < 1 || x > n)) return "Geçersiz soru numarası.";

            var uyusmayan = Enumerable.Range(1, n)
                .Where(no => digerAnahtar.GetValueOrDefault((kit.Kitapcik, no)) != aAnahtari.GetValueOrDefault(kit.AnaSoruNolari[no - 1]!.Value))
                .Take(3)
                .Select(no => $"{kit.Kitapcik}{no} ({digerAnahtar.GetValueOrDefault((kit.Kitapcik, no))}) ↔ A{kit.AnaSoruNolari[no - 1]} ({aAnahtari.GetValueOrDefault(kit.AnaSoruNolari[no - 1]!.Value)})")
                .ToList();
            if (uyusmayan.Count > 0)
                return $"Eşleştirilen soruların doğru cevapları uyuşmuyor: {string.Join(", ", uyusmayan)}. " +
                       "Aynı sorunun iki kitapçıktaki cevabı aynı olmalıdır; eşleştirmeyi kontrol edin.";
        }
        return null;
    }

    // ------------------------------------------------------------------ Öğrenme çıktıları

    [HttpGet("ogrenme-ciktilari")]
    public async Task<ActionResult<CiktiDurumu>> CiktiGetir(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        return await Durum(sinav);
    }

    [HttpPost("ogrenme-ciktilari")]
    public async Task<ActionResult<CiktiDurumu>> CiktiEkle(int id, YeniCikti istek)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var aciklama = (istek.Aciklama ?? "").Trim();
        if (aciklama.Length is < 5 or > 500) return BadRequest(new { mesaj = "Öğrenme çıktısı açıklaması 5-500 karakter olmalıdır." });

        var mevcut = await db.OgrenmeCiktilari.Where(c => c.DersId == sinav.DersId).ToListAsync();
        if (mevcut.Any(c => string.Equals(c.Aciklama, aciklama, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { mesaj = "Bu öğrenme çıktısı zaten ekli." });

        db.OgrenmeCiktilari.Add(new OgrenmeCiktisi { DersId = sinav.DersId, Aciklama = aciklama, SiraNo = mevcut.Count == 0 ? 1 : mevcut.Max(c => c.SiraNo) + 1 });
        await db.SaveChangesAsync();
        return await Durum(sinav);
    }

    /// <summary>Çıktı dersin tüm sınavlarında ortaktır; başka bir sınavda sorulara bağlıysa silinemez.</summary>
    [HttpDelete("ogrenme-ciktilari/{ciktiId:int}")]
    public async Task<ActionResult<CiktiDurumu>> CiktiSil(int id, int ciktiId)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        var cikti = await db.OgrenmeCiktilari.FirstOrDefaultAsync(c => c.Id == ciktiId && c.DersId == sinav.DersId);
        if (cikti is null) return NotFound(new { mesaj = "Öğrenme çıktısı bulunamadı." });
        if (await db.SoruOgrenmeCiktilari.AnyAsync(s => s.OgrenmeCiktisiId == ciktiId && s.SinavId != id))
            return Conflict(new { mesaj = "Bu çıktı dersin başka bir sınavında kullanılıyor; silinemez." });

        db.SoruOgrenmeCiktilari.RemoveRange(await db.SoruOgrenmeCiktilari.Where(s => s.OgrenmeCiktisiId == ciktiId).ToListAsync());
        db.OgrenmeCiktilari.Remove(cikti);
        await db.SaveChangesAsync();
        return await Durum(sinav);
    }

    /// <summary>"Öğrenme Çıktılarını Getir": aynı ders kodunun en son tanımlı döneminden çıktıları kopyalar.</summary>
    [HttpPost("ogrenme-ciktilari/getir")]
    public async Task<ActionResult<CiktiDurumu>> CiktiGetirKopyala(int id)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });

        var (kaynak, _) = await GetirilecekCiktilar(sinav.DersId);
        if (kaynak.Count == 0) return BadRequest(new { mesaj = "Bu ders kodu için daha önceki dönemlerde tanımlı öğrenme çıktısı bulunamadı." });

        var mevcut = await db.OgrenmeCiktilari.Where(c => c.DersId == sinav.DersId).ToListAsync();
        var sira = mevcut.Count == 0 ? 0 : mevcut.Max(c => c.SiraNo);
        foreach (var c in kaynak.Where(k => mevcut.All(m => !string.Equals(m.Aciklama, k.Aciklama, StringComparison.OrdinalIgnoreCase))))
            db.OgrenmeCiktilari.Add(new OgrenmeCiktisi { DersId = sinav.DersId, Aciklama = c.Aciklama, SiraNo = ++sira });
        await db.SaveChangesAsync();
        return await Durum(sinav);
    }

    /// <summary>
    /// Çıktı - soru matrisini kaydeder. Tamamla=true ise sınav tamamlanır ("Kaydet ve Analizi Gör"):
    /// en az bir çıktı en az bir soruyla ilişkilendirilmiş ve kitapçık eşleştirmesi kaydedilmiş olmalıdır.
    /// </summary>
    [HttpPut("ogrenme-ciktilari/eslestirme")]
    public async Task<ActionResult<CiktiDurumu>> CiktiEslestir(int id, CiktiEslestirmeKaydi istek)
    {
        var sinav = await db.ErisilebilirSinavAsync(id, User.KullaniciId());
        if (sinav is null) return NotFound(new { mesaj = "Sınav bulunamadı." });
        if (sinav.TamamlananAdim < 4) return BadRequest(new { mesaj = "Önce not düzenleme adımını tamamlayın." });
        if (sinav.KitapcikSayisi > 1 && !sinav.KitapcikEslestirmeKaydedildi)
            return BadRequest(new { mesaj = "Kazanım girişi için önce kitapçık soru eşleştirmesini tamamlayıp kaydedin." });

        var ciktilar = await db.OgrenmeCiktilari.Where(c => c.DersId == sinav.DersId).Select(c => c.Id).ToListAsync();
        var eslestirmeler = istek.Eslestirmeler ?? [];
        if (eslestirmeler.Any(e => !ciktilar.Contains(e.CiktiId))) return BadRequest(new { mesaj = "Geçersiz öğrenme çıktısı." });
        if (eslestirmeler.SelectMany(e => e.Sorular ?? []).Any(s => s < 1 || s > sinav.SoruSayisi)) return BadRequest(new { mesaj = "Geçersiz soru numarası." });

        db.SoruOgrenmeCiktilari.RemoveRange(await db.SoruOgrenmeCiktilari.Where(s => s.SinavId == id).ToListAsync());
        foreach (var e in eslestirmeler)
            foreach (var no in (e.Sorular ?? []).Distinct())
                db.SoruOgrenmeCiktilari.Add(new SoruOgrenmeCiktisi { SinavId = id, OgrenmeCiktisiId = e.CiktiId, SoruNo = no });

        if (istek.Tamamla)
        {
            if (!eslestirmeler.Any(e => e.Sorular?.Count > 0))
                return BadRequest(new { mesaj = "Analiz için en az bir öğrenme çıktısı ekleyip sorularla ilişkilendirmelisiniz." });
            sinav.TamamlananAdim = 5;
            sinav.Durum = SinavDurumu.Tamamlandi;
        }
        sinav.GuncellemeTarihi = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return await Durum(sinav);
    }

    private async Task<(List<OgrenmeCiktisi> Ciktilar, string? Donem)> GetirilecekCiktilar(int dersId)
    {
        var ders = await db.Dersler.FindAsync(dersId);
        var kaynakDers = await db.Dersler
            .Where(d => d.Kod == ders!.Kod && d.Id != dersId && d.OgrenmeCiktilari.Any())
            .OrderByDescending(d => d.AkademikYil).ThenByDescending(d => d.Donem)
            .Include(d => d.OgrenmeCiktilari)
            .FirstOrDefaultAsync();
        return kaynakDers is null
            ? ([], null)
            : (kaynakDers.OgrenmeCiktilari.OrderBy(c => c.SiraNo).ToList(), $"{kaynakDers.AkademikYil} {(kaynakDers.Donem == Donem.Guz ? "Güz" : "Bahar")}");
    }

    private async Task<CiktiDurumu> Durum(Sinav sinav)
    {
        var ciktilar = await db.OgrenmeCiktilari.Where(c => c.DersId == sinav.DersId).OrderBy(c => c.SiraNo).ToListAsync();
        var eslesme = await db.SoruOgrenmeCiktilari.Where(s => s.SinavId == sinav.Id).ToListAsync();
        var (kaynak, donem) = await GetirilecekCiktilar(sinav.DersId);
        var getirilebilir = kaynak.Count(k => ciktilar.All(c => !string.Equals(c.Aciklama, k.Aciklama, StringComparison.OrdinalIgnoreCase)));

        return new CiktiDurumu(sinav.SoruSayisi, sinav.KitapcikSayisi > 1, sinav.KitapcikEslestirmeKaydedildi,
            ciktilar.Select(c => new CiktiBilgisi(c.Id, c.SiraNo, c.Aciklama,
                eslesme.Where(e => e.OgrenmeCiktisiId == c.Id).Select(e => e.SoruNo).Order().ToList())).ToList(),
            getirilebilir, donem);
    }
}
