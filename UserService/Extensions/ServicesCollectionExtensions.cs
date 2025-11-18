using UserService.Repositories.Implementations;
using UserService.Repositories.Interfaces;
using UserService.Services.Implementations;
using UserService.Services.Interfaces;

namespace UserService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddUserServiceDependencies(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthService, AuthService>();
            return services;
        }
    }
}
