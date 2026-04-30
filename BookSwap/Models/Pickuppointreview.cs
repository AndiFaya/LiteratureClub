using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class PickupPointReview
    {
        public int Id { get; set; }

        public int PickupPointId { get; set; }

        [Required]
        public string ReviewerId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public bool IsFlagged { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public PickupPoint PickupPoint { get; set; } = null!;
        public ApplicationUser Reviewer { get; set; } = null!;
    }
}