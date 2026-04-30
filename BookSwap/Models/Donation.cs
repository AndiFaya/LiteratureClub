using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookSwap.Models
{
    public enum DonationStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public class Donation
    {
        public int Id { get; set; }

        public string? DonorId { get; set; }  // Null if anonymous

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        public DonationStatus Status { get; set; } = DonationStatus.Pending;

        [MaxLength(200)]
        public string? PaymentReference { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; }

        public bool IsAnonymous { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ApplicationUser? Donor { get; set; }
    }
}