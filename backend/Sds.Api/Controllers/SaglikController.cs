using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sds.Api.Data;

namespace Sds.Api.Controllers;

/// <summary>Sistemin ve veritabanının ayakta olduğunu kontrol etmek için basit uç nokta.</summary>
[ApiController]
[Route("api/saglik")]
public class SaglikController(SdsDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        object veritabani;
        try
        {
            veritabani = new
            {
                bagli = await db.Database.CanConnectAsync(),
                kullaniciSayisi = await db.Kullanicilar.CountAsync(),
                dersSayisi = await db.Dersler.CountAsync(),
                harfNotuSayisi = await db.HarfNotuAraliklari.CountAsync(),
                uygulananMigrationlar = (await db.Database.GetAppliedMigrationsAsync()).Count(),
            };
        }
        catch (Exception)
        {
            veritabani = new { bagli = false };
        }

        return Ok(new
        {
            durum = "calisiyor",
            sistem = "Sınav Değerlendirme Sistemi",
            kurum = "Selçuk Üniversitesi",
            zaman = DateTime.UtcNow,
            veritabani,
        });
    }
}
