using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace LiteratureClub.Models
{
    public class TextbookCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    }
}