# Sınav Değerlendirme Sistemi API (ASP.NET Core 8)
# Derleme bağlamı depo köküdür (global.json, Sds.sln): docker build -f backend/Sds.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS derleme
WORKDIR /kaynak
COPY global.json ./
COPY backend/Sds.Api/Sds.Api.csproj backend/Sds.Api/
RUN dotnet restore backend/Sds.Api/Sds.Api.csproj
COPY backend/Sds.Api/ backend/Sds.Api/
RUN dotnet publish backend/Sds.Api/Sds.Api.csproj -c Release -o /yayin --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS calisma
WORKDIR /uygulama
# Türkçe tarih / sayı biçimleri ve Windows-1254 optik dosyaları için tam ICU
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_HTTP_PORTS=8080 \
    TZ=Europe/Istanbul
COPY --from=derleme /yayin .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sds.Api.dll"]
