# Geliştirme ortamı gizli ayarlarını .NET User Secrets deposuna yazar (repoya girmez).
# Yeni bir bilgisayarda projeyi ilk kez çalıştırmadan önce bir kez çalıştırın:
#   powershell -ExecutionPolicy Bypass -File scripts/gizli-ayarlari-kur.ps1 -VeritabaniSifresi <postgres şifresi>
param(
    [Parameter(Mandatory = $true)][string]$VeritabaniSifresi
)

$proje = Join-Path $PSScriptRoot "..\backend\Sds.Api"

# Her kurulumda rastgele 48 baytlık JWT imza anahtarı üretilir
$bayt = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bayt)
$anahtar = [Convert]::ToBase64String($bayt)

dotnet user-secrets set "Jwt:Anahtar" $anahtar --project $proje | Out-Null
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=sds;Username=postgres;Password=$VeritabaniSifresi" --project $proje | Out-Null

Write-Host "Gizli ayarlar kaydedildi (JWT anahtarı ve veritabanı bağlantısı)."
