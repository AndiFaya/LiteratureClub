using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public enum ReportTargetType
    {
        Listing,
        SellerReview,
        Message,
        User
    }

    public enum ReportStatus
    {
        Pending,
        UnderReview,
        Resolved,
        Dismissed
    }

    public class Report
    {
        public int Id { get; set; }

        [Required]
        public string ReporterId { get; set; } = string.Empty;

        public ReportTargetType TargetType { get; set; }

        // Only one of these will be set depending on TargetType
        public int? ListingId { get; set; }
        public int? SellerReviewId { get; set; }
        public int? MessageId { get; set; }
        public string? ReportedUserId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Reason { get; set; } = string.Empty;

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        [MaxLength(2000)]
        public string? AdminNotes { get; set; }

        public string? ReviewedByAdminId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        public ApplicationUser Reporter { get; set; } = null!;
        public Listing? Listing { get; set; }
        public SellerReview? SellerReview { get; set; }
        public Message? Message { get; set; }
        public ApplicationUser? ReportedUser { get; set; }
    }
}