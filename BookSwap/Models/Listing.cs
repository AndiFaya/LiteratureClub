using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace BookSwap.Models
{
    public enum ListingStatus
    {
        Available,
        UnderOffer,
        Sold,
        Removed
    }

    public enum BookCondition
    {
        New,
        Used
    }

    public enum BookFormat
    {
        Hardcover,
        Paperback,
        SpiralBound,
        RingBound,
        Other
    }

    public class Listing
    {
        public int Id { get; set; }

        [Required]
        public string SellerId { get; set; } = string.Empty;

        // Textbook details
        [Required]
        [MaxLength(20)]
        public string ISBN { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Author { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Edition { get; set; }

        [Required]
        [Range(1900, 2100)]
        public int PublicationYear { get; set; }

        [Required]
        [MaxLength(200)]
        public string Publisher { get; set; } = string.Empty;

        public BookCondition Condition { get; set; }

        [MaxLength(1000)]
        public string? ConditionDescription { get; set; }  // Required when Condition = Used

        public BookFormat Format { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int CategoryId { get; set; }

        public int CourseCodeId { get; set; }

        // Listing state
        public ListingStatus Status { get; set; } = ListingStatus.Available;

        // Bidding / auction fields
        public bool IsOpenForBidding { get; set; } = false;

        public DateTime? BidExpiresAt { get; set; }

        // Image
        [MaxLength(500)]
        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ApplicationUser Seller { get; set; } = null!;
        public TextbookCategory Category { get; set; } = null!;
        public CourseCode CourseCode { get; set; } = null!;
        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
        public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}