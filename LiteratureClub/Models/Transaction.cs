using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiteratureClub.Models
{
    public enum TransactionStatus
    {
        PaymentPending,
        PaymentConfirmed,
        AwaitingPickup,
        ExchangeVerified,
        Completed,
        Refunded,
        Disputed
    }

    public class Transaction
    {
        public int Id { get; set; }

        public int ListingId { get; set; }

        [Required]
        public string BuyerId { get; set; } = string.Empty;

        [Required]
        public string SellerId { get; set; } = string.Empty;

        public int? PickupPointId { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public TransactionStatus Status { get; set; } = TransactionStatus.PaymentPending;

        // Payment gateway reference
        [MaxLength(200)]
        public string? PaymentReference { get; set; }

        // Verification code sent to buyer; seller inputs to confirm exchange
        [MaxLength(10)]
        public string? VerificationCode { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Listing Listing { get; set; } = null!;
        public ApplicationUser Buyer { get; set; } = null!;
        public ApplicationUser Seller { get; set; } = null!;
        public PickupPoint? PickupPoint { get; set; }
        public Receipt? Receipt { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<SellerReview> SellerReviews { get; set; } = new List<SellerReview>();
    }
}