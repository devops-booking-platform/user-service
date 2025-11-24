using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using UserService.Common.Constants;
using UserService.Data;
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
            Assert.Equal(ExceptionMessages.Auth.UserAlreadyExists, problem.Detail);
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
            Assert.Equal(ExceptionMessages.Auth.InvalidCredentials, problem.Detail);
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
            Assert.Equal(ExceptionMessages.User.UserNotFound, problem!.Detail);
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

        [Fact]
        public async Task UpdateProfile_Should_Return401_WhenNotLoggedIn()
        {
            var updateDto = new UpdateProfileRequestDTO
            {
                Username = "new_username",
                Email = "new@example.com",
                FirstName = "New",
                LastName = "User",
                Address = "New address"
            };

            var response = await _client.PutAsJsonAsync("/api/auth/profile", updateDto);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateProfile_Should_Return404_WhenUserNotFound()
        {
            var username = "profile_missing_user";
            var password = "Password123!";

            var token = await _authHelper.RegisterAndAuthenticateAsync(username, password);

            using (var scope = _authHelper.Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = db.Users.Single(u => u.Username == username);
                db.Users.Remove(user);
                await db.SaveChangesAsync();
            }

            var updateDto = new UpdateProfileRequestDTO
            {
                Username = "whatever",
                Email = "whatever@example.com",
                FirstName = "New",
                LastName = "User",
                Address = "New address"
            };

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PutAsJsonAsync("/api/auth/profile", updateDto);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(ExceptionMessages.User.UserNotFound, problem.Detail);
            Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
        }

        [Fact]
        public async Task UpdateProfile_Should_Return200_AndUpdateData_WhenRequestIsValid()
        {
            var username = "update_profile_user";
            var password = "Password123!";

            var token = await _authHelper.RegisterAndAuthenticateAsync(username, password);

            var updateDto = new UpdateProfileRequestDTO
            {
                Username = "updated_username",
                Email = "updated@example.com",
                FirstName = "UpdatedFirst",
                LastName = "UpdatedLast",
                Address = "Updated address"
            };

            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PutAsJsonAsync("/api/auth/profile", updateDto);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var profile = await response.Content.ReadFromJsonAsync<UserProfileResponseDTO>();
            Assert.NotNull(profile);

            Assert.Equal(updateDto.Username, profile!.Username);
            Assert.Equal(updateDto.Email, profile.Email);
            Assert.Equal(updateDto.FirstName, profile.FirstName);
            Assert.Equal(updateDto.LastName, profile.LastName);
            Assert.Equal(updateDto.Address, profile.Address);

            using (var scope = _authHelper.Factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userInDb = db.Users.Single(u => u.Username == updateDto.Username);

                Assert.Equal(updateDto.Email, userInDb.Email);
                Assert.Equal(updateDto.FirstName, userInDb.FirstName);
                Assert.Equal(updateDto.LastName, userInDb.LastName);
                Assert.Equal(updateDto.Address, userInDb.Address);
            }
        }
    }
}