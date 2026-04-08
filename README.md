<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/PostgreSQL-15-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" />
  <img src="https://img.shields.io/badge/RabbitMQ-Management-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white" />
  <img src="https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white" />
  <img src="https://img.shields.io/badge/MediatR-CQRS-blue?style=for-the-badge" />
  <img src="https://img.shields.io/badge/FluentValidation-✓-success?style=for-the-badge" />
</p>

# 🔧 Distributed Configuration Hub

Modern ve dağıtık mikroservis mimarileri için geliştirilmiş, **merkezi konfigurasyon yönetimi** sağlayan bir .NET 10 sistemidir. Proje; konfigürasyon kayıtlarını merkezi bir veritabanında tutmayı, REST API üzerinden yönetmeyi ve servisler yeniden başlatılmadan **canlı (live)** güncelleme sağlamayı amaçlamaktadır.

> **Tek komutla ayağa kalkar:** `docker compose up --build -d`

---

## 📐 Mimari (Clean Architecture + CQRS)

```
┌─────────────────────────────────────────────────────────────────┐
│                    🌐 Admin Panel (SPA)                         │
│                    http://localhost:5173                        │ 
└──────────────────────────┬──────────────────────────────────────┘
                           │ REST API
┌──────────────────────────▼────────────────────────────────────────┐
│               DistributedConfigHub.Api                            │
│  ┌────────────┐  ┌──────────────┐  ┌─────────────────────────┐    │
│  │ Controllers│  │ Action Filter│  │ Exception Handlers      │    │
│  │            │  │ (ApiKey Auth)│  │ (Validation + Global)   │    │
│  └─────┬──────┘  └──────────────┘  └─────────────────────────┘    │
│        │ MediatR (CQRS)                                           │
│  ┌─────▼────────────────────────────────────────────────────┐     │
│  │          DistributedConfigHub.Application                │     │
│  │  Commands: Create, Update, Delete, Rollback              │     │
│  │  Queries:  GetAll, GetById, GetHistory                   │     │
│  │  Behaviors: ValidationBehavior (Pipeline)                │     │
│  └─────┬────────────────────────────────────────────────────┘     │
│        │                                                          │
│  ┌─────▼────────────────────────────────────────────────────────┐ │
│  │          DistributedConfigHub.Infrastructure                 │ │
│  │  EF Core + PostgreSQL │ RabbitMQ Publisher │ AuditInterceptor│ │
│  └─────┬───────────────────────┬────────────────────────────────┘ │
└────────┼───────────────────────┼──────────────────────────────────┘
         │                       │
    ┌────▼─────┐           ┌─────▼──────┐
    │ 🐘 PG    │           │ 🐇 RabbitMQ │
    │  :5432   │           │    :5672   │
    └──────────┘           └─────┬──────┘
                                 │ Signal (Direct Exchange)
                          ┌──────▼───────────────────────────┐
                          │     DemoConsumerApp              │
                          │  ┌─────────────────────────────┐ │
                          │  │ DistributedConfigHub.Client │ │
                          │  │ (SDK — NuGet Package)       │ │
                          │  │ • RabbitMqSubscriber        │ │
                          │  │ • ConcurrentDictionary Cache│ │
                          │  │ • Fallback (JSON file)      │ │
                          │  └─────────────────────────────┘ │
                          └──────────────────────────────────┘
```

### Proje Katmanları

| Katman | Sorumluluk |
|---|---|
| **Domain** | Entity'ler (`ConfigurationRecord`, `AuditLog`), Enum'lar, `BaseAuditableEntity` |
| **Application** | CQRS Command/Query'ler, Validator'lar, Interface'ler, DTO'lar |
| **Infrastructure** | EF Core DbContext, Repository'ler, RabbitMQ Publisher, Audit Interceptor |
| **Api** | Controller'lar, Action Filter (API Key), Exception Handler'lar, Admin Panel |
| **Client (SDK)** | Consumer'lar için NuGet-ready kütüphane: config cache, RabbitMQ subscriber, fallback |
| **DemoConsumerApp** | SDK'yı kullanan örnek servis: canlı güncelleme demo'su |
| **Tests** | Unit + Integration testler (xUnit, Testcontainers) |

---

## 🚀 Temel Tasarım Kararları

### 1. Storage Seçimi — PostgreSQL
- **Veri Bütünlüğü:** `Name + ApplicationName + Environment` üzerinde kompozit Unique Constraint
- **Clean Architecture Uyumu:** EF Core, Infrastructure katmanında Repository Pattern ile izole edilmiştir
- **Audit Trail:** Tüm değişiklikler `AuditLog` tablosuna EF Core `SaveChangesInterceptor` ile otomatik yazılır

### 2. Canlı Güncelleme — RabbitMQ (Event-Driven)
- HTTP Polling yerine **Direct Exchange** ile push-based sinyal sistemi
- Konfigürasyon güncellendiğinde → RabbitMQ'ya sinyal → Consumer SDK otomatik yeniler
- Her ApplicationName kendi `RoutingKey`'i ile izole edilir

### 3. Uygulama İzolasyonu — API Key + RoutingKey
- Her servis sadece kendi `ApplicationName` scope'undaki verilere erişir
- `ApiKeyAuthorizeAttribute`: Query parametresinde `applicationName` varsa tam izolasyon, yoksa header-based doğrulama
- RabbitMQ'da her servis kendi routing key'iyle ayrılır

### 4. Resilience — Fallback Strategy
- SDK her başarılı çekişte `local-fallback-config.json` dosyasına yazar
- API veya RabbitMQ erişilemezse son bilinen iyi değerlerle çalışmaya devam eder
- Hiçbir bağımlılık kesintisi servisi durdurmaz

---

## 🛠️ Kurulum & Çalıştırma

### Gereksinimler
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (v20+ önerilir)
- Git

### Tek Komutla Çalıştırma

```bash
# 1. Projeyi klonlayın
git clone <repo-url>
cd DistributedConfigHub.Net

# 2. Tüm sistemi ayağa kaldırın
docker compose up --build -d

# 3. Container durumlarını kontrol edin
docker compose ps
```

> ⏳ İlk `--build` yaklaşık 30-60 saniye sürer. PostgreSQL ve RabbitMQ healthcheck'leri geçtikten sonra API başlar.

### Temiz Başlangıç (Volume Sıfırlama)

Veritabanını sıfırdan oluşturmak için:

```bash
docker compose down -v        # Volume'ları sil
docker compose up --build -d  # Tekrar oluştur
```

---

## 🌐 Port & Erişim Bilgileri

Sistem ayağa kalktığında aşağıdaki adresler aktif olur:

| Servis | URL | Açıklama |
|---|---|---|
| 🖥️ **Admin Panel** | http://localhost:5173 | Yönetim arayüzü (config CRUD, health, audit) |
| 📡 **Swagger UI** | http://localhost:5173/swagger | API endpoint dokümantasyonu |
| ❤️ **Health Check** | http://localhost:5173/Health | PostgreSQL + RabbitMQ durum bilgisi |
| 🐇 **RabbitMQ Panel** | http://localhost:15672 | Kuyruk yönetimi (`guest` / `guest`) |
| 🐘 **PostgreSQL** | localhost:5432 | Veritabanı (bkz. DBeaver ayarları) |

### 🔑 API Key Bilgileri

| Application | API Key | Kullanım |
|---|---|---|
| `SERVICE-A` | `ibb-demo-secret-key` | Admin Panel varsayılan key |
| `SERVICE-B` | `baska-bir-gizli-anahtar` | İkinci servis testi |

---

## 🗄️ DBeaver ile PostgreSQL'e Bağlanma

Docker'daki PostgreSQL veritabanına erişmek için:

| Alan | Değer |
|---|---|
| **Host** | `localhost` |
| **Port** | `5432` |
| **Database** | `ConfigHubDb` |
| **Username** | `postgres` |
| **Password** | `postgres` |

**DBeaver Adımları:**
1. DBeaver → New Connection → PostgreSQL
2. Yukarıdaki bilgileri girin → Test Connection → Finish
3. `public` şeması altında `Configurations` ve `AuditLogs` tablolarını göreceksiniz

---

## 🐇 RabbitMQ Kuyruğunu Debug Etme

Canlı güncelleme sinyallerini izlemek için:

### Tarayıcıdan (RabbitMQ Management)
1. http://localhost:15672 → `guest` / `guest` ile giriş
2. **Exchanges** sekmesi → `config_updates_direct` exchange'ini bulun
3. **Queues** sekmesi → Consumer uygulaması bağlıysa kuyruğu göreceksiniz
4. Kuyruğa tıklayın → **Get Messages** ile mesajları inceleyin

### Terminalden (Docker exec)
```bash
# Exchange listesini görüntüleme
docker exec confighub-rabbitmq rabbitmqctl list_exchanges

# Kuyruk listesi ve mesaj sayıları
docker exec confighub-rabbitmq rabbitmqctl list_queues name messages consumers

# Binding'leri görüntüleme (hangi routing key hangi kuyruğa bağlı)
docker exec confighub-rabbitmq rabbitmqctl list_bindings
```

---

## 🔩 Dependency Injection — Servis Scope Kararları

### API Tarafı (`Program.cs`)

| Servis | Scope | Neden |
|---|---|---|
| `ConfigDbContext` | **Scoped** | EF Core DbContext per-request yaşam döngüsü gerektirir |
| `IConfigurationRepository` | **Scoped** | DbContext'e bağımlı, aynı scope'ta olmalı |
| `IAuditLogRepository` | **Scoped** | DbContext'e bağımlı |
| `IMessagePublisher` (RabbitMQ) | **Singleton** | Persistent connection/channel reuse, `SemaphoreSlim` ile thread-safe |
| `IAuditContextAccessor` | **Singleton** | `AsyncLocal<T>` ile request bazında veri taşır, state'siz |
| `AuditInterceptor` | **Singleton** | EF Core interceptor'lar singleton olarak çalışır |
| `ApiKeyAuthorizeAttribute` | **Scoped** | `ServiceFilter` olarak her request'te yeni instance |

### SDK (Client) Tarafı (`ServiceCollectionExtensions.cs`)

| Servis | Scope | Neden |
|---|---|---|
| `IConfigSdkService` | **Singleton** | `ConcurrentDictionary` cache tüm yaşam döngüsünce korunmalı |
| `HttpClient` | **IHttpClientFactory** | DNS havuz yönetimi, `SetHandlerLifetime(5dk)` ile socket exhaustion önlenir |
| `RabbitMqSubscriberHostedService` | **Singleton** | `BackgroundService` zaten Singleton'dır |

> **⚠️ Captive Dependency Uyarısı:** `IConfigSdkService` Singleton olmalıdır çünkü `BackgroundService` (Singleton) onu inject eder. Transient/Scoped olursa Captive Dependency anti-pattern'i oluşur.

---

## 📡 API Endpoint'leri

| Method | Endpoint | Açıklama |
|---|---|---|
| `GET` | `/configurations?applicationName=X&environment=Y` | Konfigürasyon listeleme |
| `GET` | `/configurations/{id}` | Tek konfigürasyon getirme |
| `POST` | `/configurations` | Yeni konfigürasyon oluşturma |
| `PUT` | `/configurations/{id}` | Değer güncelleme (+ RabbitMQ sinyal) |
| `DELETE` | `/configurations/{id}` | Konfigürasyonu pasife alma |
| `GET` | `/configurations/{id}/history` | Audit geçmişini görüntüleme |
| `POST` | `/configurations/{id}/rollback/{auditLogId}` | Belirli noktaya geri alma |
| `GET` | `/Health` | PostgreSQL + RabbitMQ durum kontrolü |

### Örnek İstekler

**Konfigürasyon Listeleme:**
```http
GET /configurations?applicationName=SERVICE-A&environment=dev
X-Api-Key: ibb-demo-secret-key
```

**Yeni Konfigürasyon Ekleme:**
```http
POST /configurations
Content-Type: application/json
X-Api-Key: ibb-demo-secret-key

{
  "name": "MaxUsers",
  "type": 1,
  "value": "15000",
  "applicationName": "SERVICE-A",
  "environment": "prod"
}
```

**Değer Güncelleme (Canlı Sinyal):**
```http
PUT /configurations/00000000-0000-0000-0000-000000000001
Content-Type: application/json
X-Api-Key: ibb-demo-secret-key

{
  "id": "00000000-0000-0000-0000-000000000001",
  "value": "https://yeni-odeme.ibb.istanbul"
}
```

---

## 🧪 Testler

```bash
# Tüm testleri çalıştır
dotnet test

# Sonuç: 10/10 Test Passed ✅
```

| Test Kategorisi | Kapsam |
|---|---|
| `CreateConfigurationCommandValidatorTests` | FluentValidation kuralları |
| `ApiKeyAuthorizeAttributeTests` | Tam izolasyon + temel doğrulama modları |

---

## 🎯 Sunum Demo Senaryosu

Aşağıdaki adımları sırasıyla takip ederek sistemi **canlı** olarak gösterebilirsiniz:

### Aşama 1 — Sistemi Ayağa Kaldırma
```bash
docker compose up --build -d
docker compose ps   # Tüm 4 container "Up (healthy)" olmalı
```

### Aşama 2 — Admin Panel'den Genel Bakış
1. http://localhost:5173 adresine gidin
2. **Health kartlarını** gösterin → PostgreSQL ✅, RabbitMQ ✅
3. **Kayıtlı Servisler** kartını gösterin → SERVICE-A (dev, staging, prod)
4. Filtre ile ortamlar arası geçiş yapın → dev / staging / prod

### Aşama 3 — Consumer Loglarını Açın (Yan terminalde)
```bash
docker compose logs -f demo_consumer
```
> Bu terminali açık bırakın, canlı güncellemeyi burada göreceksiniz.

### Aşama 4 — Canlı Güncelleme Demosu
1. Admin Panel'de `IsMaintenanceModeEnabled` (dev) değerini `true` → `false` olarak güncelleyin
2. **Anında** consumer terminalinde log görünecek:
   ```
   🔄 Configuration updated: IsMaintenanceModeEnabled = false
   ```
3. ✨ **Hiçbir restart gerekmedi!**

### Aşama 5 — Audit & Rollback
1. Admin Panel'de bir konfigürasyonun **📜 (history)** butonuna tıklayın
2. Değişiklik geçmişini gösterin (INSERT → UPDATE → ...)
3. **"Bu noktaya geri al"** butonuyla rollback yapın
4. Consumer'da tekrar log görünecek

### Aşama 6 — RabbitMQ Görselleştirme (Opsiyonel)
1. http://localhost:15672 → `guest` / `guest`
2. **Exchanges** → `config_updates_direct` → bindings'i gösterin
3. Admin Panel'den bir güncelleme yapın → **Message Rates** grafiğinde spike görünecek

### Aşama 7 — Swagger API Demosu (Opsiyonel)
1. http://localhost:5173/swagger
2. Doğrudan API üzerinden bir `POST /configurations` yapın
3. Oluşturulan konfigürasyonu Admin Panel'de görün

---

## 📁 Klasör Yapısı

```
DistributedConfigHub.Net/
├── 📦 DistributedConfigHub.Api/           # REST API + Admin Panel
│   ├── Controllers/                       # Configurations, Health
│   ├── Filters/                           # ApiKeyAuthorizeAttribute
│   ├── Infrastructure/ExceptionHandling/  # Validation + Global handlers
│   └── wwwroot/                           # Admin Panel (index.html)
│
├── 📦 DistributedConfigHub.Application/   # CQRS + Business Logic
│   ├── Features/Commands/                 # Create, Update, Delete, Rollback
│   ├── Features/Queries/                  # GetAll, GetById, GetHistory
│   ├── Behaviors/                         # ValidationBehavior (Pipeline)
│   ├── DTOs/                              # ConfigurationDto
│   └── Interfaces/                        # Repository + Service contracts
│
├── 📦 DistributedConfigHub.Domain/        # Entity'ler + Enum'lar
│   ├── Entities/                          # ConfigurationRecord, AuditLog, BaseAuditableEntity
│   └── Enums/                             # ConfigurationType
│
├── 📦 DistributedConfigHub.Infrastructure/# Data Access + Messaging
│   ├── Data/                              # ConfigDbContext, Configurations, Interceptors
│   ├── Repositories/                      # ConfigurationRepository, AuditLogRepository
│   └── Messaging/                         # RabbitMqPublisher (persistent connection)
│
├── 📦 DistributedConfigHub.Client/        # NuGet-ready SDK
│   ├── ConfigSdkService.cs               # ConcurrentDictionary cache (atomic swap)
│   ├── RabbitMqSubscriberHostedService.cs # BackgroundService listener
│   └── ServiceCollectionExtensions.cs     # DI registration
│
├── 📦 DemoConsumerApp/                    # Örnek consumer servis
│
├── 📦 DistributedConfigHub.Tests/         # Unit + Integration tests
│
├── 🐳 docker-compose.yml                 # Tek komutla tüm sistem
└── 📄 README.md
```

---

## ✅ Gereksinim Karşılama Tablosu

| Gereksinim | Durum | Detay |
|---|:---:|---|
| Merkezi konfigürasyon storage | ✅ | PostgreSQL + EF Core |
| REST API (GET/POST/PUT/DELETE) | ✅ | CQRS + MediatR |
| Restart gerektirmeyen güncelleme | ✅ | RabbitMQ Direct Exchange + SDK |
| Uygulama izolasyonu (ApplicationName) | ✅ | API Key filter + RoutingKey |
| Ortam desteği (Environment) | ✅ | dev / staging / prod |
| Tip desteği (String/Int/Double/Boolean) | ✅ | Enum + string storage |
| **Bonus: Rollback** | ✅ | Audit log tabanlı geri alma |
| **Bonus: Audit Log** | ✅ | SaveChangesInterceptor ile otomatik |
| **Bonus: Docker Compose** | ✅ | Healthcheck'li tek komut kurulum |
| **Bonus: Event-Driven (RabbitMQ)** | ✅ | Direct Exchange + routing |
| **Bonus: Yönetim Arayüzü** | ✅ | SPA Admin Panel (vanilla JS) |
| **Bonus: Test** | ✅ | xUnit + Testcontainers |

---

<p align="center">
  <b>Furkan Haydari</b>
</p>
