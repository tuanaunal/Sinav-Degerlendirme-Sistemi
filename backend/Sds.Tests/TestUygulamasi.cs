using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sds.Api.Controllers;
using Sds.Api.Data;

namespace Sds.Tests;

/// <summary>API'yi bellekte çalıştırır; PostgreSQL yerine her test sınıfına ayrı bir in-memory veritabanı verir.</summary>
public class TestUygulamasi : WebApplicationFactory<Program>
{
    private readonly string _veritabaniAdi = $"sds-test-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Anahtar", "test-anahtari-en-az-otuz-iki-bayt-uzunlugunda-olmali");
        builder.ConfigureServices(s =>
        {
            s.RemoveAll<DbContextOptions<SdsDbContext>>();
            s.AddDbContext<SdsDbContext>(o => o.UseInMemoryDatabase(_veritabaniAdi));
        });
    }

    public SdsDbContext Veritabani() => Services.CreateScope().ServiceProvider.GetRequiredService<SdsDbContext>();

    public async Task<HttpClient> GirisYapmisIstemciAsync(string eposta, string sifre)
    {
        var istemci = CreateClient();
        var yanit = await istemci.PostAsJsonAsync("/api/auth/giris", new GirisIstegi(eposta, sifre));
        yanit.EnsureSuccessStatusCode();
        var oturum = await yanit.Content.ReadFromJsonAsync<OturumYaniti>();
        istemci.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", oturum!.Token);
        return istemci;
    }
}

internal static class ServisUzantilari
{
    public static void RemoveAll<T>(this IServiceCollection s)
    {
        foreach (var d in s.Where(d => d.ServiceType == typeof(T)).ToList())
            s.Remove(d);
    }
}
