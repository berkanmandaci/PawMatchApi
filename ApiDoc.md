# PawMatch Backend API Dokümantasyonu

Bu doküman, PawMatch uygulamasının backend API'lerini ve uyulması gereken temel kuralları özetler.

## 1. Genel Kurallar

*   **Mimari**: Katmanlı mimari (.NET 8+ MVC - Controller-Based API) kullanılmıştır. İş mantığı servis katmanında (`PawMatch.Application.Services`), veri erişimi repository deseniyle (`PawMatch.Infrastructure.Repositories`) yönetilir.
*   **SOLID Prensipleri**: Kod genel olarak SOLID prensiplerine uygun tasarlanmıştır, özellikle bağımlılıkların arayüzler üzerinden enjekte edilmesi (`IUserService`, `IStorageProvider` vb.) dikkat çeker.
*   **RESTful Prensipler**: API endpoint'leri RESTful prensiplere uygun olarak HTTP metotları (GET, POST, PATCH, DELETE) kullanır.
*   **API Versiyonlama**: Tüm API endpoint'leri `/api/v1/` ön eki ile versiyonlanmıştır.
*   **Yanıt Formatı**: Başarılı API yanıtları `{ "data": {}, "status": "success", "error": null }` formatındadır. Hata yanıtları genellikle `{ "status": "error", "error": "mesaj" }` formatını kullanır.
*   **Kimlik Doğrulama**: Çoğu endpoint JWT (JSON Web Token) ile korunmaktadır. Kullanıcı kimliği doğrulama için `[Authorize]` niteliği kullanılır.
*   **Güvenlik**:
    *   Şifreler BCrypt ile hash'lenir.
    *   Google Drive'da depolanan fotoğraflar özeldir (private) ve doğrudan herkese açık bağlantılar paylaşılmaz.
    *   Fotoğraf yüklemeleri için dosya tipi (JPEG/PNG) ve boyutu (maksimum 5 MB) doğrulaması yapılır.
    *   Fotoğraf görüntüleme için gelişmiş yetkilendirme kuralları uygulanır (sadece fotoğrafın sahibi, evcil hayvanın sahibi veya keşif listesindeki kullanıcılar erişebilir).
*   **Veritabanı**: PostgreSQL, Entity Framework Core (Code First) ile kullanılır. Testler için in-memory veritabanı kullanılır.
*   **Konteynerleştirme**: Uygulama Docker kullanılarak konteynerleştirilmiştir. `docker-compose.yml` ile uygulama ve PostgreSQL veritabanı servisleri birlikte yönetilir.

## 2. API Endpoint'leri

### 2.1. Kimlik Doğrulama ve Kullanıcı Yönetimi (`/api/v1/users`)

*   **Kullanıcı Kaydı**
    *   `POST /api/v1/users/register`
    *   Açıklama: Yeni bir kullanıcı hesabı oluşturur ve başarılı olursa JWT token döndürür.
    *   Girdi DTO: `UserRegisterDto` (name, email, password)
    *   Çıktı DTO: `UserAuthResponseDto` (user: UserDto, token)

*   **Kullanıcı Girişi**
    *   `POST /api/v1/users/login`
    *   Açıklama: Mevcut bir kullanıcının kimlik bilgileriyle giriş yapar ve JWT token döndürür.
    *   Girdi DTO: `UserLoginDto` (email, password)
    *   Çıktı DTO: `UserAuthResponseDto` (user: UserDto, token)

*   **Kullanıcı Profilini Güncelleme**
    *   `PATCH /api/v1/users/profile`
    *   Açıklama: Kimliği doğrulanmış kullanıcının profil bilgilerini (adı, biyografisi, evcil hayvanı olup olmadığı) günceller.
    *   Yetkilendirme: Gerekli (`[Authorize]`)
    *   Girdi DTO: `UpdateProfileDto` (name, bio, hasPet)
    *   Çıktı DTO: `UserAuthResponseDto` (user: UserDto, token)

*   **Kullanıcı Hesabını Silme**
    *   `DELETE /api/v1/users/me`
    *   Açıklama: Kimliği doğrulanmış kullanıcının hesabını ve ilişkili tüm verilerini (evcil hayvanlar, fotoğraflar) siler.
    *   Yetkilendirme: Gerekli (`[Authorize]`)
    *   Girdi: Yok
    *   Çıktı: Başarılı yanıt (`status: "success"`)

*   **Kullanıcı Profil Bilgilerini Getirme**
    *   `GET /api/v1/users/me`
    *   Açıklama: Kimliği doğrulanmış kullanıcının profil bilgilerini döndürür.
    *   Yetkilendirme: Gerekli (`[Authorize]`)
    *   Girdi: Yok
    *   Çıktı DTO: `UserDto` (id, name, email, bio, hasPet, hasProfile, PhotoIds)

### 2.2. Eşleşme ve Keşif Mekanizması (`/api/v1/matches`)

*   **Kullanıcı/Pet Kartlarını Keşfetme**
    *   `GET /api/v1/matches/discover`
    *   Açıklama: Keşif için uygun kullanıcı ve evcil hayvan kartlarını listeler. Şu anda giriş yapan kullanıcı dışındaki tüm kullanıcıları basitçe listeler.
    *   Yetkilendirme: Gerekli (`[Authorize]`)
    *   Sorgu Parametreleri: `maxDistanceKm` (int, opsiyonel), `offset` (int, opsiyonel), `limit` (int, opsiyonel)
    *   Çıktı DTO: `List<DiscoverUserPetDto>` (user: DiscoverUserDto, pet: DiscoverPetDto)

*   **Beğenme/Geçme İşlemi**
    *   `POST /api/v1/matches`
    *   Açıklama: Bir kullanıcıya yönelik beğenme (like) veya geçme (pass) işlemini gerçekleştirir. Eşleşme mantığı henüz tamamlanmamıştır (TODO).
    *   Yetkilendirme: Gerekli (`[Authorize]`)
    *   Girdi DTO: `MatchActionDto` (user1Id, user2Id, liked)
    *   Çıktı DTO: `MatchResultDto` (matchId, confirmed)

### 2.3. Fotoğraf Yönetimi (`/api/v1/photos`)

*   **Kullanıcı Profil Fotoğrafı Yükleme**
    *   `POST /api/v1/photos/user`
    *   Açıklama: Kimliği doğrulanmış kullanıcıya bir fotoğraf yükler.
    *   Yetkilendirme: Gerekli (`[Authorize]`)
    *   Girdi: Multipart form-data (`IFormFile` - JPEG/PNG, maks. 5 MB)
    *   Çıktı DTO: `PhotoDto` (id, fileName, contentType, googleDriveFileId, uploadDate, userId, petId)

*   **Pet Fotoğrafı Yükleme**
    *   `POST /api/v1/users/pets/{petId}/photos`
    *   Açıklama: Belirtilen evcil hayvana bir fotoğraf yükler.
    *   Yetkilendirme: Gerekli (`[Authorize]`)
    *   URL Parametreleri: `petId` (int)
    *   Girdi: Multipart form-data (`IFormFile` - JPEG/PNG, maks. 5 MB)
    *   Çıktı DTO: `PhotoDto` (id, fileName, contentType, googleDriveFileId, uploadDate, userId, petId)

*   **Fotoğraf Görüntüleme**
    *   `GET /api/v1/photos/{id}`
    *   Açıklama: Belirtilen Google Drive Dosya Kimliğine (`id`) sahip fotoğrafın akışını döndürür. Yetkilendirme kuralları uygulanır.
    *   Yetkilendirme: Gerekli (`[Authorize]`)
    *   URL Parametreleri: `id` (string - Google Drive File ID)
    *   Çıktı: Fotoğraf akışı (`FileStreamResult`)

*   **Fotoğraf Silme**
    *   `DELETE /api/v1/photos/{id}`
    *   Açıklama: Belirtilen Google Drive Dosya Kimliğine (`id`) sahip fotoğrafı hem Google Drive'dan hem de veritabanından siler.
    *   Yetkilendirme: Gerekli (`[Authorize]`)
    *   URL Parametreleri: `id` (string - Google Drive File ID)
    *   Çıktı: Başarılı yanıt (`status: "success"`)

## 3. Repository Katmanı Güncellemesi

- Eşleşme işlemleri için IMatchRepository arayüzü ve MatchRepository implementasyonu eklendi.
- MatchService, eşleşme işlemlerinde doğrudan DbContext yerine MatchRepository kullanıyor.
- Program.cs'de IMatchRepository için DI kaydı yapıldı.
- Mesajlaşma ve eşleşme endpointleri için repository altyapısı tamamlandı.
