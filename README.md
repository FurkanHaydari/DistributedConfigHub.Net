# Distributed Configuration Hub

Modern ve dağıtık mikroservis mimarileri için geliştirilmiş, **merkezi konfigurasyon yönetimi** sağlayan bir .NET 10 sistemidir. Proje; konfigürasyon kayıtlarını merkezi bir veritabanında tutmayı, REST API üzerinden yönetmeyi ve bu konfigürasyonları kullanan servislere uygulamaları yeniden başlatmadan (restart) **canlı (live) güncellemeler** sağlamayı amaçlamaktadır.

## 🚀 Temel Tasarım Kararları ve Gereksinimlerin Karşılanması

### 1. Storage Seçimi (Neden PostgreSQL?)
Proje ödevinin en önemli bileşeni verilerin güvenilir ve tutarlı bir havuzda saklanmasıdır.
- **Veri Bütünlüğü (Unique Constraints):** `Name`, `ApplicationName` ve `Environment` alanlarının birleşimi üzerinde veritabanı seviyesinde kompozit "Unique Constraint" kuralı işletilmiştir. Bu nedenle şemasız NoSQL sistemleri (MongoDB vb.) veya in-memory bir geçici heves olan basit Redis kurgusu yerine, asıl gerçeği tutabilecek güvenilir bir ilişkisel veri tabanı yapısı (PostgreSQL) tercih edilmiştir.
- **Clean Architecture Uyumu:** Entity Framework Core bağımsız bir Infrastructure katmanında Repository Pattern sınırları dahilinde uygulanmış, gerektiğinde veri tabanını (örneğin MSSQL'e) saniyeler içinde değiştirmenin önü açık bırakılmıştır.

### 2. Canlı Güncelleme Mekanizması (Restart Gerektirmeyen Güncelleme)
Konfigürasyon sistemi asıl gücünü güncellemeleri anlık olarak dağıtabilmesinden alır.
- **RabbitMQ (Event-Driven Architecture):** Geleneksel HTTP Polling (sürekli değişti mi diye sorma) yöntemi trafik açısından masraflıdır. API tarafında bir ayar güncellendiği an (CQRS Update Command), anında RabbitMQ `Direct Exchange` yapısına bir uyarı sinyali bırakılır.
- **Dinamik Bellek:** Consumer projelerin içindeki Custom Client SDK, projeler başlatıldığı an arka planda burayı dinler (`BackgroundService`), sadece bir sinyal gelirse API'ye istek atıp kendi RAM (bellek/cache) durumunu günceller. Dolayısıyla hiçbir uygulama restart gerektirmeden en güncel değeri yaşam döngüsü bitmeden alır.

### 3. Uygulama İzolasyonu
Her servis kesinlikle yalnızca kendine tanımlanmış uzaya erişebilmelidir.
- Client SDK başlatılırken `ApplicationName` ("SERVICE-A") ve `Environment` belirtmek zorundadır. Bu değişkenler hem HTTP REST API'ye yapılan konfigurasyon çekme isteklerinde (`GET ?applicationName=SERVICE-A`) filtre olarak kullanılır hem de RabbitMQ kuyruğuna dinleyici eklenirken `RoutingKey` olarak sadece belirtilen o uygulamanın sinyali ("SERVICE-A") ile eşleştirilerek diğer servisin verileriyle yalıtılır.

### 4. Bağlantı Problemi (Maksimum Dayanıklılık & Resilience)
Ana Config API'sine ya da RabbitMQ'ya ulaşılamaması bir uygulamanın ayağa kalkmasını engellememelidir.
- Bu senaryo için SDK içerisine bir **Fallback Strategy** (Geri Çekilme Senaryosu) uygulanmıştır. Kütüphane her başarılı konfigürasyon çekişinde, değerleri servisin kendi diski içerisinde yer alan `local-fallback-config.json` isminde minik bir yedek dosyaya yazar. Servis API'ye erişemez veya Timeout'a düşerse (HTTP 500/503), bu dosyadaki "en son bilinen iyi değerlerden" programını çalıştırmaya hiçbir şey olmamışçasına devam eder.
- Bu esneklik testlerle de kanıtlanmıştır.

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
