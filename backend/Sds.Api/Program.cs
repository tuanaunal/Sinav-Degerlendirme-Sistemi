using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sds.Api.Auth;
using Sds.Api.Data;
using Sds.Api.Domain;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCors = "frontend";

builder.Services.AddDbContext<SdsDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// --- Kimlik doğrulama (JWT) ---
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtAyarlari>() ?? new JwtAyarlari();
if (Encoding.UTF8.GetByteCount(jwt.Anahtar) < 32)
    throw new InvalidOperationException("Jwt:Anahtar en az 32 bayt olmalıdır (appsettings veya ortam değişkeni).");

builder.Services.Configure<JwtAyarlari>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<TokenServisi>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Yayinci,
        ValidateAudience = true,
        ValidAudience = jwt.Hedef,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Anahtar)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy(Yetki.GirisYapmis, p => p.RequireAuthenticatedUser());

    // Şifresini değiştirmesi gereken kullanıcı, şifre değiştirme dışında hiçbir işlem yapamaz
    o.AddPolicy(Yetki.Aktif, p => p.RequireAuthenticatedUser()
        .RequireAssertion(c => c.User.FindFirst(Yetki.ClaimSifreDegistirmeli)?.Value != "true"));
    o.AddPolicy(Yetki.Yonetici, p => p.RequireAuthenticatedUser()
        .RequireAssertion(c => c.User.FindFirst(Yetki.ClaimSifreDegistirmeli)?.Value != "true")
        .RequireClaim(Yetki.ClaimYonetici, "true"));
    o.AddPolicy(Yetki.OgretimUyesi, p => p.RequireAuthenticatedUser()
        .RequireAssertion(c => c.User.FindFirst(Yetki.ClaimSifreDegistirmeli)?.Value != "true")
        .RequireRole(nameof(KullaniciRolu.OgretimUyesi)));

    // [Authorize] yazılan her uç nokta varsayılan olarak "Aktif" politikasını kullanır
    o.DefaultPolicy = o.GetPolicy(Yetki.Aktif)!;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sınav Değerlendirme Sistemi API", Version = "v1" });
    var sema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
    };
    c.AddSecurityDefinition("Bearer", sema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [sema] = [] });
});

// Vite geliştirme sunucusu (http://localhost:5173) API'ye erişebilsin
builder.Services.AddCors(o => o.AddPolicy(FrontendCors, p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// Bekleyen migration'ları uygula ve başlangıç verisini ekle
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SdsDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();
    await BaslangicVerisi.UygulaAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Entegrasyon testlerinin WebApplicationFactory ile erişebilmesi için.</summary>
public partial class Program;
