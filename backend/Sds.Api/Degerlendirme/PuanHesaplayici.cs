using Sds.Api.Domain;

namespace Sds.Api.Degerlendirme;

/// <summary>Bir sorunun puanlanmış hâli; SoruNo A kitapçığı numarasıdır (eşleştirme yoksa kitapçıktaki sırası).</summary>
public record PuanlanmisSoru(int SoruNo, char? VerilenCevap, bool DogruMu, decimal Puan);

public record PuanSonucu(int Dogru, int Yanlis, int Bos, decimal HamPuan, List<PuanlanmisSoru> Sorular);

/// <summary>
/// Çoktan seçmeli kağıdı cevap anahtarıyla karşılaştırır. Kağıt kendi kitapçığının anahtarıyla puanlanır;
/// soru puanları A kitapçığı sırasındadır, diğer kitapçıklar 5. adımdaki soru eşleştirmesiyle A'ya çevrilir
/// (eşleştirme henüz girilmemişse kitapçıktaki sıra kullanılır, dinamik dağıtımda sonuç değişmez).
/// Boş (' ') boş sayılır; birden fazla işaretleme ('*') yanlıştır; iptal edilen soru herkese tam puan verir.
/// </summary>
public static class PuanHesaplayici
{
    public static PuanSonucu Hesapla(
        string cevaplar, char kitapcik, IReadOnlyList<KitapcikSorusu> anahtar, IReadOnlyList<SinavSorusu> sorular)
    {
        var puanlar = sorular.ToDictionary(s => s.SoruNo);
        var kitapcikAnahtari = anahtar.Where(k => k.Kitapcik == kitapcik).OrderBy(k => k.SoruNo).ToList();
        if (kitapcikAnahtari.Count == 0)
            throw new InvalidOperationException($"{kitapcik} kitapçığı için cevap anahtarı yok.");

        int dogru = 0, yanlis = 0, bos = 0;
        decimal ham = 0;
        var liste = new List<PuanlanmisSoru>();
        foreach (var k in kitapcikAnahtari)
        {
            var anaNo = k.AnaSoruNo ?? k.SoruNo;
            var soru = puanlar.GetValueOrDefault(anaNo);
            var c = k.SoruNo <= cevaplar.Length ? char.ToUpperInvariant(cevaplar[k.SoruNo - 1]) : ' ';
            char? verilen = c is ' ' or '\0' ? null : c;

            bool dogruMu;
            if (soru?.Iptal == true) dogruMu = true;
            else if (verilen is null) dogruMu = false;
            else dogruMu = verilen != '*' && k.DogruCevap.Contains(verilen.Value);

            if (dogruMu) dogru++;
            else if (verilen is null) bos++;
            else yanlis++;

            var puan = dogruMu ? soru?.Puan ?? 0 : 0;
            ham += puan;
            liste.Add(new PuanlanmisSoru(anaNo, verilen, dogruMu, puan));
        }
        return new PuanSonucu(dogru, yanlis, bos, ham, liste.OrderBy(s => s.SoruNo).ToList());
    }
}
