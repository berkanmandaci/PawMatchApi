# 2024-06-15 Geliştirme Günlüğü

- JWT authentication gerçek anahtar ile uygulandı, demo için 32+ karakterlik key kullanıldı
- Swagger/OpenAPI açıklamaları login, register, profil ve matches endpointlerine eklendi
- Dummy user, pet ve photo verileri için seed işlemi eklendi (development ortamında otomatik)
- /api/v1/matches/discover ve /api/v1/matches (like/pass) endpointleri eklendi, JWT ile korumalı
- Discover endpointi artık veritabanındaki tüm kullanıcıları (giriş yapan hariç), pet ve fotoğraflarıyla döndürüyor
- Dockerfile ve docker-compose ile wait-for-it.sh entegrasyonu yapıldı, migration/seed işlemleri sorunsuz başlatılıyor
- JWT üretimi ve doğrulaması için gerekli NuGet paketleri eklendi
- Hatalı token, kısa anahtar, vs. gibi build ve runtime hataları giderildi
- Kodda eksik kalan mock/boş metotlar gerçek veritabanı sorguları ile güncellendi

# 2024-06-14 Geliştirme Günlüğü

- Proje iskeleti oluşturuldu (Api, Application, Domain, Infrastructure, Tests katmanları)
- Dockerfile ve docker-compose.yml hazırlandı, PostgreSQL entegrasyonu sağlandı
- Gerekli NuGet paketleri ve projeler arası referanslar eklendi
- User, Pet, Photo entity'leri ve DbContext yazıldı
- Kullanıcı kayıt ve login için DTO, servis, repository, controller ve JWT altyapısı kuruldu
- EF Core migration ve database update işlemleri için rehberlik edildi
- Build ve migration hataları giderildi (referans, namespace, connection string, tablo eksikliği)
- Proje başarıyla ayağa kaldırıldı ve temel API endpointleri çalışır hale getirildi

# 2024-06-15 Geliştirme Günlüğü (Devamı)

- JWT'den userId çekme işlemleri için merkezi `BaseController` oluşturuldu.
- `UsersController`, `PhotosController`, `MatchesController` ve `WeatherForecastController` artık `BaseController`'dan miras alıyor ve `GetUserIdFromClaims()` fonksiyonunu kullanıyor.
- `GetUserIdFromClaims()` fonksiyonundaki hatalı claim adı (`"nameid"` yerine `ClaimTypes.NameIdentifier`) düzeltildi, böylece kullanıcı kimlik doğrulama sorunları giderildi.
- Fotoğraf yükleme endpointleri ve akışları tamamen test edildi ve başarılı şekilde çalışıyor (örn. `POST /api/v1/photos/user`).
- `api/photofeature.md` ve `app/photofeature.md` dosyaları güncel backend ve frontend implementasyonlarına uygun şekilde revize edildi, hatalı endpoint isimleri ve akışlar düzeltildi.
- `docs/PawMatch Backend Guidelines.markdown` dokümanı güncel backend mimarisi ve endpoint detaylarına göre revize edildi, eski ve hatalı bilgiler temizlendi.
- `IDiscoverService` ve `DiscoverService` eklendi, DiscoverService şimdilik basit bir keşif listesi sağlıyor.
- Fotoğraf görüntüleme endpointindeki erişim kontrolü güncellendi: kullanıcı kendi fotoğrafını, kendi evcil hayvanının fotoğrafını veya keşfedilenler listesindeki bir kullanıcının fotoğrafını görebiliyor.

# 2024-06-16 Geliştirme Günlüğü

- Kullanıcı kayıt, giriş ve profil güncelleme API uç noktaları için entegrasyon testleri eklendi.
- Test ortamında `AppDbContext` ve `Program` sınıfı ile ilgili erişim ve uyumluluk sorunları giderildi.
- Kullanıcının kendi hesabını silebilmesi için `DELETE /api/v1/users/me` uç noktası ve `UserService` içinde `DeleteUserAsync` metodu uygulandı.
- Hesap silindiğinde kullanıcıya ve evcil hayvanlara ait fotoğrafların da silinmesi için `IPhotoService`'e ilgili metotlar eklendi ve `UserService` içinde çağrıldı.
- Google Drive entegrasyonu testleri için `IStorageProvider` arayüzü mock'lanarak test ortamı bağımlılıkları giderildi.
- Fotoğraf yükleme testlerinde `StreamContent` için `ContentType` başlığı doğru şekilde ayarlandı.
- Fotoğraf testlerinde devam eden `FileName` boş ve `404 Not Found` hataları üzerinde çalışılmaya devam edildi.

# 2024-06-18 Geliştirme Günlüğü

- Eşleşme Fonksiyonu (`LikeOrPassAsync`) ve Temel Döngüsü Uygulandı:
  - `UserSwipe` modeli oluşturuldu.
  - `AppDbContext`'e `DbSet<UserSwipe>` eklendi ve ilişkiler yapılandırıldı.
  - `IUserSwipeRepository` ve `UserSwipeRepository` uygulandı.
  - `IUserSwipeRepository` bağımlılık enjeksiyonu için kaydedildi.
  - `MatchService.LikeOrPassAsync` metodu, girdi doğrulama, `UserSwipe` kaydı oluşturma, karşılıklı beğenileri kontrol ederek eşleşme onayı ve "pas geçme" eylemleri için uygulandı.
  - `appsettings.json` dosyasına `SwipeExclusionDurationDays` ayarı eklendi.

- Keşfetme Servisi Geliştirmeleri:
  - `IDiscoverService` arayüzü `GetDiscoveredUserIdsAsync` metodu için `maxDistanceKm` ve `preferredPetType` parametreleri içerecek şekilde güncellendi.
  - `DiscoverService.GetDiscoveredUserIdsAsync` metodu, yapılandırılabilir bir süre içinde zaten kaydırılmış kullanıcıları filtreleyecek şekilde güncellendi.

- Testler ve Hata Düzeltmeleri:
  - `MatchesController` ve `DiscoverService` için entegrasyon testleri oluşturuldu.
  - Test sınıflarında `IClassFixture` kullanımı düzeltildi (`CustomWebApplicationFactory<Program>` olarak).
  - Testler için `CustomWebApplicationFactory` içinde `ASPNETCORE_ENVIRONMENT` ortam değişkeni "Production" olarak ayarlandı.
  - `DiscoverServiceTests.cs` dosyasındaki `IDiscoverService` bulunamadı hatası giderildi.
  - `DiscoverServiceTests.cs` test izolasyonunu iyileştirmek için doğrudan in-memory veritabanına `UserSwipe` varlıkları eklendi.
  - `DiscoverService.GetDiscoveredUserIdsAsync` metodundan `.Take(5)` çağrısı kaldırıldı, böylece servis tüm keşfedilebilir kullanıcıları döndürdü.
  - Tüm testler başarıyla geçti.

- Frontend Ekibi İçin Dokümantasyon:
  - `app/backendTalimat.md` dosyası oluşturularak `POST /api/v1/matches` ve `GET /api/v1/matches/discover` API endpointleri detaylı bir şekilde belgelendi.

- Yapılacaklar Listesi Güncellemesi:
  - `api/matchandmessagesfeaturetodo.md` dosyasındaki ilgili görevler tamamlandı olarak işaretlendi.

# 2024-06-19 Geliştirme Günlüğü

- Gerçek Zamanlı İletişim (SignalR) Altyapısı (Backend) Uygulandı:
  - `PawMatch.Api` projesine `Microsoft.AspNetCore.SignalR` NuGet paketi eklendi.
  - `api/PawMatch.Api/Program.cs` dosyasında SignalR servisleri kaydedildi ve Hub endpoint'i (`/chatHub`) yapılandırıldı.
  - `api/PawMatch.Api/Hubs/ChatHub.cs` adında yeni bir SignalR Hub sınıfı oluşturuldu. Bu Hub, `OnConnectedAsync()` ve `OnDisconnectedAsync()` metotlarını içeriyor ve kullanıcı ID'lerini bağlantı ID'leriyle ilişkilendiriyor.

- Eşleşme Bildirimleri Entegrasyonu:
  - `MatchService.cs` içine `IHubContext<ChatHub>` bağımlılığı enjekte edildi.
  - `MatchService.cs` içindeki `LikeOrPassAsync` metodu, karşılıklı bir eşleşme olduğunda eşleşen her iki kullanıcıya da `ChatHub` üzerinden anlık bildirim (`ReceiveMatchNotification`) gönderecek şekilde güncellendi.

- Mesajlaşma Entegrasyonu:
  - `PawMatch.Domain/Message.cs` modeli oluşturuldu (Id, SenderId, RecipientId, Content, Timestamp, IsRead).
  - `api/PawMatch.Infrastructure/AppDbContext.cs` içine `DbSet<Message>` eklendi.
  - `api/PawMatch.Infrastructure/Interfaces/IMessageRepository.cs` arayüzü ve `api/PawMatch.Infrastructure/Repositories/MessageRepository.cs` uygulaması oluşturuldu.
  - `IMessageRepository` ve `IMessageService` bağımlılık enjeksiyonu için `api/PawMatch.Api/Program.cs`'e kaydedildi.
  - `api/PawMatch.Application/Interfaces/IMessageService.cs` arayüzü ve `api/PawMatch.Application/Services/MessageService.cs` uygulaması oluşturuldu. `MessageService` içine `IHubContext<ChatHub>` ve `IMessageRepository` enjekte edildi.
  - `MessageService.cs` içindeki `SendMessageAsync` metodu, mesaj veritabanına kaydedildikten sonra alıcıya SignalR üzerinden anlık mesaj (`ReceiveMessage`) gönderecek şekilde uygulandı.

- Kimlik Doğrulama ve Yetkilendirme (SignalR):
  - `api/PawMatch.Api/Hubs/ChatHub.cs` sınıfına `[Authorize]` niteliği eklendi, böylece Hub JWT tabanlı kimlik doğrulama ile güvence altına alındı.
  - Hub metotları içinde `Context.User.Identity.Name` veya `Context.UserIdentifier` ile kullanıcı ID'sine erişim sağlandı.

- Yapılacaklar Listesi Güncellemesi:
  - `docs/RealtimeConnectiontodo.md` dosyasındaki ilgili görevler tamamlandı olarak işaretlendi.

- Mimari İyileştirmeler ve Bağımlılık Giderme (Katmanlı Mimari):
  - `PawMatch.Application.Interfaces` içinde `IRealtimeNotificationService` adında yeni bir arayüz tanımlandı. Bu arayüz, gerçek zamanlı bildirim gönderme ihtiyacını soyutluyor.
  - `PawMatch.Api/Services` dizininde `SignalRNotificationService` adında bir sınıf oluşturuldu. Bu sınıf `IRealtimeNotificationService` arayüzünü implement ediyor ve `IHubContext<ChatHub>`'ı kullanarak SignalR'a özel çağrıları yönetiyor. Bu sayede SignalR bağımlılığı API katmanında kaldı.
  - `MatchService.cs` ve `MessageService.cs` servislerinin constructor'ları `IHubContext<ChatHub>` yerine yeni `IRealtimeNotificationService` arayüzünü enjekte edecek şekilde değiştirildi. İlgili metodlarda da bu yeni arayüz kullanılarak bildirim gönderme işlemleri gerçekleştirildi.
  - `Program.cs`'te `IRealtimeNotificationService`'in `SignalRNotificationService` implementasyonuyla birlikte bağımlılık enjeksiyonu için kaydedildi.
  - Uygulama servislerinden gereksiz `Microsoft.AspNetCore.SignalR` `using` ifadeleri kaldırıldı.

- Mesajlaşma Servisi (MessageService) Hata Yönetimi ve Testler:
  - Artık geçersiz alıcıya veya gönderen kullanıcıya mesaj gönderilmek istendiğinde `MessageService.SendMessageAsync` metodu exception (ArgumentException) fırlatıyor.
  - Bu davranış için birim ve entegrasyon testleri yazıldı; geçersiz alıcı veya gönderen durumunda hata fırlatıldığı doğrulandı.
  - Tüm testler context7 SignalR best practice'lerine uygun şekilde başarıyla geçti.
  - Frontend dokümantasyonuna (app/backendTalimat.md) bu hata yönetimiyle ilgili notlar ve kullanıcıya uygun uyarı gösterilmesi gerektiği eklendi.

# 2024-06-20 Geliştirme Günlüğü

- IMatchRepository arayüzü ve MatchRepository implementasyonu eklendi. MatchService artık doğrudan DbContext yerine repository kullanıyor.
- Program.cs'de IMatchRepository için DI kaydı yapıldı.
- Mesajlaşma ve eşleşme endpointleri için repository altyapısı tamamlandı.
