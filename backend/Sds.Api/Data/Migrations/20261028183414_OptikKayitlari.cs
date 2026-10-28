using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Sds.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptikKayitlari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EslesmeyenOptikKayitlari");

            migrationBuilder.CreateTable(
                name: "OptikKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SinavId = table.Column<int>(type: "integer", nullable: false),
                    SatirNo = table.Column<int>(type: "integer", nullable: false),
                    OgrenciNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AdSoyad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Kitapcik = table.Column<char>(type: "character(1)", nullable: true),
                    SinavTuruKodu = table.Column<char>(type: "character(1)", nullable: true),
                    Cevaplar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CevapAnahtariMi = table.Column<bool>(type: "boolean", nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    SinavSonucuId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptikKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptikKayitlari_Sinavlar_SinavId",
                        column: x => x.SinavId,
                        principalTable: "Sinavlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OptikKayitlari_SinavId_SatirNo",
                table: "OptikKayitlari",
                columns: new[] { "SinavId", "SatirNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OptikKayitlari");

            migrationBuilder.CreateTable(
                name: "EslesmeyenOptikKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SinavId = table.Column<int>(type: "integer", nullable: false),
                    AdSoyad = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Cevaplar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Durum = table.Column<int>(type: "integer", nullable: false),
                    Kitapcik = table.Column<char>(type: "character(1)", nullable: true),
                    OgrenciNo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SatirNo = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_EslesmeyenOptikKayitlari_SinavId",
                table: "EslesmeyenOptikKayitlari",
                column: "SinavId");
        }
    }
}
