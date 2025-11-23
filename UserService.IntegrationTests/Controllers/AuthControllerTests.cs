using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using UserService.DTO;
using UserService.IntegrationTests.Helpers;
using UserService.IntegrationTests.Infrastructure;

namespace UserService.IntegrationTests.Controllers
{
    public class AuthControllerTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();
        private readonly AuthenticationHelper _authHelper = new(factory);

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

            // First registration succeeds
            var response1 = await _client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

            // Second time should fail
            var response2 = await _client.PostAsJsonAsync("/api/auth/register", dto);

            Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);

            var problem = await response2.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal("User with given username or email already exists.", problem.Detail);
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
            Assert.Equal("Invalid username or password.", problem.Detail);
            Assert.Equal((int)HttpStatusCode.Unauthorized, problem.Status);
        }

        [Fact]
        public async Task Login_Should_ReturnToken_WhenCredentialsAreValid()
        {
            var username = "login_user_valid";
            var password = "StrongPassword123!";

            // Using helper for seeding & login
            await _authHelper.SeedUserAsync(username, password);

            var dto = new { username, password };

            var response = await _client.PostAsJsonAsync("/api/auth/login", dto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseObj = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();

            Assert.NotNull(responseObj);
            Assert.False(string.IsNullOrWhiteSpace(responseObj.Token));
            Assert.Contains(".", responseObj.Token);
        }

        [Fact]
        public async Task Delete_Should_Return401_WhenNotLoggedIn()
        {
            var response = await _client.DeleteAsync("/api/auth");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Delete_Should_Return404_WhenUserNotFound()
        {
            // Create user and get token
            var username = "user_missing";
            var password = "Pass123!";

            var token = await _authHelper.RegisterAndAuthenticateAsync(username, password);

            // Remove user manually to simulate missing DB
            using (var scope = _authHelper.Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                var user = db.Users.Single(u => u.Username == username);
                db.Users.Remove(user);
                await db.SaveChangesAsync();
            }

            var response = await _authHelper.SendAuthenticatedAsync(
                HttpMethod.Delete, "/api/auth", token);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal("User not found.", problem!.Detail);
        }

        [Fact]
        public async Task Delete_Should_Return204_WhenUserIsDeleted()
        {
            var username = "delete_user_success";
            var password = "Password123!";

            var token = await _authHelper.RegisterAndAuthenticateAsync(username, password);

            var response = await _authHelper.SendAuthenticatedAsync(
                HttpMethod.Delete, "/api/auth", token);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}