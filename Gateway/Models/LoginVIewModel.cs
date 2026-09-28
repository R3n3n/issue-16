using System.ComponentModel.DataAnnotations;

namespace ITElectiveSSO.Models
{
    public class LoginViewModel
    {
        // Ensure form is accessible (labels, aria attributes) - FACTOR
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        // Ensure form is accessible (labels, aria attributes) - FACTOR
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }

        // Display app name if provided in returnUrl - LLORENTE
        public string? AppName { get; set; }


        // Style error messages clearly - CABARDO
        public string? ErrorMessage { get; set; }
    }
}
