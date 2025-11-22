using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.DTO;
using UserService.IntegrationTests.Infrastructure;
using UserService.Repositories.Interfaces;

namespace UserService.IntegrationTests.Helpers
{
    public class AuthenticationHelper(CustomWebApplicationFactory factory)
    {
        private readonly HttpClient _client = factory.CreateClient();

        public CustomWebApplicationFactory Factory => factory;

        /// <summary>
        /// Seeds a user directly into the test database.
        /// </summary>
        public async Task<User> SeedUserAsync(
            string username,
            string password,
            string? email = null!,
            string firstName = "Test",
            string lastName = "User",
            string address = "Address")
        {
            using var scope = factory.Services.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var user = new User
            {
                Username = username,
                Email = $"{username}@example.com",
                FirstName = "Test",
                LastName = "User",
                Address = "Address",
                Role = UserRole.Host,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            };

            await uow.Users.AddAsync(user);
            await uow.SaveChangesAsync();
            return user;
        }

        /// <summary>
        /// Logs in via the /api/auth/login endpoint and returns a valid JWT token.
        /// </summary>
        private async Task<string> LoginAndGetTokenAsync(string username, string password)
        {
            var loginDto = new
            {
                username,
                password
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
            return result!.Token;
        }

        /// <summary>
        /// Seeds a new user and returns a valid JWT token for that user.
        /// </summary>
        public async Task<string> RegisterAndAuthenticateAsync(string username, string password)
        {
            await SeedUserAsync(username, password);
            return await LoginAndGetTokenAsync(username, password);
        }

        /// <summary>
        /// Sends an authenticated HTTP request with the given JWT token.
        /// </summary>
        public async Task<HttpResponseMessage> SendAuthenticatedAsync(
            HttpMethod method,
            string url,
            string token,
            object? content = null)
        {
            var request = new HttpRequestMessage(method, url);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            if (content != null)
            {
                request.Content = JsonContent.Create(content);
            }

            return await _client.SendAsync(request);
        }
    }
}
