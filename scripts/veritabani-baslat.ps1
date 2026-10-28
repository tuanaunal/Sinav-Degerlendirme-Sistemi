# Yerel PostgreSQL 16 sunucusunu başlatır (bilgisayar yeniden başladıktan sonra çalıştırın).
# Not: Türkçe Windows yerel ayarında kurulum sihirbazının initdb adımı hata verdiği için
# veri klasörü "C" yerel ayarıyla kullanıcı klasöründe oluşturulmuştur.
$pgBin = "C:\Program Files\PostgreSQL\16\bin"
$veri = Join-Path $env:LOCALAPPDATA "PostgreSQL\16\data"
$log = Join-Path $env:LOCALAPPDATA "PostgreSQL\16\postgres.log"

& "$pgBin\pg_ctl.exe" -D $veri status *> $null
if ($LASTEXITCODE -eq 0) {
    Write-Host "PostgreSQL zaten çalışıyor."
    exit 0
}

Start-Process -FilePath "$pgBin\pg_ctl.exe" -ArgumentList @("-D", "`"$veri`"", "-l", "`"$log`"", "start") -WindowStyle Hidden -Wait
Write-Host "PostgreSQL başlatıldı (port 5432)."
