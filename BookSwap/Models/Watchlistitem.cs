using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class WatchlistItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int ListingId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public Listing Listing { get; set; } = null!;
    }
}