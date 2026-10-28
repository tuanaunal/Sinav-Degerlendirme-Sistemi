using Microsoft.EntityFrameworkCore;
using Sds.Api.Domain;

namespace Sds.Api.Data;

public class SdsDbContext(DbContextOptions<SdsDbContext> options) : DbContext(options)
{
    public DbSet<Kullanici> Kullanicilar => Set<Kullanici>();
    public DbSet<SifreSifirlamaTalebi> SifreSifirlamaTalepleri => Set<SifreSifirlamaTalebi>();
    public DbSet<Ders> Dersler => Set<Ders>();
    public DbSet<DersEgitmen> DersEgitmenleri => Set<DersEgitmen>();
    public DbSet<OgrenmeCiktisi> OgrenmeCiktilari => Set<OgrenmeCiktisi>();
    public DbSet<Ogrenci> Ogrenciler => Set<Ogrenci>();
    public DbSet<Sinav> Sinavlar => Set<Sinav>();
    public DbSet<SinavSorusu> SinavSorulari => Set<SinavSorusu>();
    public DbSet<KitapcikSorusu> KitapcikSorulari => Set<KitapcikSorusu>();
    public DbSet<SinavSonucu> SinavSonuclari => Set<SinavSonucu>();
    public DbSet<SoruCevabi> SoruCevaplari => Set<SoruCevabi>();
    public DbSet<PuanEklemesi> PuanEklemeleri => Set<PuanEklemesi>();
    public DbSet<OptikKaydi> OptikKayitlari => Set<OptikKaydi>();
    public DbSet<SoruOgrenmeCiktisi> SoruOgrenmeCiktilari => Set<SoruOgrenmeCiktisi>();
    public DbSet<NotlandirmaAyari> NotlandirmaAyarlari => Set<NotlandirmaAyari>();
    public DbSet<HarfNotuAraligi> HarfNotuAraliklari => Set<HarfNotuAraligi>();
    public DbSet<OptikFormAlani> OptikFormAlanlari => Set<OptikFormAlani>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        // Tüm puanlar 2 ondalık basamakla tutulur (örn. 87.50)
        c.Properties<decimal>().HavePrecision(6, 2);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Kullanici>(e =>
        {
            e.HasIndex(x => x.Eposta).IsUnique();
            e.Property(x => x.Eposta).HasMaxLength(150);
            e.Property(x => x.Ad).HasMaxLength(80);
            e.Property(x => x.Soyad).HasMaxLength(80);
            e.Property(x => x.Unvan).HasMaxLength(40);
            e.Property(x => x.PersonelNo).HasMaxLength(20);
            e.Ignore(x => x.TamAd);
        });

        b.Entity<SifreSifirlamaTalebi>(e =>
        {
            e.HasOne(x => x.Kullanici).WithMany().HasForeignKey(x => x.KullaniciId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.IsleyenKullanici).WithMany().HasForeignKey(x => x.IsleyenKullaniciId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Ders>(e =>
        {
            e.Property(x => x.Kod).HasMaxLength(20);
            e.Property(x => x.Ad).HasMaxLength(150);
            e.Property(x => x.AkademikYil).HasMaxLength(9);
            e.HasIndex(x => new { x.Kod, x.AkademikYil, x.Donem, x.OgretimTuru }).IsUnique();
        });

        b.Entity<DersEgitmen>(e =>
        {
            e.HasKey(x => new { x.DersId, x.KullaniciId });
            e.HasOne(x => x.Ders).WithMany(d => d.Egitmenler).HasForeignKey(x => x.DersId);
            e.HasOne(x => x.Kullanici).WithMany(k => k.Dersleri).HasForeignKey(x => x.KullaniciId);
        });

        b.Entity<OgrenmeCiktisi>(e =>
        {
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasOne(x => x.Ders).WithMany(d => d.OgrenmeCiktilari).HasForeignKey(x => x.DersId);
        });

        b.Entity<Ogrenci>(e =>
        {
            e.HasIndex(x => x.OgrenciNo).IsUnique();
            e.Property(x => x.OgrenciNo).HasMaxLength(20);
            e.Property(x => x.AdSoyad).HasMaxLength(120);
        });

        b.Entity<Sinav>(e =>
        {
            e.HasIndex(x => new { x.DersId, x.Tur }).IsUnique();
            e.HasOne(x => x.Ders).WithMany(d => d.Sinavlar).HasForeignKey(x => x.DersId);
            e.Property(x => x.OptikDosyaAdi).HasMaxLength(255);
            e.Property(x => x.SinifListesiDosyaAdi).HasMaxLength(255);
        });

        b.Entity<SinavSorusu>(e =>
        {
            e.HasIndex(x => new { x.SinavId, x.SoruNo }).IsUnique();
            e.HasOne(x => x.Sinav).WithMany(s => s.Sorular).HasForeignKey(x => x.SinavId);
        });

        b.Entity<KitapcikSorusu>(e =>
        {
            e.HasIndex(x => new { x.SinavId, x.Kitapcik, x.SoruNo }).IsUnique();
            e.Property(x => x.DogruCevap).HasMaxLength(5);
            e.HasOne(x => x.Sinav).WithMany(s => s.KitapcikSorulari).HasForeignKey(x => x.SinavId);
        });

        b.Entity<SinavSonucu>(e =>
        {
            e.HasIndex(x => new { x.SinavId, x.OgrenciId }).IsUnique();
            e.HasOne(x => x.Sinav).WithMany(s => s.Sonuclar).HasForeignKey(x => x.SinavId);
            e.HasOne(x => x.Ogrenci).WithMany().HasForeignKey(x => x.OgrenciId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.OptikOgrenciNo).HasMaxLength(20);
            e.Property(x => x.OptikAdSoyad).HasMaxLength(120);
            e.Property(x => x.OptikCevaplar).HasMaxLength(200);
            e.Ignore(x => x.SinavaGirdi);
        });

        b.Entity<SoruCevabi>(e =>
        {
            e.HasIndex(x => new { x.SinavSonucuId, x.SoruNo }).IsUnique();
            e.HasOne(x => x.SinavSonucu).WithMany(s => s.SoruCevaplari).HasForeignKey(x => x.SinavSonucuId);
        });

        b.Entity<PuanEklemesi>(e =>
        {
            e.Property(x => x.Aciklama).HasMaxLength(200);
            e.HasOne(x => x.SinavSonucu).WithMany(s => s.Eklemeler).HasForeignKey(x => x.SinavSonucuId);
        });

        b.Entity<OptikKaydi>(e =>
        {
            e.HasIndex(x => new { x.SinavId, x.SatirNo }).IsUnique();
            e.Property(x => x.OgrenciNo).HasMaxLength(20);
            e.Property(x => x.AdSoyad).HasMaxLength(120);
            e.Property(x => x.Cevaplar).HasMaxLength(200);
            e.HasOne(x => x.Sinav).WithMany(s => s.OptikKayitlari).HasForeignKey(x => x.SinavId);
        });

        b.Entity<SoruOgrenmeCiktisi>(e =>
        {
            e.HasKey(x => new { x.SinavId, x.OgrenmeCiktisiId, x.SoruNo });
            e.HasOne(x => x.Sinav).WithMany(s => s.SoruOgrenmeCiktilari).HasForeignKey(x => x.SinavId);
            e.HasOne(x => x.OgrenmeCiktisi).WithMany().HasForeignKey(x => x.OgrenmeCiktisiId);
        });

        b.Entity<HarfNotuAraligi>(e =>
        {
            e.HasIndex(x => x.Harf).IsUnique();
            e.Property(x => x.Harf).HasMaxLength(2);
            e.Property(x => x.Durum).HasMaxLength(20);
        });

        b.Entity<OptikFormAlani>(e => e.HasIndex(x => x.Alan).IsUnique());
    }
}
