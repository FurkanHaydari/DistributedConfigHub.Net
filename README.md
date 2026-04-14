<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" />
  <img src="https://img.shields.io/badge/PostgreSQL-15-4169E1?style=for-the-badge&logo=postgresql&logoColor=white" />
  <img src="https://img.shields.io/badge/RabbitMQ-Management-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white" />
  <img src="https://img.shields.io/badge/Docker-Compose-2496ED?style=for-the-badge&logo=docker&logoColor=white" />
  <img src="https://img.shields.io/badge/MediatR-CQRS-blue?style=for-the-badge" />
  <img src="https://img.shields.io/badge/FluentValidation-✓-success?style=for-the-badge" />
</p>

# 🚀 DistributedConfigHub

Merkezi, güvenli ve gerçek zamanlı (event-driven) bir konfigürasyon yönetim sistemi. Bu proje, mikroservis mimarilerinde konfigürasyon değişikliklerinin servis kesintisi (restart) gerektirmeden, izole ve güvenli bir şekilde yönetilmesini sağlar.

> **Tek komutla ayağa kalkar:** `docker compose up --build -d`

---

## 🛠️ Temel Özellikler
- [x] **Message Broker:** RabbitMQ Direct Exchange ile düşük ağ maliyeti.
- [x] **Environment Desteği:** Dev, Staging, Prod bazlı kayıt yönetimi.
- [x] **Audit & Rollback:** Tüm değişikliklerin geçmişi ve tek tıkla eski sürüme dönüş.
- [x] **Docker Compose:** Tüm ekosistemi (DB, MQ, API, Consumer) tek komutla ayağa kaldırma.
- [x] **Zero Downtime:** Servisleri yeniden başlatmadan konfigürasyon güncelleme.

---

## 📐 Mimari (Clean Architecture + CQRS)

```
┌────────────────────────────────────────────────────────────────────┐
│                🌐 🌐 🌐 Admin Panel (SPA) 🌐 🌐 🌐                   │
│                      http://localhost:5173                         │
└────────────────────────────┬───────────────────────────────────────┘
                             │ REST API
┌────────────────────────────▼───────────────────────────────────────┐
│                 DistributedConfigHub.Api                           │
│  ┌────────────┐    ┌──────────────┐    ┌─────────────────────────┐ │
│  │ Controllers│    │ Action Filter│    │ Exception Handlers      │ │
│  │            │    │ (ApiKey Auth)│    │ (Validation + Global)   │ │
│  └─────┬──────┘    └──────────────┘    └─────────────────────────┘ │
│        │ MediatR (CQRS)                                            │
│  ┌─────▼──────────────────────────────────────────────────────┐    │
│  │              DistributedConfigHub.Application              │    │
│  │    Commands: Create, Update, Delete, Rollback              │    │
│  │    Queries:  GetAll, GetById, GetHistory                   │    │
│  │    Behaviors: ValidationBehavior (Pipeline)                │    │
│  └─────┬──────────────────────────────────────────────────────┘    │
│        │                                                           │
│  ┌─────▼──────────────────────────────────────────────────────┐    │
│  │             DistributedConfigHub.Infrastructure            │    │
│  │   EF Core + PG │ RabbitMQ Publisher │ AuditInterceptor     │    │
│  └─────┬────────────────────────┬─────────────────────────────┘    │
└────────┼────────────────────────┼──────────────────────────────────┘
         │                        │
    ┌────▼─────┐            ┌─────▼──────┐
    │ 🐘 PG 🐘  │            │ 🐇 RabbitMQ│
    │  :5432   │            │    :5672   │
    └──────────┘            └─────┬──────┘
                                  │ Signal (Direct Exchange)
                           ┌──────▼──────────────────────────┐
                           │      DemoConsumerApp            │
                           │  ┌──────────────────────────┐   │
                           │  │      Client (SDK)        │   │
                           │  │  • RabbitMqSubscriber    │   │
                           │  │  • Hot-Reload Cache      │   │
                           │  │  • Fallback (JSON file)  │   │
                           │  └──────────────────────────┘   │
                           └─────────────────────────────────┘
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

### 📁 Klasör Yapısı

```
DistributedConfigHub.Net/
├── 📦 DistributedConfigHub.Api/           # REST API + Admin Panel
│   ├── Controllers/                       # Configurations, Health
│   ├── Filters/                           # ApiKeyAuthorizeAttribute (X-Api-Key Güvenliği)
│   ├── Infrastructure/ExceptionHandling/  # GlobalExceptionHandler
│   └── wwwroot/                           # Admin Panel (Pure JS/CSS Login & Dashboard)
│
├── 📦 DistributedConfigHub.Application/   # CQRS + İş Mantığı
│   ├── Features/Commands/                 # Create, Update, Delete, Rollback Handlers
│   ├── Features/Queries/                  # GetList, GetById, GetDeleted, GetHistory Handlers
│   ├── Behaviors/                         # Validation & TenantAuthorization (Pipeline)
│   ├── Exceptions/                        # Custom Application Exceptions
│   ├── DTOs/                              # Veri Transfer Nesneleri (ConfigurationDto)
│   └── Interfaces/                        # Repository, Messaging ve Context Sözleşmeleri
│
├── 📦 DistributedConfigHub.Domain/        # Domain Modeli (Zero Dependency)
│   ├── Entities/                          # ConfigurationRecord, AuditLog, BaseAuditableEntity
│   └── Enums/                             # ConfigurationType (Numeric/JSON/Bool/etc)
│
├── 📦 DistributedConfigHub.Infrastructure/# Altyapı ve Veri Erişim
│   ├── Data/                              # ConfigDbContext, FluentAPI Configs, Interceptors
│   ├── Migrations/                        # PostgreSQL Veritabanı Şemaları
│   ├── Repositories/                      # EF Core Repository Gerçekleştirimleri
│   └── Messaging/                         # RabbitMqPublisher (Signaling Logic)
│
├── 📦 DistributedConfigHub.Client/        # Akıllı SDK (Consumer-Side)
│   ├── Interfaces/                        # IConfigSdkService (Sözleşmeler)
│   ├── Models/                            # DTO'lar ve Ayarlar (Item, Options)
│   ├── Services/                          # Uygulama Mantığı (SDK, Subscriber)
│   └── ServiceCollectionExtensions.cs     # DI Kayıt Mekanizması
│
├── 📦 DemoConsumerApp/                    # "Sıfır Kesinti" Uygulama Örneği
│   ├── Controllers/                       # ServiceHealth (DB & SDK Sağlık Kontrolü)
│   ├── Data/                              # ProductDbContext & DatabaseInitializer
│   └── local-fallback-config.json         # API Çökünce Kullanılan Yedek Konfigürasyon
│
├── 📦 DistributedConfigHub.Tests/         # Test Süreçleri
│   ├── DistributedConfigHub.UnitTests     # Moq Tabanlı Mantık Testleri
│   └── DistributedConfigHub.IntegrationTests # Testcontainers (Docker) Bazlı Uçtan Uca Testler
│
├── 📂 docs/                               # Dokümantasyon Varlıkları
│   ├── ConfigHub.postman_collection.json  # Güncel Postman Koleksiyonu
│   └── assets/                            # Demo GIF ve Ekran Görüntüleri
│
├── 🐳 docker-compose.yml                  # Full-stack Orchestration
└── 📄 README.md

```
---

## 🏗️ Mimari Tercihler ve Teknik Gerekçeler

### 1. Storage: Neden PostgreSQL? (Relational vs. NoSQL)
Projelerde konfigürasyon sadece "Key-Value" çifti değildir; sahiplik, ortam ve aktiflik gibi metadata'lar içerir.
- **Veri Bütünlüğü (ACID):** `Name + ApplicationName + Environment` üzerinde **Composite Unique Constraint** kullanılarak, tutarsız veri girişi veritabanı seviyesinde engellenmiştir.
- **Audit Trail (Denetim İzi):** Bonus olan "Rollback" ve "History" özelliklerini sağlamak için ilişkisel bir yapı tercih edilmiştir. `EF Core Interceptor` mimarisi ile her değişiklik otomatik olarak `AuditLog` tablosuna kaydedilir.
- **Kalıcılık (Persistence):** Redis bir cache çözümüdür; konfigürasyon sistemin "beyni" olduğu için kalıcılık ve ilişkisel sorgulama gücü nedeniyle PostgreSQL seçilmiştir.

### 2. Canlı Güncelleme: Neden RabbitMQ? (Event-Driven)
Değişikliklerin anında yansıması için **RabbitMQ (Direct Exchange)** tercih edilmiştir.
- **Guaranteed Delivery:** Redis Pub/Sub'ın aksine, RabbitMQ mesajın ulaştığından emin olur (Queue yapısı).
- **Targeted Routing (RoutingKey):** Her uygulama kendi `ApplicationName` değerini `RoutingKey` olarak kullanır. Bu sayede `SERVICE-A` güncellendiğinde `SERVICE-B` gereksiz network trafiğine ve CPU yüküne maruz kalmaz.
- **Anlık Senkronizasyon:** Değişiklik yapıldığı an consumer SDK'lar haberdar edilir ve belleklerini (cache) 10ms altında bir sürede günceller.

### 3. Uygulama İzolasyonu (Multi-Layered Security)
Sistemde "Sıfır Güven" (Zero Trust) prensibi uygulanmıştır:
- **Identification (Filter):** `ApiKeyAuthorizeAttribute` ile isteği yapanın kimliği API Key üzerinden doğrulanır.
- **Authorization (MediatR Pipeline):** `TenantAuthorizationBehavior` kullanılarak, bir servisin başka bir servisin verisine erişme denemesi daha iş mantığına (Handler) ulaşmadan **"Zero-Database-Trip"** yöntemiyle engellenir.
- **Double-Trip Protection:** ID tabanlı isteklerde (Update/Delete) performans kaybını önlemek için yetki kontrolü Handler seviyesinde yapılarak veritabanına mükerrer gitmekten (Double-Trip Anti-pattern) kaçınılmıştır.

### 4. Resilience (Dayanıklılık) — Last Known Good Configuration (LKGC)
Merkezi sistemlerin "Single Point of Failure" (SPOF) riskine karşı çok katmanlı bir koruma stratejisi uygulanmıştır:
- **3-Kademeli Retry (Yeniden Deneme):** SDK, API'ye ulaşamadığında hemen pes etmez; üstel bekleme (exponential backoff) ile 3 kez tekrar dener.
- **Hafıza Önceliği (Memory-over-Disk):** API hatası veya veritabanı kesintisi anında, eğer SDK hafızasında (Memory) hali hazırda çalışan bir veri varsa **asla silinmez/bozulmaz.** "Eski ama çalışan veri, hiç yoktan iyidir" prensibi (Graceful Stay) uygulanır.
- **Boş Veri Koruması (Empty Response Guard):** API 200 OK dönse bile, veritabanı bağlantısı koptuğu için "boş" bir liste gönderirse; SDK bu veriyi "şüpheli" kabul eder ve mevcut sağlıklı cache'ini korur.
- **Local Snapshot & Graceful Degradation:** Sadece ilk açılışta API kapalıysa diskteki asenkron yedek (`local-fallback-config.json`) devreye girer. Bu sayede merkez çökse bile istemci "son iyi değerlerle" (Last Known Good) ayağa kalkabilir.

> [!NOTE]
> **Cache Hiyerarşisi:** SDK, performans için "RAM > Disk" hiyerarşisini kullanır. API kapalıyken RAM'de veri varsa diske gidilmez (Disk I/O maliyetinden kaçınılır). Disk snapshot'ları sadece hafızanın boş olduğu "Cold Start" senaryoları (Örn: Gece 03:00'te merkez API çöktü, consumer app de o an restart yedi vb.) için sigortadır.

### 5. Yapısal Tasarım: Neden Clean Architecture & CQRS?
Sistemin sürdürülebilirliği, büyüme potansiyeli ve test edilebilirliği için merkezde iş kurallarının olduğu, bağımlılıkların dışarıdan içeriye doğru aktığı Clean Architecture benimsenmiştir.
- **Bağımlılıkların İzolasyonu (Separation of Concerns):** `Domain` ve `Application` katmanları hiçbir altyapı aracına (EF Core, RabbitMQ, HTTP) bağımlı değildir. Bu sayede yarın PostgreSQL yerine başka bir veritabanına geçilmek istense bile iş mantığında (Business Logic) tek satır kod değişmez.
- **Odaklanmış İş Mantığı (CQRS):** `MediatR` kullanılarak okuma (Query) ve yazma (Command) işlemleri birbirinden ayrılmıştır. Bu yaklaşım, sınıfların (Handler) yalnızca tek bir amaca hizmet etmesini (Single Responsibility) sağlar ve spagetti kod oluşumunu engeller.
- **Yüksek Test Edilebilirlik:** İş mantığı veritabanı veya network altyapısına sıkı sıkıya bağlı (tightly-coupled) olmadığı için, dış servisleri `Mock`'layarak Unit Test yazmak son derece hızlı ve güvenilirdir.

### 6. Bellek Yönetimi: Neden ConcurrentDictionary & Volatile? (High-Performance Caching)
SDK içerisinde konfigürasyonlar bellekte tutulurken "Lock-Free Read" (Kilitsiz Okuma) prensibi uygulanmıştır:
- **ConcurrentDictionary:** Aynı anda binlerce thread'in sözlükten veri okumasını, yazma (update) operasyonunu engellemeden sağlar.
- **Volatile & Atomic Swap:** `_cache` referansı `volatile` olarak işaretlenmiştir. Yeni konfigürasyonlar API'den geldiğinde, eski sözlük üzerinde işlem yapmak yerine yeni bir sözlük oluşturulur ve referans **atomik** olarak değiştirilir. Bu sayede okuyucu thread'ler asla "yarım dolmuş" veya "bozuk" bir veri görmez.
- **SemaphoreSlim:** Konfigürasyon yenileme (`Reload`) işlemi sırasında sistemin gereksiz yere birden fazla API isteği atmasını engellemek için kullanılmıştır. Okumalar kilitsizdir, ancak yazmalar (güncelleme anı) kontrollü bir şekilde serialize edilir.


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

## 🛠️ Kurulum & Çalıştırma

### Gereksinimler
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (v20+ önerilir)
- Git

### Tek Komutla Çalıştırma

```bash
# 1. Projeyi klonlayın
git clone https://github.com/FurkanHaydari/DistributedConfigHub.Net
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
| 📡 **Scalar UI (API)** | http://localhost:5173/scalar/v1 | Admin API modern endpoint dokümantasyonu |
| ❤️ **Health Check** | http://localhost:5173/Health | PostgreSQL + RabbitMQ durum bilgisi |
| 🖥️ **Demo Consumer App** | http://localhost:5174 | Consumer App Servisi|
| 📡 **Swagger UI** | http://localhost:5174/swagger | Demo Consumer App API endpoint dokümantasyonu |
| 🐇 **RabbitMQ Panel** | http://localhost:15672 | Kuyruk yönetimi (`guest` / `guest`) |
| 🐘 **PostgreSQL** | localhost:5432 | Veritabanı (`postgres` / `postgres`) |

### 🔑 API Key Bilgileri

| Application | API Key | Kullanım |
|---|---|---|
| `SERVICE-A` | `service-a-secret-key` | Service A Key |
| `SERVICE-B` | `service-b-secret-key` | Service B Key |

---

## 📡 API Endpoint'leri

| Method | Endpoint | Açıklama |
|---|---|---|
| `GET` | `/configurations?applicationName=SERVICE-A` | Konfigürasyon listeleme |
| `GET` | `/configurations/{id}` | Tek konfigürasyon getirme |
| `POST` | `/configurations` | Yeni konfigürasyon oluşturma |
| `GET` | `/configurations/deleted?applicationName=SERVICE-A` | Silinmiş (IsActive = false) konfigürasyonları listeleme |
| `PUT` | `/configurations/{id}` | Değer güncelleme (+ RabbitMQ sinyal) |
| `DELETE` | `/configurations/{id}` | Konfigürasyonu pasife alma |
| `GET` | `/configurations/{id}/history` | Audit geçmişini görüntüleme |
| `POST` | `/configurations/{id}/rollback/{auditLogId}` | Belirli noktaya geri alma |
| `GET` | `/Health` | PostgreSQL + RabbitMQ durum kontrolü |


### 📬 Postman Collection ile Hızlı Test

Projeyi ayağa kaldırdıktan sonra API'yi hızlıca keşfetmek ve test etmek için repository içerisinde hazır bir Postman yapılandırması bulunmaktadır.

**Nasıl Kullanılır?**
1. Postman uygulamasını açın ve sol üstteki **Import** butonuna tıklayın.
2. Proje dizinindeki `docs` klasörü altında yer alan `ConfigHub.postman_collection.json` dosyasını seçip içe aktarın.
3. İçe aktarılan klasördeki tüm isteklerde environment değişkenleri (Örn: `X-Api-Key`, `url`, `configId`, `auditLogId`) ve bazı scriptler hazır olarak gelir.
4. Herhangi bir ayar yapmadan doğrudan **Send** butonuna basarak API'yi test etmeye başlayabilirsiniz!

### Örnek İstekler

**Konfigürasyon Listeleme:**
```http
GET /configurations?applicationName=SERVICE-A
X-Api-Key: service-a-secret-key
```
**Response:**
```json
[
    {
        "id": "00000000-0000-0000-0000-000000000103",
        "name": "MainDatabase",
        "type": "String",
        "value": "Host=postgres;Database=db_beta;Username=postgres;Password=postgres",
        "applicationName": "SERVICE-A",
        "environment": "dev",
        "isActive": true
    },
    {
        "id": "00000000-0000-0000-0000-000000000103",
        "name": "MainDatabase",
        "type": "String",
        "value": "Host=postgres;Database=db_beta;Username=postgres;Password=postgres",
        "applicationName": "SERVICE-A",
        "environment": "staging",
        "isActive": true
    },
    {
        "id": "00000000-0000-0000-0000-000000000103",
        "name": "MainDatabase",
        "type": "String",
        "value": "Host=postgres;Database=db_alpha;Username=postgres;Password=postgres",
        "applicationName": "SERVICE-A",
        "environment": "prod",
        "isActive": true
    }
]
```

**Yeni Konfigürasyon Ekleme:**
```http
POST /configurations
Content-Type: application/json
X-Api-Key: service-a-secret-key

{
  "name": "NewFeatureFlag",
  "type": 0,
  "value": "enabled",
  "applicationName": "SERVICE-A",
  "environment": "prod"
}
```
**Response:**
```json
"74cc9281-f916-4d37-8265-c653d1d626b2"
```


**Değer Güncelleme (Canlı Sinyal):**
```http
PUT /configurations/00000000-0000-0000-0000-000000000001
Content-Type: application/json
X-Api-Key: service-a-secret-key

{
  "id": "00000000-0000-0000-0000-000000000009",
  "value": "true"
}
```
**Response:**
204 No Content

---

## 🧪 Testler

```bash
# Tüm testleri çalıştır
dotnet test

# Sonuç: 17/17 Test Passed ✅
```

| Test Kategorisi / Sınıfı | Kapsam |
|---|---|
| **🧪 Birim Testleri (Unit Tests)** | |
| `ConfigSdkServiceTests` | SDK tarafındaki her türlü ağ kesintisi senaryosu, yerel dosya (fallback) mekanizması ve relative URL çözünürlüğü. |
| `UpdateConfigurationCommandHandlerTests` | MediatR komut işleme, eksik kayıtlarda fırlatılan `KeyNotFoundException` ve asenkron RabbitMQ event tetikleme. |
| `ApiKeyAuthorizeAttributeTests` | `X-Api-Key` doğrulama mantığı, eksik/yanlış anahtarda `401 Unauthorized` yanıtları ve güvenlik filtreleri. |
| **🔗 Entegrasyon Testleri (Integration Tests)** | |
| `LiveUpdateIntegrationTest` | Gerçek bir RabbitMQ ve PostgreSQL (Testcontainers) üzerinde canlı sinyalizasyon ve veri tutarlılık testi. |
| `RollbackIntegrationTest` | Audit logları üzerinden konfigürasyonu milisaniyelik doğrulukla eski bir noktaya geri döndürme akışı. |
| `SecurityAndResilienceIntegrationTest`| Altyapı katmanındaki dayanıklılık senaryoları ve API güvenliğinin uçtan uca doğrulanması. |


---

## 🎯 Demo Senaryosu

Aşağıdaki adımları sırasıyla takip ederek sistemi **canlı** olarak deneyebilirsiniz.

### Ön Hazırlık — Pencere Düzeni

Canlı teste başlamadan önce aşağıdaki pencereleri hazırlayın:

| Pencere | İçerik | Not |
|---|---|---|
| 🖥️ Terminal 1 | Docker komutları | Ana terminal |
| 🖥️ Terminal 2 | `docker compose logs -f demo_consumer` | Consumer logları (canlı) |
| 🖥️ Terminal 3 | `docker compose logs -f config_api` | Config Hub logları (canlı) |
| 🌐 Tarayıcı Tab 1 | `http://localhost:5173` | Admin Panel |
| 🌐 Tarayıcı Tab 2 | `http://localhost:5174` | Consumer Ürün Sayfası |
| 🌐 Tarayıcı Tab 3 | `http://localhost:15672` | RabbitMQ (opsiyonel) |

---

### Aşama 1 — Sistemi Ayağa Kaldırma

**Terminal 1**'de:
```bash
docker compose up --build -d
docker compose ps   # 4 containerın tümü "Up" olmalı
```

**Terminal 2** ve **Terminal 3**'ü açın (yan yana):
```bash
# Terminal 2 — Consumer loglarını canlı izle
docker compose logs -f demo_consumer

# Terminal 3 — Config Hub API loglarını canlı izle
docker compose logs -f config_api
```

> 💡 Terminal 2'de başlangıçta şu loglar görünecek:
> ```
> Database 'db_alpha' created on PostgreSQL server.
> Database 'db_alpha' initialized with seed data.
> Database 'db_beta' created on PostgreSQL server.
> Database 'db_beta' initialized with seed data.
> RabbitMQ Background Subscriber initialized and listening to routing key: SERVICE-A
> ```
> Her iki veritabanı da `DatabaseInitializer` tarafından yaratılır.

---

### Aşama 2 — Admin Panel Genel Bakış

![Admin Panel](./docs/assets/admin-panel.png)

🌐 **Tarayıcı Tab 1** → `http://localhost:5173`

1. **Health kartları** sağlık durumunu gösterir → PostgreSQL ✅, RabbitMQ ✅
2. **Kayıtlı Servisler** kartı kayıtlı servisleri gösterir → SERVICE-A (dev, staging, prod)
3. Filtre ile ortamlar arası geçiş yapabilirsiniz → dev / staging / prod
4. Konfigürasyon listesinde `MainDatabase`, `IsMaintenanceModeEnabled`, `ExternalPaymentApiUrl` gibi kayıtları inceleyebilirsiniz.

---

### Aşama 3 — Consumer Ürün Sayfası

![Consumer Product Page](./docs/assets/consumer-product-page.png)

🌐 **Tarayıcı Tab 2** → `http://localhost:5174`

1. Sayfa otomatik olarak ürünleri yükleyecek (5 adet Alpha ürünü)
2. **Sağ alt köşedeki "Sistem Bilgisi" paneli** görülebilir:
   - **Uptime** → Uygulama ne kadar süredir ayakta (bu değer artmaya devam edecek!)
   - **Environment** → PROD (`ASPNETCORE_ENVIRONMENT`'tan otomatik)
   - **Aktif DB** → db_alpha
   - **Bakım Modu** → Kapalı
3. Header'da **ENV: PROD** ve **Aktif DB: db_alpha** badge'leri görünür

> 💡 Sayfa her 3 saniyede bir otomatik güncellenir — manuel yenileme gerekmez.

---

### Aşama 4 — Bakım Modu Demosu (Canlı Config Değişikliği)

![Maintenance Mode](./docs/assets/maintenance-mode.gif)

🌐 **Tarayıcı Tab 1** → Admin Panel

1. `IsMaintenanceModeEnabled` (dev) değerini `false` → `true` olarak güncelleyin ve kaydedin

🌐 **Tarayıcı Tab 2** → Consumer Sayfası (Her 3 saniyede bir otomatik güncellenir)

2. **Anında** bakım modu overlay'i görünecek:
   - 🔧 Bakım modu özel penceresi
   - **Ortam: DEV · DB: db_alpha** bilgisi

3. **Sistem Bilgisi paneli** bakım modunda da görünmeye devam eder:
   - Bakım Modu → **AKTİF** (sarı)
   - Uptime → Hâlâ artıyor (uygulama restart olmuyor)

🖥️ **Terminal 2** → Consumer loglarında:
```bash
confighub-demo  | info: DistributedConfigHub.Client.RabbitMqSubscriberHostedService[0]
confighub-demo  |       Received config update for current environment 'prod': SERVICE-A|prod. Reloading configs...
confighub-demo  | info: System.Net.Http.HttpClient.ConfigHub.LogicalHandler[100]
confighub-demo  |       Start processing HTTP request GET http://config_api:8080/Configurations?*
confighub-demo  | info: System.Net.Http.HttpClient.ConfigHub.ClientHandler[100]
confighub-demo  |       Sending HTTP request GET http://config_api:8080/Configurations?*
confighub-demo  | info: System.Net.Http.HttpClient.ConfigHub.ClientHandler[101]
confighub-demo  |       Received HTTP response headers after 6.523ms - 200
confighub-demo  | info: System.Net.Http.HttpClient.ConfigHub.LogicalHandler[101]
confighub-demo  |       End processing HTTP request after 6.7487ms - 200
confighub-demo  | info: DistributedConfigHub.Client.ConfigSdkService[0]
confighub-demo  |       Configurations successfully loaded from API and cached to local-fallback-config.json.
confighub-demo  | info: Program[0]
confighub-demo  |         ↳ MainDatabase = Host=postgres;Database=db_alpha;Username=postgres;Password=postgres
confighub-demo  | info: Program[0]
confighub-demo  |         ↳ IsMaintenanceModeEnabled = true
confighub-demo  | info: Program[0]
confighub-demo  |         ↳ MaxConcurrentTransactions = 50000
confighub-demo  | info: Program[0]
confighub-demo  |         ↳ ExternalPaymentApiUrl = https://pay.enterprise.com
```

4. Admin Panel'e dönüp `IsMaintenanceModeEnabled` → `false` yapın
5. Consumer sayfası **anında** ürün kataloğuna geri döner ✨

---

### Aşama 5 — Database Hot-Swap (Sihir Anı ✨)

![Database Hot-Swap](./docs/assets/live-db-change.gif)

🌐 **Tarayıcı Tab 2** → Consumer sayfasında ürünleri ve sağ alttaki panele bakın:
- 5 adet **Alpha** ürünü (Alpha Laptop Pro, Alpha Phone X, ...)
- Aktif DB: **db_alpha**
- Uptime değerine dikkat edin

🌐 **Tarayıcı Tab 1** → Admin Panel'e dönün:

1. `MainDatabase` (dev) konfigürasyonunu bulun
2. Değerini değiştirin:
   ```
   Eski: Host=postgres;Database=db_alpha;Username=postgres;Password=postgres
   Yeni: Host=postgres;Database=db_beta;Username=postgres;Password=postgres
   ```
3. Kaydedin

🌐 **Tarayıcı Tab 2** → Consumer sayfasına dönün:

4. Artık **4 adet Beta ürünü** görünüyor! (Beta Smartwatch Ultra, Beta Tablet Air, ...)
5. Sağ alt panelde:
   - Aktif DB → **db_beta** 🟣
   - Uptime → **Hâlâ artıyor!** Uygulama kapanmadı 🟢
   - Bakım Modu → Kapalı

🖥️ **Terminal 2** loglarında:
```bash
confighub-demo  | info: DistributedConfigHub.Client.ConfigSdkService[0]
confighub-demo  |       Configurations successfully loaded from API and cached to local-fallback-config.json.
confighub-demo  | info: Program[0]
confighub-demo  |         ↳ MainDatabase = Host=postgres;Database=db_beta;Username=postgres;Password=postgres
```

🖥️ **Terminal 3** loglarında:
```bash
confighub-api  |       Published config update to exchange 'config_updates_direct' for application 'SERVICE-A' on environment 'prod'
```

---

### Aşama 6 — Audit & Rollback

![Audit & Rollback](./docs/assets/rollback.gif)

🌐 **Tarayıcı Tab 1** → Admin Panel

1. `MainDatabase` konfigürasyonunun **📜 (history)** butonuna tıklayın
2. Değişiklik geçmişi görülecek (INSERT → UPDATE → ...)
3. İlk değere (db_alpha) **"Bu noktaya geri al"** butonuyla rollback yapın
4. Consumer sayfasında tekrar **Alpha ürünleri** görünecek
5. Uptime **hâlâ** devam ediyor 🟢

---

### Aşama 7 — RabbitMQ Görselleştirme (Opsiyonel)

🌐 **Tarayıcı Tab 3** → `http://localhost:15672` → Giriş: `guest` / `guest`

1. **Exchange Yapısını İnceleme:**
   - **Exchanges** sekmesine gidin ve `config_updates_direct` exchange'ine tıklayın.
   - **Bindings** kısmında, o an bağlı olan `demo_consumer` kuyruğunu ve `SERVICE-A` routing key'i göreceksiniz.

2. **Yeni Kuyruk ile "Sniffing" (Güvenlik Testi):**
   - Gerçek consumer'lar `Exclusive` (bağlantı kopunca silinen) kuyruklar kullandığı için onları dışarıdan izlemek zordur. Bu yüzden **Queues and Streams** sekmesine gidin.
   - **Add a new queue** bölümünden `debug-sniffer` isimli kalıcı bir kuyruk oluşturun.
   - Bu yeni kuyruğun içine girin ve **Bindings** kısmından onu `config_updates_direct` exchange'ine, `SERVICE-A` routing key'i ile bind edin.

3. **Mesajı Canlı Yakalama:**
   - Admin Panel'den bir ayarı güncelleyin.
   - RabbitMQ'da `debug-sniffer` kuyruğuna mesajın düştüğünü (`Ready: 1`) göreceksiniz.
   - Kuyruk detayında **Get Messages** butonuna basın. 
   - ✨ Sinyalin payloadı (Örn: `{"SERVICE-A|prod"}`) ham veri olarak görünecek.

---

### Aşama 8 — Güvenlik Demosu (Opsiyonel)

Yanlış API anahtarı ile istek gönderin:
```bash
curl -H "X-Api-Key: yanlis-anahtar" "http://localhost:5173/configurations?applicationName=SERVICE-A"
# → 401 Unauthorized
```

Doğru API anahtarı ile:
```bash
curl -H "X-Api-Key: service-a-secret-key" "http://localhost:5173/configurations?applicationName=SERVICE-A"
# → 200 OK + JSON verileri
```

---

### 📋 Hızlı Referans — Docker Log Komutları

<details>
<summary><b>Log Komutlarını Görmek İçin Tıklayın</b></summary>
<br>

```bash
# Tüm container logları (canlı)
docker compose logs -f

# Sadece Consumer logları
docker compose logs -f demo_consumer

# Sadece Config Hub API logları
docker compose logs -f config_api

# Sadece PostgreSQL logları
docker compose logs -f postgres

# Sadece RabbitMQ logları
docker compose logs -f rabbitmq

# Son 50 satır + canlı takip
docker compose logs -f --tail=50 demo_consumer

# Container durumları
docker compose ps
```
</details>

---

<p align="center">
  💻 <b>Furkan Haydari</b> tarafından geliştirildi. <br><br>
  <a href="https://github.com/FurkanHaydari"><img src="https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white" alt="GitHub"></a>
  <a href="https://www.linkedin.com/in/furkanhaydari"><img src="https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white" alt="LinkedIn"></a>
</p>