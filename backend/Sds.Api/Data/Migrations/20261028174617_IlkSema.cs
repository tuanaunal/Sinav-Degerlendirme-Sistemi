using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sds.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class IlkSema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dersler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Kod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ad = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    AkademikYil = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Donem = table.Column<int>(type: "integer", nullable: false),
                    OgretimTuru = table.Column<int>(type: "integer", nullable: false),
                    Sinif = table.Column<int>(type: "integer", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dersler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HarfNotuAraliklari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Harf = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Katsayi = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    AltSinir = table.Column<int>(type: "integer", nullable: false),
                    UstSinir = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Sira = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HarfNotuAraliklari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kullanicilar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Unvan = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Ad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Soyad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Eposta = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PersonelNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SifreHash = table.Column<string>(type: "text", nullable: false),
                    Rol = table.Column<int>(type: "integer", nullable: false),
                    YoneticiYetkisi = table.Column<bool>(type: "boolean", nullable: false),
                    SifreDegistirmeli = table.Column<bool>(type: "boolean", nullable: false),
                    Aktif = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kullanicilar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotlandirmaAyarlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SemaAdi = table.Column<string>(type: "text", nullable: false),
                    VizeAgirligi = table.Column<int>(type: "integer", nullable: false),
                    FinalAgirligi = table.Column<int>(type: "integer", nullable: false),
                    FinalAltSiniri = table.Column<int>(type: "integer", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotlandirmaAyarlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ogrenciler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OgrenciNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdSoyad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ogrenciler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OptikFormAlanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Alan = table.Column<int>(type: "integer", nullable: false),
                    Baslangic = table.Column<int>(type: "integer", nullable: false),
                    Bitis = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptikFormAlanlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OgrenmeCiktilari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DersId = table.Column<int>(type: "integer", nullable: false),
                    SiraNo = table.Column<int>(type: "integer", nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OgrenmeCiktilari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OgrenmeCiktilari_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sinavlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DersId = table.Column<int>(type: "integer", nullable: false),
                    Tur = table.Column<int>(type: "integer", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    TamamlananAdim = table.Column<int>(type: "integer", nullable: false),
                    Tarih = table.Column<DateOnly>(type: "date", nullable: true),
                    SoruSayisi = table.Column<int>(type: "integer", nullable: false),
                    MaksimumPuan = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    KitapcikSayisi = table.Column<int>(type: "integer", nullable: false),
                    PuanlariDinamikDagit = table.Column<bool>(type: "boolean", nullable: false),
                    BirdenFazlaDogruKabul = table.Column<bool>(type: "boolean", nullable: false),
                    KitapcikEslestirmeKaydedildi = table.Column<bool>(type: "boolean", nullable: false),
                    OptikDosyaAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SinifListesiDosyaAdi = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    OlusturanKullaniciId = table.Column<int>(type: "integer", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sinavlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sinavlar_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DersEgitmenleri",
                columns: table => new
                {
                    DersId = table.Column<int>(type: "integer", nullable: false),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DersEgitmenleri", x => new { x.DersId, x.KullaniciId });
                    table.ForeignKey(
                        name: "FK_DersEgitmenleri_Dersler_DersId",
                        column: x => x.DersId,
                        principalTable: "Dersler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DersEgitmenleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SifreSifirlamaTalepleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KullaniciId = table.Column<int>(type: "integer", nullable: false),
                    TalepTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    IslemTarihi = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsleyenKullaniciId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SifreSifirlamaTalepleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SifreSifirlamaTalepleri_Kullanicilar_IsleyenKullaniciId",
                        column: x => x.IsleyenKullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SifreSifirlamaTalepleri_Kullanicilar_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "Kullanicilar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EslesmeyenOptikKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SinavId = table.Column<int>(type: "integer", nullable: false),
                    SatirNo = table.Column<int>(type: "integer", nullable: false),
                    OgrenciNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdSoyad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Kitapcik = table.Column<char>(type: "character(1)", nullable: true),
                    Cevaplar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EslesmeyenOptikKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EslesmeyenOptikKayitlari_Sinavlar_SinavId",
                        column: x => x.SinavId,
                        principalTable: "Sinavlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KitapcikSorulari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SinavId = table.Column<int>(type: "integer", nullable: false),
                    Kitapcik = table.Column<char>(type: "character(1)", nullable: false),
                    SoruNo = table.Column<int>(type: "integer", nullable: false),
                    AnaSoruNo = table.Column<int>(type: "integer", nullable: true),
                    DogruCevap = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitapcikSorulari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitapcikSorulari_Sinavlar_SinavId",
                        column: x => x.SinavId,
                        principalTable: "Sinavlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SinavSonuclari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SinavId = table.Column<int>(type: "integer", nullable: false),
                    OgrenciId = table.Column<int>(type: "integer", nullable: false),
                    Eslesme = table.Column<int>(type: "integer", nullable: false),
                    Kitapcik = table.Column<char>(type: "character(1)", nullable: true),
                    OptikOgrenciNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    OptikAdSoyad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    OptikCevaplar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DogruSayisi = table.Column<int>(type: "integer", nullable: false),
                    YanlisSayisi = table.Column<int>(type: "integer", nullable: false),
                    BosSayisi = table.Column<int>(type: "integer", nullable: false),
                    HamPuan = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    ManuelPuan = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinavSonuclari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SinavSonuclari_Ogrenciler_OgrenciId",
                        column: x => x.OgrenciId,
                        principalTable: "Ogrenciler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SinavSonuclari_Sinavlar_SinavId",
                        column: x => x.SinavId,
                        principalTable: "Sinavlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SinavSorulari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SinavId = table.Column<int>(type: "integer", nullable: false),
                    SoruNo = table.Column<int>(type: "integer", nullable: false),
                    Puan = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Iptal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinavSorulari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SinavSorulari_Sinavlar_SinavId",
                        column: x => x.SinavId,
                        principalTable: "Sinavlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoruOgrenmeCiktilari",
                columns: table => new
                {
                    SinavId = table.Column<int>(type: "integer", nullable: false),
                    OgrenmeCiktisiId = table.Column<int>(type: "integer", nullable: false),
                    SoruNo = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoruOgrenmeCiktilari", x => new { x.SinavId, x.OgrenmeCiktisiId, x.SoruNo });
                    table.ForeignKey(
                        name: "FK_SoruOgrenmeCiktilari_OgrenmeCiktilari_OgrenmeCiktisiId",
                        column: x => x.OgrenmeCiktisiId,
                        principalTable: "OgrenmeCiktilari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoruOgrenmeCiktilari_Sinavlar_SinavId",
                        column: x => x.SinavId,
                        principalTable: "Sinavlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PuanEklemeleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SinavSonucuId = table.Column<int>(type: "integer", nullable: false),
                    Tur = table.Column<int>(type: "integer", nullable: false),
                    Puan = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Tarih = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuanEklemeleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuanEklemeleri_SinavSonuclari_SinavSonucuId",
                        column: x => x.SinavSonucuId,
                        principalTable: "SinavSonuclari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoruCevaplari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SinavSonucuId = table.Column<int>(type: "integer", nullable: false),
                    SoruNo = table.Column<int>(type: "integer", nullable: false),
                    VerilenCevap = table.Column<char>(type: "character(1)", nullable: true),
                    DogruMu = table.Column<bool>(type: "boolean", nullable: true),
                    Puan = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoruCevaplari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoruCevaplari_SinavSonuclari_SinavSonucuId",
                        column: x => x.SinavSonucuId,
                        principalTable: "SinavSonuclari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DersEgitmenleri_KullaniciId",
                table: "DersEgitmenleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Dersler_Kod_AkademikYil_Donem_OgretimTuru",
                table: "Dersler",
                columns: new[] { "Kod", "AkademikYil", "Donem", "OgretimTuru" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EslesmeyenOptikKayitlari_SinavId",
                table: "EslesmeyenOptikKayitlari",
                column: "SinavId");

            migrationBuilder.CreateIndex(
                name: "IX_HarfNotuAraliklari_Harf",
                table: "HarfNotuAraliklari",
                column: "Harf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KitapcikSorulari_SinavId_Kitapcik_SoruNo",
                table: "KitapcikSorulari",
                columns: new[] { "SinavId", "Kitapcik", "SoruNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kullanicilar_Eposta",
                table: "Kullanicilar",
                column: "Eposta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ogrenciler_OgrenciNo",
                table: "Ogrenciler",
                column: "OgrenciNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OgrenmeCiktilari_DersId",
                table: "OgrenmeCiktilari",
                column: "DersId");

            migrationBuilder.CreateIndex(
                name: "IX_OptikFormAlanlari_Alan",
                table: "OptikFormAlanlari",
                column: "Alan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PuanEklemeleri_SinavSonucuId",
                table: "PuanEklemeleri",
                column: "SinavSonucuId");

            migrationBuilder.CreateIndex(
                name: "IX_SifreSifirlamaTalepleri_IsleyenKullaniciId",
                table: "SifreSifirlamaTalepleri",
                column: "IsleyenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SifreSifirlamaTalepleri_KullaniciId",
                table: "SifreSifirlamaTalepleri",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Sinavlar_DersId_Tur",
                table: "Sinavlar",
                columns: new[] { "DersId", "Tur" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SinavSonuclari_OgrenciId",
                table: "SinavSonuclari",
                column: "OgrenciId");

            migrationBuilder.CreateIndex(
                name: "IX_SinavSonuclari_SinavId_OgrenciId",
                table: "SinavSonuclari",
                columns: new[] { "SinavId", "OgrenciId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SinavSorulari_SinavId_SoruNo",
                table: "SinavSorulari",
                columns: new[] { "SinavId", "SoruNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoruCevaplari_SinavSonucuId_SoruNo",
                table: "SoruCevaplari",
                columns: new[] { "SinavSonucuId", "SoruNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoruOgrenmeCiktilari_OgrenmeCiktisiId",
                table: "SoruOgrenmeCiktilari",
                column: "OgrenmeCiktisiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DersEgitmenleri");

            migrationBuilder.DropTable(
                name: "EslesmeyenOptikKayitlari");

            migrationBuilder.DropTable(
                name: "HarfNotuAraliklari");

            migrationBuilder.DropTable(
                name: "KitapcikSorulari");

            migrationBuilder.DropTable(
                name: "NotlandirmaAyarlari");

            migrationBuilder.DropTable(
                name: "OptikFormAlanlari");

            migrationBuilder.DropTable(
                name: "PuanEklemeleri");

            migrationBuilder.DropTable(
                name: "SifreSifirlamaTalepleri");

            migrationBuilder.DropTable(
                name: "SinavSorulari");

            migrationBuilder.DropTable(
                name: "SoruCevaplari");

            migrationBuilder.DropTable(
                name: "SoruOgrenmeCiktilari");

            migrationBuilder.DropTable(
                name: "Kullanicilar");

            migrationBuilder.DropTable(
                name: "SinavSonuclari");

            migrationBuilder.DropTable(
                name: "OgrenmeCiktilari");

            migrationBuilder.DropTable(
                name: "Ogrenciler");

            migrationBuilder.DropTable(
                name: "Sinavlar");

            migrationBuilder.DropTable(
                name: "Dersler");
        }
    }
}
