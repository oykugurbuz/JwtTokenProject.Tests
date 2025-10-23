using System.Net.Http.Json;
using JwtTokenProject.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace JwtTokenProject.Tests.Integration
{
    public class AuthIntegrationTests
    {
        private readonly HttpClient _client;

        public AuthIntegrationTests()
        {

            var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var settings = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=DESKTOP-UTB1M8P\\SQLEXPRESS;Database=WebAppDemoDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true;Connection Timeout=60;"
                };
                config.AddInMemoryCollection(settings);
            });
        });

            _client = factory.CreateClient(); // 🔹 Artık test için client hazır
        }

        [Fact]
        public async Task Login_ShouldReturnJwtToken_WhenCredentialsAreValid()
        {
            // Arrange

            var loginModel = new 
            {
                UserName = "ayse_kara",
                Password = "securepass"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/Auth/login", loginModel);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var result = await response.Content.ReadAsStringAsync();
            result.Should().NotBeNullOrEmpty();
        }
    }
}