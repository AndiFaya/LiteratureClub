using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class SellerReview
    {
        public int Id { get; set; }

        public int TransactionId { get; set; }

        [Required]
        public string ReviewerId { get; set; } = string.Empty;   // Buyer

        [Required]
        public string SellerId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }

        public bool IsFlagged { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Transaction Transaction { get; set; } = null!;
        public ApplicationUser Reviewer { get; set; } = null!;
        public ApplicationUser Seller { get; set; } = null!;
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}