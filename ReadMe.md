# JWTTokenProject.Test

## 🧩 Proje Hakkında

Bu proje, JwtTokenProject API’si için(https://github.com/oykugurbuz/JwtToken_Project__WebApi) birim (unit) ve entegrasyon (integration) testlerini içermektedir.
Amaç, kimlik doğrulama (authentication) sürecinin hem izole edilmiş (mock verilerle) hem de gerçek API üzerinden test edilmesidir.

## 📁 Klasör Yapısı
```markdown
JwtTokenProject.Tests/
│
├── AuthControllerTests.cs       → Unit testler
├── LoginIntegrationTest.cs      → Integration testler
└── JwtTokenProject.Tests.csproj → Test projesi yapılandırma dosyası
```
## ⚙️ Kullanılan Teknolojiler

- xUnit → Test framework’ü

- FluentAssertions → Anlaşılır assertion yazımı

- Microsoft.AspNetCore.Mvc.Testing → Integration test altyapısı

- Microsoft.AspNetCore.TestHost → In-memory web host (gerçek sunucu olmadan test)

- InMemory EF Core (isteğe bağlı) → Veritabanı bağımsız testler

## 🧪 Test Türleri
### 🔹 1. Unit Test (AuthControllerTests.cs)

- AuthController’ın metodlarını izole bir şekilde test eder.

- Mock (sahte) bağımlılıklar kullanır.
       ```csharp
        private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _mockConfig;
        private readonly ApplicationDbContext _mockContext;
        private readonly Mock<IHubContext<UserHub>> _mockUserHub;
        private readonly Mock<IHubContext<ExcelProgressBarHub>> _mockExcelHub;
        private readonly Mock<INotificationServices> _mockNotificationService;
        private readonly AuthController _controller;
       ```
- Gerçek veritabanına veya HTTP isteğine ihtiyaç duymaz.Gerçek veritabanından bağlanmaz, RAMüzerinde çalışan geçici bir veritabanı oluşturur.Test sonunda veriler kaybolur.

```csharp
  var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseInMemoryDatabase(databaseName: "TestDb")
      .Options;
  _mockContext = new ApplicationDbContext(options);
 ```
  #### GenerateJwtToken_ShouldReturnValidToken_WhenUserExists Methodu

  Amacı : Var olan bir kullanıcı için üretilen tokenın geçerli olup olmadığı test edilir.

 - Token boş ya da null mı?
 - Token headers.payload.signature olmak üzere üç parçaya bölünüyor mu?
 - Token secret key eşleşiyor mu?
 - Üretilen Token içindeki claimlar doğru mu? 

#### GenerateJwtToken_ShouldThrowArgumentException_WhenUserNotFound Methodu

Amacı: Var olmayan bir kullanıcı için ArgumentException fırlatıyor mu?

 #### GenerateJwtToken_ShouldThrowArgumentException_WhenJwtKeyIsMissing Methodu

 Amacı: JWT anahtarı eksik olduğunda ArgumentException fırlatıyor mu?


### 🔹 2. Integration Test (LoginIntegrationTest.cs)
- API’nin uçtan uca (end-to-end) davranışını test eder.

- Gerçek HTTP istekleri (HttpClient) üzerinden /api/Auth/login endpoint’ini dener.

- WebApplicationFactory<Program> kullanarak test sırasında API’yi in-memory olarak ayağa kaldırır.

#### Test edilen senaryolar:

- /api/Auth/login endpoint’i doğru kullanıcı bilgileriyle 200 OK döndürür.

- Dönen içerik boş değildir ve JWT token içerir.

- Hatalı bilgilerle istek atıldığında 401 Unauthorized döner.

## ▶️ Testleri Çalıştırma

Test prjesini çalıştırmak için terminalde:
```markdown
dotnet test JwtTokenProject.Tests/JwtTokenProject.Tests.csproj
```

## 🎯 Hedef

- Bu test yapısının amacı, hem kontrol katmanının (AuthController) mantıksal doğruluğunu,
hem de tüm API akışının bütünsel olarak doğru çalıştığını garanti altına almaktır.

