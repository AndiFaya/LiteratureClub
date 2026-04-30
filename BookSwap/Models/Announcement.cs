using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        public string PostedByAdminId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(5000)]
        public string Body { get; set; } = string.Empty;

        public bool IsPublished { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        // Navigation property
        public ApplicationUser PostedByAdmin { get; set; } = null!;
    }
}