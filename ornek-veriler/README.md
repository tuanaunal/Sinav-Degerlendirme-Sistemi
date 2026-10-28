# Örnek Veriler

Sistemi gerçekçi bir sınıfla denemek için üretilmiş dosyalar. Tamamı kurgusaldır; gerçek öğrencilere ait değildir.
Dosyalar sabit tohumlu bir üretici ile oluşturulur; her çalıştırmada aynı içerik çıkar:

```bash
dotnet run --project araclar/Sds.OrnekVeri
```

**Ders:** 3304701 – Bilgisayar Mühendisliği Uygulamaları · 2026-2027 Güz · 4. sınıf · Normal Öğretim
**Sınıf mevcudu:** 120 öğrenci (104'ü 2023 girişli, 16'sı dersi tekrar alan 2022 / 2021 girişli)

## Dosyalar

| Dosya | Ne için | Biçim |
|---|---|---|
| `OBIS_Sinif_Listesi_3304701_2026-2027_Guz.xlsx` | Vize ve final sınıf listesi | OBİS sınav şablonu: başlık bilgileri + `Sıra No, Öğrenci No, Adı, Soyadı, Sınıf, Durum` |
| `Optik_3304701_Vize.txt` | Vize (çoktan seçmeli, 25 soru, A/B kitapçık) | Optik okuyucu çıktısı, Windows-1254, sabit genişlik |
| `Final_Acik_Uclu_Notlar_3304701.xlsx` | Final (açık uçlu, 10 soru × 10 puan) | `OGRENCINO, ADSOYAD, S1 … S10` |
| `Proje_Notlari_3304701.xlsx` | Not düzenleme adımında "Proje Notları Yükle" | `OGRENCINO, ADSOYAD, SINAVNOTU` (sınav notuna eklenecek 1–5 puan) |
| `OBIS_Sinif_Listesi_3304701_2026-2027_Guz_Butunleme.xlsx` | Bütünleme sınıf listesi (yalnızca hakkı olan 61 öğrenci) | OBİS sınav şablonu |
| `Optik_3304701_Butunleme.txt` | Bütünleme (çoktan seçmeli, 25 soru, A/B kitapçık) | Optik okuyucu çıktısı |

## Optik dosya düzeni

Sistemin varsayılan optik form ayarlarıyla birebir uyumludur (0 tabanlı, bitiş dahil):

| Alan | Sütunlar | Örnek |
|---|---|---|
| Öğrenci numarası | 0–8 | `233301014` |
| Sınav türü | 9 | `1` vize, `3` bütünleme |
| Ad soyad | 10–30 | `AYŞE NAZ YALÇIN` (21 karakteri aşan adlar kesilir) |
| Kitapçık türü | 31 | `A` / `B` |
| Cevaplar | 32–56 | `A`–`E`, boş bırakılan soru için boşluk, birden fazla işaretleme için `*` |

İlk iki satır, öğretim üyesinin optiğe okuttuğu **cevap anahtarı** kağıtlarıdır (numara `000000000`, ad `CEVAP ANAHTARI`,
kitapçık A ve B). Cevap anahtarı adımında "Optikten seç" ile bu satırlar seçilir.

## Bilerek eklenen gerçek hayat durumları (vize)

| Durum | Öğrenci | Sistemde nerede görülür |
|---|---|---|
| 5 öğrenci sınava girmemiş | — | Veri eşleştirme: "sınava girmedi" |
| Numaranın ilk hanesi yanlış kodlanmış (`133301032`) | Ayşe Naz Yalçın | Veri eşleştirme: otomatik tanınır |
| Numaranın bir hanesi yanlış kodlanmış (`233307110`) | Emre Bayram | Veri eşleştirme: otomatik tanınır |
| Numara hiç kodlanmamış | Meryem Yaman | Veri eşleştirme: isimden elle eşleştirilir |
| Kitapçık türü boş bırakılmış | Ece Şen (B kitapçığı) | Puanlama: kitapçık sorulur |
| Sınıf listesinde olmayan öğrenci (`233302014`, başka şube) | Burak Şahin | Veri eşleştirme: göz ardı edilir |
| 3. soru çok kolay | — | Analiz: ~%97 doğru |
| 10. soru çok zor | — | Analiz: ~%3 doğru |
| 17. soruda öğrencilerin büyük çoğunluğu B'yi seçmiş, anahtar D | — | Analiz: cevap anahtarı hatası şüphesi |

## Doğrulama dosyaları (`_dogrulama/`)

Sistemin hesaplaması gereken doğru sonuçlar; testlerde ve elle kontrolde kullanılır.

| Dosya | İçerik |
|---|---|
| `vize_beklenen_sonuclar.csv` | Her öğrencinin kitapçığı, doğru / yanlış / boş sayısı, ham puanı ve senaryosu |
| `vize_cevap_anahtari.txt` | A ve B kitapçığı cevap anahtarları + B → A soru eşleştirmesi |
| `final_beklenen_sonuclar.csv` | Açık uçlu finalin soru puanları ve toplamı |
| `butunleme_beklenen_sonuclar.csv`, `butunleme_cevap_anahtari.txt` | Bütünleme için aynısı |

**Özet istatistikler:** Vize 115 giren, ortalama 54,1 · Final 116 giren, ortalama 62,7 ·
Bütünleme hakkı 61 öğrenci, giren 50.
