using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSwap.Models
{
    public enum BidStatus
    {
        Pending,
        Accepted,
        Rejected,
        Expired,
        Withdrawn
    }

    public class Bid
    {
        public int Id { get; set; }

        public int ListingId { get; set; }

        [Required]
        public string BidderId { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal OfferAmount { get; set; }

        public int Quantity { get; set; } = 1;

        [MaxLength(500)]
        public string? Message { get; set; }

        public BidStatus Status { get; set; } = BidStatus.Pending;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Listing Listing { get; set; } = null!;
        public ApplicationUser Bidder { get; set; } = null!;
    }
}