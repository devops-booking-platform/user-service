using UserService.DTO;

namespace UserService.Services.Interfaces
{
    public interface IAuthService
    {
        public Task RegisterAsync(RegisterRequestDTO registerRequest);
    }
}
