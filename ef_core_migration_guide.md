## 📦 Entity Framework Core Migration Kullanım Kılavuzu

Bu döküman, **EF Core Code First** yaklaşımıyla veri tabanı migration'larının nasıl oluşturulup uygulandığını adım adım açıklar.

> 💡 **Not:** Aşağıdaki komutlar çalıştırılırken terminalde `C:\Projects\Business\PawMatch\api` klasöründe (ana projenin bulunduğu dizin) olunması gerekir.

---

### 🧱 Migration Oluşturmak

```bash
 dotnet ef migrations add <MIGRATION_ADI> --project PawMatch.Infrastructure --startup-project PawMatch.Api
```

#### Parametreler:

- `<MIGRATION_ADI>`: Migration'a verilecek isim.
  - Örnek: `InitialCreate`, `AddedPetEntity`, `UpdateUserEmail`

#### Sabit Parametreler:

- `--project PawMatch.Infrastructure`: `DbContext` ve entity'lerin bulunduğu proje katmanı.
- `--startup-project PawMatch.Api`: `Program.cs` ve `appsettings.json` gibi dosyaların bulunduğu başlangıç projesi.

#### Örnek:

```bash
dotnet ef migrations add InitialCreate --project PawMatch.Infrastructure --startup-project PawMatch.Api
```

> ⚠️ Bu komutu çalıştırmadan önce projelerin derlenebilir durumda olduğundan emin olun.

---

### 🏗️ Veritabanını Güncellemek

```bash
dotnet ef database update --project PawMatch.Infrastructure --startup-project PawMatch.Api
```

#### Sabit Parametreler:

- `--project PawMatch.Infrastructure`
- `--startup-project PawMatch.Api`

#### Açıklama:

Bu komut, mevcut en son migration'ı kullanarak veritabanını günceller.

#### Örnek:

```bash
dotnet ef database update --project PawMatch.Infrastructure --startup-project PawMatch.Api
```

> Bu işlem sonucunda veritabanında gerekli tablolar oluşturulur veya güncellenir.

---

### 🔁 Yeni Migration Eklemek

Yeni bir model eklendikten veya mevcut modellerde değişiklik yapıldıktan sonra tekrar migration oluşturmak gerekir:

```bash
dotnet ef migrations add <YENI_MIGRATION_ADI> --project PawMatch.Infrastructure --startup-project PawMatch.Api
```

#### Örnek:

```bash
dotnet ef migrations add AddedPetEntity --project PawMatch.Infrastructure --startup-project PawMatch.Api
```

> Migration adı, yapılan değişikliği tanımlayacak şekilde verilmelidir. Örnekler: `RenamedUserField`, `RemovedOldTable`, `AddProfilePictureToUser`

---

### ✅ Notlar

- EF Core CLI aracının kurulu olduğundan emin olun:

```bash
dotnet tool install --global dotnet-ef
```

- Projenizde `Microsoft.EntityFrameworkCore.Design` paketi kurulu olmalıdır:

```bash
dotnet add package Microsoft.EntityFrameworkCore.Design
```

- Tüm komutlar, proje kök dizininden (solution ".sln" dosyasının bulunduğu yer, bu durumda `C:\Projects\Business\PawMatch\api`) çalıştırılmalıdır.

