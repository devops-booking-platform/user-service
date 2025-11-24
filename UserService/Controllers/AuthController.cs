using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTO;
using UserService.Services.Interfaces;

namespace UserService.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileResponseDTO>> GetProfile()
        {
            var profile = await _authService.GetProfileAsync();
            return Ok(profile);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerRequest)
        {
            await _authService.RegisterAsync(registerRequest);
            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginRequest)
        {
            var jwtToken = await _authService.LoginAsync(loginRequest);
            return Ok(new LoginResponseDTO { Token = jwtToken });
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDTO updateRequest)
        {
            var updatedProfile = await _authService.UpdateProfileAsync(updateRequest);
            return Ok(updatedProfile);
        }

        [Authorize]
        [HttpPut("password")]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequestDTO updatePasswordRequest)
        {
            await _authService.UpdatePasswordAsync(updatePasswordRequest);
            return NoContent();
        }

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> DeleteAccount()
        {
            await _authService.Delete();
            return NoContent();
        }
    }
}
