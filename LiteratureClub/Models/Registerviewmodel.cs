using System.ComponentModel.DataAnnotations;

namespace LiteratureClub.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(50)]
        [Display(Name = "Username")]
        public string DisplayUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student number is required.")]
        [MaxLength(50)]
        [Display(Name = "Student Number")]
        public string StudentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "University email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "University Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [MaxLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your campus.")]
        [Display(Name = "Campus")]
        public int CampusId { get; set; }

        // Populated from DB for the campus dropdown
        public List<CampusOption> Campuses { get; set; } = new();
    }

    public class CampusOption
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty; // "University – Campus"
    }
}