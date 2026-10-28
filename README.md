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

Gerekenler: .NET 8 SDK, Node.js 22+, PostgreSQL 16.

JWT imza anahtarı ve veritabanı şifresi repoda tutulmaz; .NET User Secrets ile bilgisayarda saklanır.
Yeni bir bilgisayarda ilk çalıştırmadan önce bir kez:

```bash
powershell -ExecutionPolicy Bypass -File scripts/gizli-ayarlari-kur.ps1 -VeritabaniSifresi <postgres-sifresi>
```

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

## Docker ile çalıştırma

Gerekenler: Docker Desktop (veya Docker Engine + Compose eklentisi). PostgreSQL, API ve Nginx üç ayrı konteynerde çalışır;
Nginx arayüzü sunar ve `/api` isteklerini API konteynerine iletir.

```bash
cp .env.example .env        # POSTGRES_PASSWORD ve JWT_ANAHTAR değerlerini değiştirin
docker compose up -d --build
```

Uygulama: http://localhost:8080 — ilk açılışta veritabanı tabloları ve demo hesaplar otomatik oluşturulur.

```bash
docker compose logs -f api   # API günlükleri
docker compose down          # durdur (veriler sds-veri biriminde kalır)
docker compose down -v       # durdur ve veritabanını sil
```

## Sürekli entegrasyon

Her push'ta GitHub Actions ([.github/workflows/ci.yml](.github/workflows/ci.yml)) şunları çalıştırır:

1. **Backend:** derleme ve xUnit testleri (örnek veri dosyalarıyla uçtan uca sınav akışları dahil)
2. **Frontend:** ESLint ve üretim derlemesi
3. **Docker:** imajlar derlenir, `docker compose` ile ayağa kaldırılır ve Nginx üzerinden API ile arayüzün yanıt verdiği doğrulanır

## Demo hesaplar

Şifreler sistemin kuralıyla üretilir: e-postanın @ öncesi + personel no + `*`.

| Rol | E-posta | Şifre |
|---|---|---|
| Admin | `admin@selcuk.edu.tr` | `admin10001*` |
| Admin + Öğretim Üyesi | `elif.yildirim@selcuk.edu.tr` | `elif.yildirim20417*` |
| Öğretim Üyesi | `mehmet.kaya@selcuk.edu.tr` | `mehmet.kaya20533*` |

## Geliştirme aşamaları

Proje aşama aşama geliştirilmektedir; her aşama ayrı bir commit ve `asama-XX` etiketi ile işaretlenir.
Ayrıntılar için [docs/YOL_HARITASI.md](docs/YOL_HARITASI.md).
