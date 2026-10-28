using Microsoft.EntityFrameworkCore;
using Sds.Api.Data;

var builder = WebApplication.CreateBuilder(args);

const string FrontendCors = "frontend";

builder.Services.AddDbContext<SdsDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Sınav Değerlendirme Sistemi API", Version = "v1" });
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
    await db.Database.MigrateAsync();
    await BaslangicVerisi.UygulaAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCors);
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Entegrasyon testlerinin WebApplicationFactory ile erişebilmesi için.</summary>
public partial class Program;
