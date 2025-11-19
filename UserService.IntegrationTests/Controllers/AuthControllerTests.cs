using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using UserService.Data;
using UserService.Domain.Entities;
using UserService.DTO;
using UserService.IntegrationTests.Infrastructure;

namespace UserService.IntegrationTests.Controllers
{
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory _factory;

        public AuthControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task SeedUserAsync(string username, string password)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = new User
            {
                Username = username,
                Email = $"{username}@example.com",
                FirstName = "Test",
                LastName = "User",
                Address = "Address",
                Role = UserService.Domain.Enums.UserRole.Host,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        [Fact]
        public async Task Register_Should_Return201_Then409_OnDuplicate()
        {
            var dto = new
            {
                username = "testuser",
                email = "test@example.com",
                password = "password",
                firstName = "Test",
                lastName = "User",
                address = "Address",
                role = 0
            };

            var response1 = await _client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

            var response2 = await _client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);

            var problem = await response2.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal("User with given username or email already exists.", problem!.Detail);
            Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        }

        [Fact]
        public async Task Login_Should_Return401_WhenUserDoesNotExist()
        {
            var dto = new
            {
                username = "non_existing_user",
                password = "somePassword"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal("Invalid username or password.", problem!.Detail);
            Assert.Equal((int)HttpStatusCode.Unauthorized, problem.Status);
        }

        [Fact]
        public async Task Login_Should_ReturnToken_WhenCredentialsAreValid()
        {
            var username = "login_user_valid";
            var password = "StrongPassword123!";

            await SeedUserAsync(username, password);

            var dto = new
            {
                username,
                password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseObj = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();

            Assert.NotNull(responseObj);
            Assert.False(string.IsNullOrWhiteSpace(responseObj!.Token));
            Assert.Contains(".", responseObj.Token);
        }
    }
}
