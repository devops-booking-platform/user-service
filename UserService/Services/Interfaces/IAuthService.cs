using UserService.DTO;

namespace UserService.Services.Interfaces
{
    public interface IAuthService
    {
        Task<UserProfileResponseDTO> GetProfileAsync();
        Task<UserProfileResponseDTO> UpdateProfileAsync(UpdateProfileRequestDTO updateRequest);
        Task<string> LoginAsync(LoginRequestDTO loginRequest);
        Task RegisterAsync(RegisterRequestDTO registerRequest);
        Task Delete(CancellationToken ct = default);
        Task UpdatePasswordAsync(UpdatePasswordRequestDTO updatePasswordRequest);
    }
}
