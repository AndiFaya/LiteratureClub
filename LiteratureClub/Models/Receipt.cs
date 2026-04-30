using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiteratureClub.Models
{
    public class Receipt
    {
        public int Id { get; set; }

        public int TransactionId { get; set; }

        // Snapshot of details at time of purchase
        [Required]
        [MaxLength(300)]
        public string TextbookTitle { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string TextbookAuthor { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? ISBN { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal AmountPaid { get; set; }

        [Required]
        [MaxLength(200)]
        public string SellerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string BuyerName { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? PickupPointName { get; set; }

        // Set to true once emailed to buyer
        public bool EmailSent { get; set; } = false;

        public DateTime? EmailSentAt { get; set; }

        // Navigation property
        public Transaction Transaction { get; set; } = null!;
    }
}