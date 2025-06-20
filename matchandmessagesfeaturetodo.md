# PawMatch Eşleşme ve Mesajlaşma Özelliği - Yapılacaklar Listesi

Bu belge, PawMatch uygulamasında eşleşme ve mesajlaşma özelliklerinin implementasyon adımlarını, belirlenen iş akışına göre sıralı bir şekilde listeler. Her ana görevin ardından, ilgili test yazma ve çalıştırma adımları belirtilmiştir.

## 1. Match Fonksiyonu Core Loop Çalışacak (`LikeOrPassAsync` Implementasyonu)

### Görevler:

*   **Veri Modeli ve Veritabanı:**
    *   [x] `PawMatch.Domain/UserSwipe.cs` modelini oluştur. (Id, SwiperId, SwipedUserId, IsLiked, SwipeDate)
    *   [x] `AppDbContext.cs` içine `DbSet<UserSwipe> UserSwipes { get; set; }` ekle.
    *   [x] Yeni `UserSwipe` tablosu için Entity Framework Core migrasyonu oluştur.
    *   [x] `IUserSwipeRepository.cs` arayüzünü oluştur. (AddAsync, GetBySwiperAndSwipedUserAsync gibi metodlar)
    *   [x] `UserSwipeRepository.cs` implementasyonunu oluştur.
    *   [x] `Program.cs`'te `IUserSwipeRepository`'yi Dependency Injection'a ekle.

*   **`MatchService.cs` Geliştirmesi:**
    *   [x] `MatchService` içine `IUserSwipeRepository` bağımlılığını enjekte et.
    *   [x] `LikeOrPassAsync` metodunu güncelle:
        *   [x] Girdi doğrulamasını yap (istek yapan kullanıcının ID'si ile `User1Id`'nin eşleştiğini kontrol et).
        *   [x] `UserSwipe` kaydını oluştur ve kaydet (`SwiperId`, `SwipedUserId`, `IsLiked`, `SwipeDate`).
        *   [x] `IsLiked = true` ise, karşılıklı beğeni olup olmadığını kontrol et (`UserSwipe` tablosunda).
        *   [x] Eğer karşılıklı beğeni varsa:
            *   [x] `Match` tablosunda yeni bir eşleşme oluştur veya mevcut `Confirmed = false` eşleşmeyi `Confirmed = true` olarak güncelle.
            *   [x] `MatchResultDto` içinde `Confirmed = true` ve `MatchId` döndür.
        *   [x] Eğer karşılıklı beğeni yoksa, `MatchResultDto` içinde `Confirmed = false` döndür.
        *   [x] `IsLiked = false` ise, sadece `UserSwipe` kaydını oluştur.
        *   [x] Eğer daha önce onaylanmış bir eşleşme varsa, bu eşleşmenin durumu `Confirmed = false` olarak güncellenmeli veya silinmelidir (genellikle durum güncellemek geçmişi korumak adına tercih edilir).
        *   [x] `MatchResultDto` içinde `Confirmed = false` döndür.
    *   [x] Veritabanı işlemlerini transaction içerisinde yap.

*   **`DiscoverService.cs` Geliştirmesi (Tekrar Karşılaşma Engellemesi):**
    *   [x] `DiscoverService` içine `IUserSwipeRepository` bağımlılığını enjekte et.
    *   [x] `DiscoverUsersAsync` metodunu güncelle:
        *   [x] `currentUserId`'nin belirli bir süre içinde (örn. 30 gün) zaten beğenmiş veya geçmiş olduğu kullanıcıları `UserSwipe` tablosundan sorgulayarak hariç tut. Bu süre `appsettings.json`'dan okunacak.
        *   [ ] Konum tabanlı filtrelemeyi (`maxDistanceKm`) ve pet türü filtrelemesini gerçek algoritma ile uygula.

*   **Yapılandırma:**
    *   [x] `appsettings.json` içine `SwipeExclusionDurationDays` ayarını ekle (örn. `30`).

*   **Repository Katmanı:**
    *   [x] IMatchRepository arayüzünü oluştur.
    *   [x] MatchRepository implementasyonunu oluştur.
    *   [x] Program.cs'de IMatchRepository için DI kaydı yap.
    *   [x] MatchService, eşleşme işlemlerinde MatchRepository kullanacak şekilde güncellendi.

### Testler (Match Fonksiyonu):

*   [x] `PawMatch.Tests/MatchesControllerTests.cs` içinde `LikeOrPass` endpoint'i için test senaryoları yaz:
    *   [x] Başarılı beğenme ve karşılıklı eşleşme testi (hem beğeni hem de onaylanmış eşleşme).
    *   [x] Başarılı beğenme ancak eşleşme olmaması testi.
    *   [x] Başarılı geçme testi.
    *   [x] Geçersiz kullanıcı ID'leri ile deneme (yetkilendirme kontrolü).
*   [x] `PawMatch.Tests/DiscoverServiceTests.cs` (veya mevcut ilgili test dosyasına) keşif algoritmasının swiped kullanıcıları hariç tuttuğunu doğrulayan testler ekle.
*   [x] Yazılan testleri çalıştır ve doğrula.

## 2. Gerçek Zamanlı Mesajlaşma

### Görevler:

*   [X] Gerçek zamanlı mesajlaşma için teknoloji seçimi yap (SignalR seçildi).
*   [X] Backend tarafında SignalR Hub kur (SignalR Hub ve endpoint'i oluşturuldu, bağımlılık enjeksiyonu için `IRealtimeNotificationService` arayüzü ve `SignalRNotificationService` implementasyonu kullanıldı).
*   [X] `Message` modeli ve veritabanı yapısını gerçek zamanlı iletişim için optimize et (Message modeli, DbSet, IMessageRepository ve MessageRepository oluşturuldu).
*   [X] Mesajlaşma endpoint'lerini gerçek zamanlı iletişimi kullanacak şekilde güncelle (örn. `POST /api/v1/messages` için mesajı sadece veritabanına kaydetmek yerine, aynı zamanda `IRealtimeNotificationService` aracılığıyla eşleşen kullanıcının bağlı istemcisine anlık olarak gönder).

### Testler (Gerçek Zamanlı Mesajlaşma) - Güncelleme:
* [x] Gerçek zamanlı mesajlaşma için entegrasyon testleri yaz (örneğin, iki kullanıcının bağlanıp mesaj gönderip alabilmesi).
* [x] Yazılan testleri çalıştır ve doğrula.
* SignalR backend testlerinde, SignalR Hub bağlantısı kurarken JWT authentication zorunludur. Testlerde önce bir kullanıcı register/login edilip JWT token alınmalı, ardından SignalR HubConnection oluşturulurken `options.AccessTokenProvider = () => Task.FromResult(token);` ile bu token kullanılmalıdır.
* Bu yöntem, hem .NET hem de context7 SignalR dokümantasyonundaki best practice'lere uygundur.
* SignalR testleri bu şekilde güncellendi ve tüm testler başarıyla geçti.

## 3. Uçtan Uca Şifreleme

### Görevler:

*   **Veri Modeli Güncellemesi:**
    *   [ ] `PawMatch.Domain/User.cs` modeline `PublicKey` alanı ekle (string veya byte[]).
    *   [ ] `PawMatch.Domain/Message.cs` modelindeki `Content` alanını şifrelenmiş veri için ayarla (string/byte[]).
    *   [ ] `PawMatch.Domain/MessageKeyBundle.cs` gibi, mesajın şifrelenmiş simetrik oturum anahtarını ve diğer şifreleme meta verilerini tutacak yeni bir model oluştur (Opsiyonel: eğer her mesaj için ayrı bir oturum anahtarı yönetilecekse).
    *   [ ] `AppDbContext.cs` içine ilgili `DbSet`'leri ekle.
    *   [ ] Veritabanı migrasyonlarını oluştur.

*   **Backend Servisleri ve Endpoint'ler (Kavramsal ve Altyapı):**
    *   [ ] Kullanıcının herkese açık anahtarını yüklemesi için `UsersController`'a `POST /api/v1/users/publicKey` gibi bir endpoint ekle.
    *   [ ] Belirli bir kullanıcının herkese açık anahtarını alması için `UsersController`'a `GET /api/v1/users/{userId}/publicKey` gibi bir endpoint ekle.
    *   [ ] `MessageService`'i, gelen şifrelenmiş mesaj içeriğini ve şifrelenmiş oturum anahtarını işleyecek şekilde güncelle. İçeriği çözmeye çalışmayacak, sadece kaydedecek.
    *   [ ] `MessageService`'i, şifrelenmiş mesajları ve ilgili anahtar paketlerini istemciye döndürecek şekilde güncelle.
    *   [ ] Herkese açık anahtarları güvenli bir şekilde yönetmek ve dağıtmak için arka plan mekanizmalarını düşün.

### Testler (Uçtan Uca Şifreleme):

*   [ ] Şifrelenmiş mesajların doğru şekilde saklandığını ve alındığını doğrulayan entegrasyon testleri yaz (içeriği çözmeden, sadece verinin bütünlüğünü kontrol ederek).
*   [ ] Herkese açık anahtar yükleme ve indirme endpoint'leri için testler yaz.
*   [ ] Yazılan testleri çalıştır ve doğrula.

## 4. Pass Diyerek Geçtiklerimizi Bir Süre Sonra Tekrardan Kullanıcı Karşısına Çıkarma

### Görevler:

*   [x] `DiscoverService.cs` Geliştirmesi:
    *   [x] `DiscoverService` içindeki `DiscoverUsersAsync` (tekil keşif metodu) güncellenecektir.
    *   [x] Bu metot, `UserSwipe` tablosundaki `SwipeDate` alanını kullanarak, belirli bir "geçiş süresinden" (`SwipeReappearDurationDays` gibi `appsettings.json`'dan okunacak) daha eski olan `IsLiked = false` (geçme) kayıtlarını hariç tutmamalıdır. Böylece, kullanıcılar belirli bir süre sonra tekrar keşif listesinde görünebilir.
    *   [x] **Yapılandırma**: `appsettings.json` içine `SwipeReappearDurationDays` ayarını ekle (örn. `90` gün).

### Testler (Geçilenleri Tekrar Gösterme):

*   [x] `PawMatch.Tests/DiscoverServiceTests.cs` (veya ilgili test dosyasına) geçilen kullanıcıların belirli bir süre sonra tekrar keşif listesinde göründüğünü doğrulayan testler ekle.
*   [x] Yazılan testleri çalıştır ve doğrula.

## 5. Kullanıcı Response Refaktörü ve DTO Ayrımı

- Tüm kullanıcı response'ları UserPublicDto (public) ve UserPrivateDto (private) ile dönmektedir.
- Hassas bilgiler (email, passwordHash) sadece UserPrivateDto'da bulunur ve sadece login/register/profile response'larında yer alır.
- Keşif, eşleşme ve genel kullanıcı listelerinde sadece UserPublicDto ile public alanlar yer alır.
- Hiçbir endpoint doğrudan domain User veya eski UserDto ile veri döndürmez.
- Mapping işlemleri merkezi UserPublicDtoMapper ve UserPrivateDtoMapper ile yapılır.

### Response Örnekleri

#### 1. Public (Keşif/Eşleşme)
```json
{
  "user": {
    "id": 2,
    "name": "Ali",
    "bio": "Kuşsever",
    "hasPet": false,
    "hasProfile": true,
    "photoIds": ["fileid3"],
    "age": null,
    "gender": null
  }
}
```
#### 2. Private (Login/Register/Profile)
```json
{
  "userPrivate": {
    "id": 1,
    "name": "Berkan",
    "email": "berkan@example.com",
    "bio": "Kedisever",
    "hasPet": true,
    "hasProfile": true,
    "photoIds": ["fileid1", "fileid2"],
    "age": null,
    "gender": null,
    "passwordHash": "..."
  }
}
```
