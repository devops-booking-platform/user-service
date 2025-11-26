using System.ComponentModel.DataAnnotations;
using UserService.Common.Constants;

namespace UserService.DTO
{
    public class UpdatePasswordRequestDTO
    {
        [Required]
        [MinLength(ValidationConstants.MinPasswordLength)]
        [MaxLength(ValidationConstants.MaxStringLength)]
        public string CurrentPassword { get; set; }

        [Required]
        [MinLength(ValidationConstants.MinPasswordLength)]
        [MaxLength(ValidationConstants.MaxStringLength)]
        public string NewPassword { get; set; }
    }
}
