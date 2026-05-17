# ŞarjRota API

Elektrikli araç şarj istasyonu rota planlama platformunun backend API'si.

## 🔋 Özellikler

- **Akıllı Rota Hesaplama** — OSRM tabanlı gerçek yol mesafesi ile batarya optimizasyonu
- **Şarj İstasyonu Veritabanı** — ZES, Eşarj, Trugo, Tesla Supercharger
- **Araç Veritabanı** — Türkiye'deki popüler EV modellerinin menzil ve batarya bilgileri
- **Rate Limiting** — IP bazlı DoS koruması
- **Güvenlik Middleware'leri** — Security headers, global exception handling

## 🛠 Teknolojiler

- **.NET 10** — ASP.NET Core Web API
- **Entity Framework Core** — ORM
- **SQLite** — Veritabanı
- **OSRM** — Open Source Routing Machine
- **xUnit** — Unit Testing
- **Swagger** — API Dokümantasyonu

## 🏗 Mimari (Clean Architecture)

```
ElektrikliRota.Core/            # Domain entities & interfaces
ElektrikliRota.Application/     # Business logic & services
ElektrikliRota.Infrastructure/  # Data access & external services
ElektrikliRota.WebApi/          # API controllers & middlewares
ElektrikliRota.UnitTests/       # xUnit test projesi
```

## 🚀 Kurulum

```bash
# Bağımlılıkları yükle
dotnet restore

# Geliştirme sunucusu
dotnet run --project ElektrikliRota.WebApi

# Testleri çalıştır
dotnet test

# Production build
dotnet publish ElektrikliRota.WebApi -c Release -o ./publish
```

## 📡 API Endpoints

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| `GET` | `/api/vehicles` | Araç listesi |
| `GET` | `/api/stations` | Şarj istasyonları |
| `POST` | `/api/route/calculate` | Rota hesaplama |

Swagger UI: `http://localhost:5261/swagger`

## 🔗 İlgili Projeler

- [ŞarjRota Web](https://github.com/ferhatcklt/sarjrotaweb) — React Frontend
- [ŞarjRota Mobil](https://github.com/ferhatcklt/sarjrotamobil) — React Native / Expo

## 📄 Lisans

MIT
