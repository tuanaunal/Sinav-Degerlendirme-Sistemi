using Microsoft.EntityFrameworkCore;
using Sds.Api.Data;
using Sds.Api.Domain;

namespace Sds.Tests;

public class VeriModeliTestleri
{
    [Theory]
    [InlineData("ali@selcuk.edu.tr", "12345", "ali12345*")]
    [InlineData("elif.yildirim@selcuk.edu.tr", "20417", "elif.yildirim20417*")]
    public void VarsayilanSifre_EpostaOnekiPersonelNoVeYildiz(string eposta, string personelNo, string beklenen)
    {
        Assert.Equal(beklenen, BaslangicVerisi.VarsayilanSifre(eposta, personelNo));
    }

    [Fact]
    public void DuzeltilmisPuan_ManuelPuanVarsaHamPuaninYerineGecer()
    {
        var sonuc = new SinavSonucu { HamPuan = 40, ManuelPuan = 48 };
        sonuc.Eklemeler.Add(new PuanEklemesi { Tur = PuanEklemeTuru.Bonus, Puan = 5 });

        Assert.Equal(53, sonuc.DuzeltilmisPuan());
    }

    [Fact]
    public void DuzeltilmisPuan_MaksimumPuaniAsamaz()
    {
        var sonuc = new SinavSonucu { HamPuan = 96 };
        sonuc.Eklemeler.Add(new PuanEklemesi { Tur = PuanEklemeTuru.Bonus, Puan = 10 });

        Assert.Equal(100, sonuc.DuzeltilmisPuan());
    }

    [Fact]
    public void Model_TumTablolarOlusturulur()
    {
        // Model kurulumu veritabanı bağlantısı gerektirmez; yapılandırma hatalarını yakalar
        var options = new DbContextOptionsBuilder<SdsDbContext>()
            .UseNpgsql("Host=localhost;Database=yok")
            .Options;
        using var db = new SdsDbContext(options);

        var tablolar = db.Model.GetEntityTypes().Select(t => t.GetTableName()).ToList();

        Assert.Equal(17, tablolar.Count);
        Assert.Contains("Sinavlar", tablolar);
        Assert.Contains("HarfNotuAraliklari", tablolar);

        var puan = db.Model.FindEntityType(typeof(SinavSonucu))!.FindProperty(nameof(SinavSonucu.HamPuan))!;
        Assert.Equal(2, puan.GetScale());
    }
}
