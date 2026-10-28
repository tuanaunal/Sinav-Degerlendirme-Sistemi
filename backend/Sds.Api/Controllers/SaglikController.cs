using Microsoft.AspNetCore.Mvc;

namespace Sds.Api.Controllers;

/// <summary>Sistemin ayakta olduğunu kontrol etmek için basit uç nokta.</summary>
[ApiController]
[Route("api/saglik")]
public class SaglikController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        durum = "calisiyor",
        sistem = "Sınav Değerlendirme Sistemi",
        kurum = "Selçuk Üniversitesi",
        zaman = DateTime.UtcNow
    });
}
