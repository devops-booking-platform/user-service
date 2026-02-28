using System.ComponentModel.DataAnnotations;
using UserService.Common.Constants;
using UserService.Domain.Enums;

namespace UserService.DTO
{
    public class RegisterRequestDTO
    {
        [Required]
        [MinLength(ValidationConstants.MinStringLength)]
        [MaxLength(ValidationConstants.MaxStringLength)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(ValidationConstants.MaxStringLength)]
        public string Email { get; set; }

        [Required]
        [MinLength(ValidationConstants.MinPasswordLength)]
        [MaxLength(ValidationConstants.MaxStringLength)]
        public string Password { get; set; }

        [Required]
        [MinLength(ValidationConstants.MinStringLength)]
        [MaxLength(ValidationConstants.MaxStringLength)]
        public string FirstName { get; set; }

        [Required]
        [MinLength(ValidationConstants.MinStringLength)]
        [MaxLength(ValidationConstants.MaxStringLength)]
        public string LastName { get; set; }

        [Required]
        [MinLength(ValidationConstants.MinStringLength)]
        [MaxLength(ValidationConstants.MaxStringLength)]
        public string Address { get; set; }

        [Required]
        public UserRole Role { get; set; }
    }
}
