using System.ComponentModel.DataAnnotations;
using UserService.Common.Constants;

namespace UserService.DTO
{
    public class UpdateProfileRequestDTO
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
    }
}
