# Sınav Değerlendirme Sistemi (SDS)

**Selçuk Üniversitesi · Teknoloji Fakültesi · Bilgisayar Mühendisliği Uygulamaları**

Ölçme ve değerlendirme işlemlerinin yönetilmesini ve takibini kolaylaştıran platform. Öğretim üyesi yalnızca
sınıf listesini (OBİS `.xlsx`) ve optik okuyucu dosyasını (`.txt`) yükler; eşleştirme, puanlama, harf notu,
öğrenme çıktısı analizi ve AMÜDEK için resmi "Sınav Başarısı Analiz ve Değerlendirme Formu" sistem tarafından üretilir.

## Kullanılan teknolojiler

| Katman | Teknoloji |
|---|---|
| Sunucu | ASP.NET Core 8 (C#), Entity Framework Core, PostgreSQL, JWT |
| Arayüz | React + TypeScript, Vite, Tailwind CSS, TanStack Query |
| Dağıtım ve test | Docker + Nginx, GitHub Actions (CI), xUnit |

Arayüz, Selçuk Üniversitesi kurumsal kimliğine göre hazırlanmıştır: lacivert `#0D2D5E`, kurumsal mavi `#1C458B`,
altın `#FFC90D`, Montserrat yazı tipi ve resmi amblem.

## Klasör yapısı

```
backend/Sds.Api      ASP.NET Core 8 Web API
backend/Sds.Tests    xUnit testleri
frontend/            React + TypeScript arayüzü
docs/                Yol haritası ve belgeler
ornek-veriler/       Gerçekçi örnek optik ve sınıf listesi dosyaları (Aşama 5)
```

## Geliştirme ortamında çalıştırma

Gerekenler: .NET 8 SDK, Node.js 20+, PostgreSQL 16.

```bash
# Veritabanı (bilgisayar yeniden başladıysa)
powershell -ExecutionPolicy Bypass -File scripts/veritabani-baslat.ps1

# Sunucu: http://localhost:5080 (Swagger: /swagger)
dotnet run --project backend/Sds.Api

# Arayüz: http://localhost:5173
cd frontend
npm install
npm run dev
```

## Geliştirme aşamaları

Proje aşama aşama geliştirilmektedir; her aşama ayrı bir commit ve `asama-XX` etiketi ile işaretlenir.
Ayrıntılar için [docs/YOL_HARITASI.md](docs/YOL_HARITASI.md).
