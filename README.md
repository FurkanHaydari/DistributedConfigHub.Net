# Distributed Configuration Hub

Modern ve dağıtık mikroservis mimarileri için geliştirilmiş, **merkezi konfigurasyon yönetimi** sağlayan bir .NET 10 sistemidir. Proje; konfigürasyon kayıtlarını merkezi bir veritabanında tutmayı, REST API üzerinden yönetmeyi ve bu konfigürasyonları kullanan servislere uygulamaları yeniden başlatmadan (restart) **canlı (live) güncellemeler** sağlamayı amaçlamaktadır.

## 🚀 Temel Tasarım Kararları (Mimari Yaklaşım)

### 1. Neden PostgreSQL Tercih Edildi? (Merkezi Storage)
Proje ödevinin en önemli bileşeni verilerin güvenilir ve tutarlı bir havuzda saklanmasıdır.
- **Veri Bütünlüğü (Unique Constraints):** `Name`, `ApplicationName` ve `Environment` alanlarının birleşimi üzerinde veritabanı seviyesinde kompozit "Unique Constraint" kuralı işletilmiştir. Uygulamalar JSON vb. şemasız NoSQL sistemlerine kıyasla çok daha öngörülebilir ve ilişkisel bir yapıda tutulur.
- **Clean Architecture Uyumu:** Entity Framework Core (MediatR CQRS pattern ile harmanlanarak) bağımsız bir Infrastructure katmanında Repository Pattern sınırları dahilinde uygulanmış, gerektiğinde veri tabanını değiştirmenin önü açık bırakılmıştır.

### 2. Canlı Güncelleme Mekanizması: RabbitMQ & Yerel Fallback
Konfigürasyon sistemi asıl gücünü güncellemeleri anlık olarak dağıtabilmesinden alır.
- **RabbitMQ (Event-Driven Architecture):** 
Geleneksel Polling (örneğin 5 saniyede bir config değişti mi diye API'ye sorma) yöntemi HTTP trafiği açısından maliyetlidir. API tarafında bir ayar güncellendiği an (CQRS Update Command), anında RabbitMQ `Direct Exchange` yapısına bir uyarı sinyali bırakılır.
Consumer tarafı (Custom Client SDK), başlatıldığı an silinmeye hazır (Exclusive, AutoDelete) bir kuyrukla arka planda (`BackgroundService`) burayı dinler ve sadece değişiklik olursa REST API'ye istek atıp belleğini (`ConcurrentDictionary`) tazeler.
- **Dayanıklılık & Fallback (Resilience):**
Ana Config API'sine ya da RabbitMQ'ya ulaşılamaması bir uygulamanın açılışını engellememelidir. Bu nedenle kütüphane her başarılı veri çekişinde verileri o sunucu içindeki ufak bir `local-fallback-config.json` dosyasına yazarak yedekler. Servis API'ye erişemezse bu "son başarılı veriden" ayağa kalkarak çökmenin önüne geçer.

---

## 🛠️ Nasıl Çalıştırılır?

*(Not: İlerleyen safhalarda ana uygulama projeleri de Dockerize edilecektir. Şimdilik RabbitMQ ve PostgreSQL için Docker Compose ayarlanmıştır.)*

1. **Gereksinimleri Başlatın:**
   ```bash
   docker compose up -d
   ```
2. **API Projesini (Hub) Çalıştırın:**
   ```bash
   cd DistributedConfigHub.Api
   dotnet run
   ```
3. **Demo Tüketici (Consumer) Uygulamasını Çalıştırın:**
   ```bash
   cd DemoConsumerApp
   dotnet run
   ```

---

## 📡 API Kullanımı (Örnek İstek ve Yanıtlar)

Sistemdeki veri tipleri `Types` enum karşılığıdır (0: String, 1: Int, 2: Double, 3: Boolean).

### 1. Konfigürasyon Listeleme (GET)
**İstek:**
```http
GET /configurations?applicationName=SERVICE-A&environment=prod
```

**Yanıt:**
```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "SiteName",
    "type": 0,
    "value": "Kadikoy Belediyesi Tech Ekibi",
    "isActive": true,
    "applicationName": "SERVICE-A",
    "environment": "prod"
  }
]
```

### 2. Yeni Konfigürasyon Ekleme (POST)
**İstek:**
```http
POST /configurations
Content-Type: application/json

{
  "name": "MaxUsers",
  "type": 1,
  "value": "15000",
  "applicationName": "SERVICE-A",
  "environment": "prod",
  "isActive": true
}
```

**Yanıt:** `200 OK` (Guid Formatında yeni Id).

### 3. Konfigürasyon Güncelleme ve Canlı Tüketim Sinyali (PUT)
**İstek:**
```http
PUT /configurations/22222222-2222-2222-2222-222222222222
Content-Type: application/json

{
  "value": "20000",
  "isActive": true
}
```

**Yanıt:** `204 No Content` 
*(Arkada gerçekleşen olay: Bu istek başarılı olduğunda API, RabbitMQ'ya `SERVICE-A` key'iyle sinyal yollar. O sırada arka planda açık olan `DemoConsumerApp`, bu sinyali yakalayıp yeni 20.000 değerini anında hafızasına [restart atmadan] kopyalar.)*
