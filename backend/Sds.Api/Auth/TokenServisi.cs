using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sds.Api.Domain;

namespace Sds.Api.Auth;

public class TokenServisi(IOptions<JwtAyarlari> ayarlar)
{
    private readonly JwtAyarlari _ayarlar = ayarlar.Value;

    public (string Token, DateTime GecerlilikSonu) Olustur(Kullanici k)
    {
        var bitis = DateTime.UtcNow.AddMinutes(_ayarlar.SureDakika);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, k.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, k.Eposta),
            new(ClaimTypes.Name, k.TamAd),
            new(ClaimTypes.Role, k.Rol.ToString()),
            new(Yetki.ClaimYonetici, (k.Rol == KullaniciRolu.Admin || k.YoneticiYetkisi).ToString().ToLowerInvariant()),
            new(Yetki.ClaimSifreDegistirmeli, k.SifreDegistirmeli.ToString().ToLowerInvariant()),
        };

        var anahtar = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_ayarlar.Anahtar));
        var token = new JwtSecurityToken(
            issuer: _ayarlar.Yayinci,
            audience: _ayarlar.Hedef,
            claims: claims,
            expires: bitis,
            signingCredentials: new SigningCredentials(anahtar, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), bitis);
    }
}

public static class ClaimsUzantilari
{
    public static int KullaniciId(this ClaimsPrincipal p) =>
        int.Parse(p.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? p.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
