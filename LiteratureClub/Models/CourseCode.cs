using System.ComponentModel.DataAnnotations;

namespace LiteratureClub.Models
{
    public class CourseCode
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;       // e.g. "CS101"

        [Required]
        [MaxLength(200)]
        public string CourseName { get; set; } = string.Empty;

        public int CampusId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Campus Campus { get; set; } = null!;
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    }
}