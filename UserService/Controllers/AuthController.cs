using Microsoft.AspNetCore.Mvc;
using UserService.DTO;
using UserService.Services.Interfaces;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerRequest)
        {
            await _authService.RegisterAsync(registerRequest);
            return StatusCode(StatusCodes.Status201Created);
        }
    }
}
