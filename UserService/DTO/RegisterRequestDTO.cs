using System.ComponentModel.DataAnnotations;
using UserService.Domain.Enums;

namespace UserService.DTO
{
    public class RegisterRequestDTO
    {
        [Required]
        [MinLength(3)]
        public string Username { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength (6)]
        public string Password { get; set; }
        [Required]
        [MinLength(3)]
        public string FirstName { get; set; }
        [Required]
        [MinLength(3)]
        public string LastName { get; set; }
        [Required]
        [MinLength(3)]
        public string Address { get; set; }
        [Required]
        public UserRole Role { get; set; }
    }
}
