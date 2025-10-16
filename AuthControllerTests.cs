using Xunit;
using Moq;
using JwtTokenProject.Models;
using JwtTokenProject.Controllers;
using Microsoft.AspNetCore.SignalR;
using JwtTokenProject.Hubs;
using JwtTokenProject.Services;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using System.Reflection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;

namespace JwtTokenProject.Tests
{
    public class AuthControllerTests
    {
        //testte kullanacağımız sahte bağımlılıklar:kontrollü sahte davranış
        private readonly Mock<Microsoft.Extensions.Configuration.IConfiguration> _mockConfig;
        private readonly ApplicationDbContext _mockContext;
        private readonly Mock<IHubContext<UserHub>> _mockUserHub;
        private readonly Mock<IHubContext<ExcelProgressBarHub>> _mockExcelHub;
        private readonly Mock<INotificationServices> _mockNotificationService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            //_mockConfig nesnesi oluşturma ve yapılandırma
            _mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _mockConfig.SetupGet(x => x["Jwt:Key"]).Returns("a_very_secure_256_bit_jwt_key_1234567890"); 
            _mockConfig.SetupGet(x => x["Jwt:Issuer"]).Returns("https://localhost:5269");
            _mockConfig.SetupGet(x => x["Jwt:Audience"]).Returns("https://localhost:5269");
            _mockConfig.SetupGet(x => x["Jwt:ExpireMinutes"]).Returns("30");

            /*gerçek veritabanına bağlanmadan ram üzerinde çalışan geçicici bir veritabanı oluşturma
            test sonunda veriler kaybolur.*/
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _mockContext = new ApplicationDbContext(options);

            //  Fake bağımlılıklar
            _mockUserHub = new Mock<IHubContext<UserHub>>();
            _mockExcelHub = new Mock<IHubContext<ExcelProgressBarHub>>();
            _mockNotificationService = new Mock<INotificationServices>();

            //  Controller instance
            _controller = new AuthController(
                _mockConfig.Object,
                _mockContext,
                _mockUserHub.Object,
                _mockNotificationService.Object,
                _mockExcelHub.Object
            );
        }

        [Fact]
        public void GenerateJwtToken_ShouldReturnValidToken_WhenUserExists() // amacı: var olan bir kullanıcı için geçerli bir JWT token döndürüyor mu?
        {
            //Arrange(Hazırlık): sahte bir kullanıcı oluşturma ve veritabanına ekleme
            var user = new AppUserInfo
            {
                UserName = "oyku",
                Password = "oyku123456789",
                Email= "oyku.@example.com",
                IdentityNumber = 12345678900,
                AuthorityLevel = 4,               
                IsActive = true,
                FailedAttempt = 0,
                LastLoginDate = null,
                RememberMe = false,
                UserTypeName = "user",
                Token = null
              
            };
            _mockContext.AppUserInfos.Add(user);
            _mockContext.SaveChanges();

            // //Act(Eylem): GenerateJwtToken methodunu çağırma oradaki dönen token'ı alma. method private olduğu için reflection ile çağırıyoruz

            var token = typeof(AuthController)
                .GetMethod("GenerateJwtToken", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_controller, new object[] { "oyku" }) as string;

            // Assert
            //Assert(Doğrulama): dönen token'ın null olmadığını ve boş olmadığını doğrulama + üç parçalı token yapısına sahip olduğunu doğrulama
            token.Should().NotBeNullOrEmpty(); 
            token.Split(".").Should().HaveCount(3); // headers.payload.signature 

            // Token sahte mi değil mi uygulamaya mı ait mi kontrolü
            var handler = new JwtSecurityTokenHandler();
            
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a_very_secure_256_bit_jwt_key_1234567890")),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false
            };

            handler.Invoking(h => h.ValidateToken(token, validationParams, out _))
                   .Should().NotThrow();
            
            
            var jwtToken = handler.ReadJwtToken(token);
            //üretilen token içindeki claimleri doğrulama
            jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "oyku");
            jwtToken.Claims.Should().Contain(c => c.Type == "AuthorityLevel" && c.Value == "4");
            jwtToken.Claims.Should().Contain(c => c.Type == "IdentityNumber" && c.Value == "12345678900");


        }


        [Fact]
        public void GenerateJwtToken_ShouldThrowArgumentException_WhenUserNotFound() //amacı: var olmayan bir kullanıcı için ArgumentException fırlatıyor mu?
        {
            // Act
            Action act = () =>
            {
                typeof(AuthController)
                    .GetMethod("GenerateJwtToken",BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(_controller, new object[] { "nonexistent" });
            };

            // Assert
            act.Should().Throw<TargetInvocationException>() //exception doğrudan fırlatılmaz, TargetInvocationException içine sarılır
                .WithInnerException<ArgumentException>()
                .WithMessage("Kullanıcı bulunamadı.");
        }

        [Fact]
        public void GenerateJwtToken_ShouldThrowArgumentException_WhenJwtKeyIsMissing() //amacı: JWT anahtarı eksik olduğunda ArgumentException fırlatıyor mu?
        {
            // Arrange
            var user = new AppUserInfo { UserName = "testuser" ,IdentityNumber = 36052147896,Password= "123456789123",Email= "testuser.@example.com" };
            _mockContext.AppUserInfos.Add(user);
            _mockContext.SaveChanges();

            _mockConfig.SetupGet(x => x["Jwt:Key"]).Returns(string.Empty);

            // Act
            Action act = () =>
            {
                typeof(AuthController)
                    .GetMethod("GenerateJwtToken", BindingFlags.NonPublic |.BindingFlags.Instance)
                    .Invoke(_controller, new object[] { "testuser" });
            };

            // Assert
            act.Should().Throw<TargetInvocationException>()
                .WithInnerException<ArgumentException>()
                .WithMessage("JWT anahtarı yapılandırılmamış. Lütfen appsettings.json dosyasını kontrol edin.");
        }


    }
}
