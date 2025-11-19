using UserService.DTO;

namespace UserService.Services.Interfaces
{
    public interface IAuthService
    {
        Task<string> LoginAsync(LoginRequestDTO loginRequest);
        Task RegisterAsync(RegisterRequestDTO registerRequest);
    }
}
