# PawMatch Eşleşme ve Mesajlaşma Özelliği Planlama Dokümantasyonu

Bu doküman, PawMatch uygulamasında eşleşme mekanizmasının geliştirilmesi ve uçtan uca mesajlaşma altyapısının planlanması için ayrıntılı bilgileri içerir. Bu aşamada sadece planlama yapılmakta, kod yazımı henüz başlamamıştır.

## 1. Özellik: `MatchService.cs` - `LikeOrPassAsync` Geliştirmesi

Bu özellik, kullanıcıların birbirlerini beğenmesi veya geçmesi durumunda gerçekleşecek eşleşme mantığını tanımlayacaktır.

**Mevcut Durum:**
`MatchActionDto` (User1Id, User2Id, Liked) girdisi alıyor ve `MatchResultDto` (MatchId, Confirmed) döndürüyor. Mevcut implementasyon bir yer tutucudur (`TODO`).

**Planlama:**

1.  **Girdi Doğrulama**: `MatchActionDto` içindeki `User1Id`'nin, isteği yapan kimliği doğrulanmış kullanıcının ID'si olduğundan emin olunmalıdır. Yetkisiz bir ID gelirse hata fırlatılmalıdır.
2.  **Kullanıcı Kaydını Tutma (Swipe Geçmişi)**:
    *   Kullanıcıların birbiri üzerindeki beğenme/geçme eylemlerini kaydetmek için yeni bir veri modeli/tablosu (`UserSwipe`) gereklidir. Bu, hem geçmişi tutacak hem de tekrar karşılaşma engellemesini sağlayacaktır.
    *   Eğer `MatchActionDto.Liked` `true` ise (beğenme eylemi), bu eylem `UserSwipe` tablosuna kaydedilir.
    *   Eğer `MatchActionDto.Liked` `false` ise (geçme eylemi), bu da `UserSwipe` tablosuna kaydedilir.
3.  **Eşleşme Kontrolü**:
    *   Mevcut kullanıcı (User1), hedef kullanıcıyı (User2) beğendiyse (`Liked = true`):
        *   Veritabanında User2'nin de User1'i daha önce beğenip beğenmediği (`UserSwipe` tablosunda `SwiperId = User2Id` ve `SwipedUserId = User1Id` ve `IsLiked = true` olan bir kayıt var mı) kontrol edilir.
        *   Eğer karşılıklı bir beğeni varsa, yani bir "eşleşme" gerçekleştiyse:
            *   `Match` tablosunda bu iki kullanıcı arasında yeni bir eşleşme kaydı oluşturulur veya mevcut eşleşme (`Confirmed = false` ise) `Confirmed = true` olarak güncellenir.
            *   Yanıt olarak `MatchResultDto` içinde `Confirmed = true` ve ilgili `MatchId` döndürülür.
        *   Eğer karşılıklı beğeni yoksa, sadece beğenme eylemi kaydedilir ve `MatchResultDto` içinde `Confirmed = false` döndürülür.
    *   Eğer mevcut kullanıcı (User1) hedef kullanıcıyı (User2) geçtiyse (`Liked = false`):
        *   Sadece geçme eylemi kaydedilir. Bir eşleşme gerçekleşmez.
        *   Eğer bu iki kullanıcı arasında daha önce onaylanmış bir eşleşme varsa, bu eşleşmenin durumu `Confirmed = false` olarak güncellenmeli veya silinmelidir (genellikle durum güncellemek geçmişi korumak adına tercih edilir).
        *   Yanıt olarak `MatchResultDto` içinde `Confirmed = false` döndürülür.
4.  **Veritabanı İşlemleri**: `UserSwipe` kayıtları ve `Match` tablosu güncellemeleri transaction içerisinde yapılmalıdır.
5.  **Yanıt**: İşlem sonucuna göre `MatchResultDto` döndürülür.

## 2. Özellik: Kullanıcının Beğendiği veya Geçtiği Kullanıcıların Veritabanında Tutulması ve Tekrar Karşılaşma Engellemesi

Bu özellik, keşif algoritmasının iyileştirilmesi ve kullanıcılara daha önce etkileşimde bulundukları profillerin tekrar gösterilmemesini sağlayacaktır.

**Planlama (Altyapı):**

1.  **Yeni Veri Modeli (`UserSwipe`)**:
    *   **Konum**: `PawMatch.Domain/UserSwipe.cs`
    *   **Özellikler**:
        *   `Id` (int): Birincil anahtar.
        *   `SwiperId` (int): Swipe eylemini yapan kullanıcının ID'si (Foreign Key: `User`).
        *   `SwipedUserId` (int): Swipe eylemine maruz kalan kullanıcının ID'si (Foreign Key: `User`).
        *   `IsLiked` (bool): `true` ise beğenme, `false` ise geçme.
        *   `SwipeDate` (DateTimeOffset / DateTime UTC): Swipe eyleminin gerçekleştiği zaman damgası.
    *   **İlişkiler**: `User` modeli ile iki adet bire-çok ilişki (Swiper ve SwipedUser).
2.  **`AppDbContext` Güncellemesi**: `AppDbContext.cs` dosyasına `DbSet<UserSwipe> UserSwipes { get; set; }` eklenecektir.
3.  **Veritabanı Migrasyonu**: `UserSwipe` tablosunu veritabanına eklemek için yeni bir Entity Framework Core migrasyonu oluşturulacaktır.
4.  **`IUserSwipeRepository` (Opsiyonel ama Önerilir)**: `UserSwipe` işlemleri için (`AddAsync`, `GetBySwiperAndSwipedUserAsync`, `GetRecentSwipesAsync` vb.) bir `IUserSwipeRepository` arayüzü ve `UserRepository.cs` gibi somut bir implementasyon (`UserSwipeRepository.cs`) oluşturulması, veri erişim mantığını daha düzenli hale getirir.
5.  **`DiscoverService` Geliştirmesi**:
    *   `DiscoverService.DiscoverUsersAsync` (tekil keşif metodu) güncellenecektir.
    *   Bu metod, `currentUserId`'nin belirli bir süre (`configurableDuration` gibi bir ayardan okunabilir) içinde zaten beğenmiş veya geçmiş olduğu kullanıcıları (`UserSwipe` tablosundan) sorgulayarak hariç tutmalıdır.
    *   **Süre Ayarı**: Bu "değişken süre" (`configurableDuration`), `appsettings.json` gibi bir yapılandırma dosyasında tutulmalıdır (örn. `SwipeExclusionDurationDays: 30`).
    *   **Konum Tabanlı Filtreleme**: `PawMatch Backend Guidelines.markdown`'da belirtilen PostGIS tabanlı konum filtreleme (`maxDistanceKm` parametresi) `DiscoverService` içinde uygulanmalıdır.
    *   **Pet Türü Filtreleme**: Yönergelerde belirtilen pet türü filtrelemesi de keşif algoritmasına dahil edilmelidir.

## 3. Özellik: Uçtan Uca Mesaj Şifreleme (Altyapı Planlaması)

Bu, mesajlaşma özelliğinin gelecekteki güvenliğini sağlamaya yönelik önemli bir adımdır. Backend, şifrelenmemiş mesaj içeriğine asla erişemez.

**Planlama (Altyapı):**

1.  **Anahtar Yönetimi**:
    *   **Herkese Açık/Özel Anahtar Çiftleri (Public/Private Key Pairs)**: Her kullanıcı için bir asimetrik anahtar çifti (örneğin RSA veya Eliptik Eğri Kriptografisi) gereklidir.
    *   **Özel Anahtar Depolama**: Kullanıcının özel anahtarları **ASLA sunucuda saklanmayacaktır**. Bunlar istemci tarafında (mobil uygulama) oluşturulmalı ve kullanıcının cihazında güvenli bir şekilde (örneğin mobil cihazın güvenli bölgesi, anahtar zinciri veya güçlü bir parola türetme mekanizmasıyla) saklanmalıdır.
    *   **Herkese Açık Anahtar Depolama**: Kullanıcının herkese açık anahtarları sunucuda saklanacaktır (örneğin `User` tablosunda yeni bir sütun veya ayrı bir `UserPublicKey` tablosunda). Bu anahtarlar, diğer kullanıcıların mesajları şifrelemesi için kullanılacaktır.
2.  **Şifreleme Süreci (Kavramsal)**:
    *   **İstemci Tarafında Şifreleme**: Mesaj içeriği, sunucuya gönderilmeden önce gönderen istemci tarafında şifrelenmelidir.
    *   **Symmetric Oturum Anahtarı (Session Key)**: Her bir mesajlaşma oturumu (veya her mesaj için), daha küçük ve hızlı bir simetrik anahtar (örneğin AES-256) oluşturulacaktır. Mesajın kendisi bu simetrik anahtar ile şifrelenecektir.
    *   **Asimetrik Anahtar Değişimi**: Bu simetrik oturum anahtarı, alıcının **herkese açık anahtarı** kullanılarak şifrelenecektir. Böylece, mesajla birlikte gönderilen şifrelenmiş oturum anahtarı, yalnızca alıcının **özel anahtarı** ile çözülebilir.
    *   **Mesaj Yapısı**: Mesajlar, şifrelenmiş içerik, şifrelenmiş oturum anahtarı ve mesajın çözülmesinde kullanılacak ek şifreleme parametrelerini (IV, nonce vb.) içerecektir.
3.  **Sunucunun Rolü**:
    *   Sunucu, şifrelenmemiş mesaj içeriğine **hiçbir zaman** erişemeyecektir.
    *   Şifrelenmiş mesajları ve ilgili anahtar paketlerini veritabanında saklayacaktır.
    *   Kullanıcıların herkese açık anahtarlarını yönetmekten ve eşleşen kullanıcılara dağıtmaktan sorumlu olacaktır.
    *   Mesajların meta verilerini (kimden, kime, zaman damgası, eşleşme ID'si) yönetecektir.
4.  **Veritabanı Değişiklikleri**:
    *   `User` Modeli: Herkese açık anahtar için `PublicKey` (string veya byte dizisi) gibi bir alan eklenebilir.
    *   `Message` Modeli: `Content` alanı artık şifrelenmiş veriyi (Base64 kodlanmış string veya byte dizisi) saklayacaktır.
    *   Mesajın şifrelenmiş simetrik oturum anahtarını ve diğer şifreleme meta verilerini tutmak için yeni bir tablo (`MessageKeyBundle` gibi) gerekebilir. Bu tablo, belirli bir mesaj veya sohbetle ilişkilendirilecektir.
5.  **API Değişiklikleri (Kavramsal)**:
    *   Kullanıcıların herkese açık anahtarlarını yüklemesi ve alması için yeni endpoint'ler gerekebilir.
    *   Mevcut `POST /api/v1/messages` endpoint'i, artık şifrelenmiş mesaj içeriğini ve şifrelenmiş oturum anahtarını kabul edecek şekilde güncellenmelidir.
    *   Mevcut `GET /api/v1/messages/{matchId}` endpoint'i, şifrelenmiş mesajları ve istemci tarafında çözümleme için gerekli anahtar paketlerini döndürmelidir.
6.  **İstemci Tarafı Gereksinimleri**: Flutter mobil uygulaması tarafında, anahtar çifti oluşturma, mesaj şifreleme/şifre çözme ve güvenli anahtar yönetimi için güçlü kriptografik kütüphanelerin entegrasyonu gerekecektir.

## 4. Eksik ve İleriye Yönelik İyileştirmeler

1.  **Eşleşme ve Swipe Mekanizması İçin Daha Detaylı Durum Yönetimi**:
    *   `LikeOrPassAsync` içinde, bir kullanıcı bir diğerini beğendiğinde ve henüz karşılıklı beğeni yoksa, bu beğeni durumunun veritabanında "beklemede" (`pending`) bir eşleşme olarak işaretlenmesi ve karşıdaki kullanıcı beğendiğinde bu eşleşmenin "onaylanması" (`confirmed`) mekanizması daha açıkça belirtilebilir. Mevcut plan `Match` tablosunda `Confirmed` alanı üzerinden bunu yönetiyor, ancak akışın netleştirilmesi faydalı olabilir.
    *   Bir kullanıcı birini "geçtiğinde" (`pass`), bu bilginin sadece keşif listesinde tekrar görünmemesi için değil, aynı zamanda ileride kullanıcı fikrini değiştirirse bu eylemi geri alma gibi senaryolar için de düşünülmesi. (Bu belki daha sonraki bir aşama ama altyapıda esneklik sağlar).

2.  **Mesajlaşma (Uçtan Uca Şifreleme) - İleriye Dönük Güvenlik Odaklı Eksikler**:
    *   **Mükemmel İleri Gizlilik (Perfect Forward Secrecy - PFS)**: Mevcut plan simetrik oturum anahtarları ve asimetrik anahtar değişimi içeriyor, bu da bir PFS formuna işaret ediyor. Ancak bu terimin açıkça belirtilmesi, iletişimin uzun vadeli güvenliğini vurgular. Bu, tek bir uzun ömürlü anahtarın tehlikeye atılmasının geçmiş konuşmaları tehlikeye atmamasını sağlar. Genellikle, her yeni oturum veya mesaj için efemeral anahtarların kullanılmasıyla sağlanır.
    *   **Kimlik Doğrulama/Anahtar Doğrulama (Key Verification)**: Herkese açık anahtarların değişimi planlanmış olsa da, kullanıcıların bu herkese açık anahtarların gerçekten bekledikleri kişiye ait olduğunu nasıl doğrulayacakları (yani "anahtar doğrulama") konusu önemlidir. Bu, genellikle istemci tarafı bir zorluktur (örn. QR kodları, anahtar parmak izleri), ancak backend'in güvenilir anahtar dağıtımında bir rolü vardır. Bu planlama aşamasında doğrudan backend implementasyonu olmasa da, mimari düşünceler içinde belirtilmesi gereken bir güvenlik özelliğidir.
    *   **İletişim Güvenliği Protokolleri**: Plan, anahtar yönetimi ve şifreleme sürecinin kavramsal bir tanımını veriyor. İleride Signal Protocol gibi kurulmuş ve denetlenmiş bir uçtan uca şifreleme protokolünün detaylarının araştırılması ve implementasyonunun düşünülmesi gerekebilir.

3.  **Performans ve Ölçeklenebilirlik İçin Ek Düşünceler (Genel)**:
    *   **Ön Bellekleme (Caching)**: Özellikle `DiscoverService` gibi sık çağrılan ve yoğun hesaplama gerektirebilecek servisler için ön bellekleme stratejileri (örneğin Redis ile) düşünülebilir. Beğenme/geçme geçmişi büyüdükçe, bu sorguların performansı etkileyebilir.
    *   **Gerçek Zamanlı İletişim (SignalR)**: Anlık mesajlaşma ve bildirim deneyimi için **SignalR** kullanılacaktır. Bu, mesajların veritabanına kaydedildikten sonra eşleşen kullanıcının bağlı istemcilerine anlık olarak iletilmesini sağlayacaktır.

4.  **Hata Yönetimi ve Gözlemlenebilirlik (Genel İyileştirme)**:
    *   **Detaylı Hata Kodları/Mesajları**: API yanıtlarında kullanılan `status: "error"` ve `error: "mesaj"` formatı geneldir. Daha spesifik, istemci tarafının anlayabileceği ve işleyebileceği hata kodları ve daha açıklayıcı hata mesajları sağlamak, hata ayıklama ve entegrasyonu kolaylaştırır.
    *   **Kapsamlı Loglama**: Yeni özellikler implemente edilirken, özellikle eşleşme akışı ve şifreleme işlemleri için detaylı loglama (hata, uyarı, bilgi seviyeleri) eklenmesi, sorun giderme ve izleme için kritik öneme sahiptir.

### 5.5.1. Gerçek Zamanlı İletişim (SignalR)
- **Amaç**: Anlık mesajlaşma ve eşleşme bildirimleri için gerçek zamanlı iletişim sağlamak.
- **Teknoloji**: ASP.NET Core SignalR, `IRealtimeNotificationService` aracılığıyla soyutlanmış.
- **Hub Yapılandırması**: 
  - `Program.cs`'te SignalR servisleri eklenmeli (`builder.Services.AddSignalR()`).
  - Bir SignalR Hub (`ChatHub.cs` gibi) oluşturulmalı. Bu Hub, istemcilerin çağırabileceği ve sunucunun istemcilere gönderebileceği metotları içerecek (örn. `SendMessage`, `ReceiveMessage`, `NotifyMatchFound`).
  - Hub, belirli bir URL üzerinde yapılandırılmalı (örn. `app.MapHub<ChatHub>("/chatHub");`).
- **Servis Katmanı Entegrasyonu (`IRealtimeNotificationService`)**:
  - `PawMatch.Application.Interfaces` içinde `IRealtimeNotificationService` adında bir arayüz tanımlanır. Bu arayüz, gerçek zamanlı bildirim gönderme ihtiyacını soyutlar.
  - `api/PawMatch.Api/Services/SignalRNotificationService.cs` adında bir sınıf oluşturularak `IRealtimeNotificationService` arayüzü implemente edilir. Bu sınıf, `IHubContext<ChatHub>`'ı kullanarak gerçek SignalR çağrılarını yapar.
  - `MatchService.cs` ve `MessageService.cs` gibi uygulama katmanındaki servisler, doğrudan `IHubContext<ChatHub>` yerine `IRealtimeNotificationService` bağımlılığını enjekte eder ve kullanır. Bu, uygulama katmanının SignalR'a olan doğrudan bağımlılığını kaldırarak daha temiz bir mimari sağlar.
- **Mesajlaşma Akışı**: 
  - Kullanıcı bir mesaj gönderdiğinde (`POST /api/v1/messages`), `MessageService` mesajı veritabanına kaydeder.
  - Kayıt başarılı olduktan sonra, `MessageService`, `IRealtimeNotificationService`'ı kullanarak mesajı eşleşen kullanıcının bağlı SignalR istemcilerine anlık olarak iletir.
- **Eşleşme Bildirimi Akışı**: 
  - `MatchService` içinde yeni bir eşleşme onaylandığında, `IRealtimeNotificationService`'ı kullanarak ilgili kullanıcılara anlık bildirim (`NotifyMatchFound` gibi bir metod ile) gönderilir.
- **Kimlik Doğrulama**: SignalR bağlantıları, mevcut JWT kimlik doğrulama mekanizması ile güvence altına alınacaktır. İstemciler, SignalR bağlantısı kurulurken JWT token'larını sağlayacaklardır.
- **Ölçeklenebilirlik**: Yüksek trafik durumlarında birden fazla sunucuya ölçeklenmek için SignalR backplane'leri (örn. Redis, Azure SignalR Service) kullanılabilir.

## 5.5.1. Gerçek Zamanlı İletişim (SignalR) - Testler ve Entegrasyon Notu
- SignalR backend testlerinde, SignalR Hub bağlantısı kurarken JWT authentication zorunludur. Testlerde önce bir kullanıcı register/login edilip JWT token alınmalı, ardından SignalR HubConnection oluşturulurken `options.AccessTokenProvider = () => Task.FromResult(token);` ile bu token kullanılmalıdır.
- Bu yöntem, hem .NET hem de context7 SignalR dokümantasyonundaki best practice'lere uygundur.
- SignalR testleri bu şekilde güncellendi ve tüm testler başarıyla geçti.

## 4. Kullanıcı Response Refaktörü ve DTO Ayrımı

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

## Repository Pattern ve Katmanlı Mimari Güncellemesi
- Eşleşme işlemleri için IMatchRepository arayüzü ve MatchRepository implementasyonu eklendi.
- MatchService, eşleşme işlemlerinde doğrudan DbContext yerine MatchRepository kullanıyor.
- Program.cs'de IMatchRepository için DI kaydı yapıldı.
- Mesajlaşma ve eşleşme endpointleri için repository altyapısı tamamlandı.
