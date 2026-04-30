using System.ComponentModel.DataAnnotations;

namespace LiteratureClub.Models
{
    public class Campus
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string University { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<PickupPoint> PickupPoints { get; set; } = new List<PickupPoint>();
        public ICollection<CourseCode> CourseCodes { get; set; } = new List<CourseCode>();
    }
}