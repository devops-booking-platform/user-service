using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using UserService.IntegrationTests.Infrastructure;

namespace UserService.IntegrationTests.Controllers
{
    public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
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

            var response1 = await _client.PostAsJsonAsync("/api/Auth", dto);

            Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

            var response2 = await _client.PostAsJsonAsync("/api/Auth", dto);

            Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);

            var problem = await response2.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal("User with given username or email already exists.", problem!.Detail);
            Assert.Equal((int)HttpStatusCode.Conflict, problem.Status);
        }
    }
}
