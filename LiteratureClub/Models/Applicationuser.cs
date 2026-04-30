using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Cryptography;

namespace LiteratureClub.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string StudentNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DisplayUsername { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        public int CampusId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Earnings balance available for withdrawal
        public decimal EarningsBalance { get; set; } = 0m;

        // Navigation properties
        public Campus Campus { get; set; } = null!;

        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public ICollection<Transaction> Purchases { get; set; } = new List<Transaction>();
        public ICollection<Transaction> Sales { get; set; } = new List<Transaction>();
        public ICollection<SellerReview> ReviewsGiven { get; set; } = new List<SellerReview>();
        public ICollection<SellerReview> ReviewsReceived { get; set; } = new List<SellerReview>();
        public ICollection<Message> MessagesSent { get; set; } = new List<Message>();
        public ICollection<Message> MessagesReceived { get; set; } = new List<Message>();
        public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();
        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
        public ICollection<Report> ReportsSubmitted { get; set; } = new List<Report>();
        public ICollection<TextbookRequest> TextbookRequests { get; set; } = new List<TextbookRequest>();
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
        public ICollection<PickupPointReview> PickupPointReviews { get; set; } = new List<PickupPointReview>();
    }
}