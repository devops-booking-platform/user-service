using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StackExchange.Redis;
using UserService.Common.Events;
using UserService.Data;
using UserService.Infrastructure.Clients;

namespace UserService.IntegrationTests.Infrastructure
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("UserServiceTestDb"));
                services.RemoveAll<IEventBus>();
                services.AddSingleton<IEventBus, NoOpEventBus>();
				services.RemoveAll<IReservationClient>();
                services.AddSingleton<IReservationClient>(new FakeReservationClient(eligible: true));

                // Remove real Redis and add mock
                services.RemoveAll<IConnectionMultiplexer>();
                var mockRedisDb = new Mock<IDatabase>();
                mockRedisDb.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                    .ReturnsAsync(false);
                mockRedisDb.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), 
                    It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                    .ReturnsAsync(true);

                var mockRedis = new Mock<IConnectionMultiplexer>();
                mockRedis.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                    .Returns(mockRedisDb.Object);
                
                services.AddSingleton<IConnectionMultiplexer>(mockRedis.Object);

				var serviceProvider = services.BuildServiceProvider();

                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
            });
        }
    }
}
