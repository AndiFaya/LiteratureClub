using LiteratureClub.Models;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace LiteratureClub.ViewModels
{
    public class ListingFormViewModel
    {
        public int Id { get; set; } // 0 for Create, >0 for Edit

        [Required(ErrorMessage = "ISBN is required.")]
        [MaxLength(20)]
        [Display(Name = "ISBN")]
        public string ISBN { get; set; } = string.Empty;

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(300)]
        [Display(Name = "Book Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required.")]
        [MaxLength(200)]
        [Display(Name = "Author(s)")]
        public string Author { get; set; } = string.Empty;

        [MaxLength(10)]
        [Display(Name = "Edition")]
        public string? Edition { get; set; }

        [Required(ErrorMessage = "Publication year is required.")]
        [Range(1900, 2100, ErrorMessage = "Enter a valid year.")]
        [Display(Name = "Publication Year")]
        public int PublicationYear { get; set; } = DateTime.Now.Year;

        [Required(ErrorMessage = "Publisher is required.")]
        [MaxLength(200)]
        [Display(Name = "Publisher")]
        public string Publisher { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a condition.")]
        [Display(Name = "Condition")]
        public BookCondition Condition { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Condition Description")]
        public string? ConditionDescription { get; set; }

        [Required(ErrorMessage = "Please select a format.")]
        [Display(Name = "Format")]
        public BookFormat Format { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 99999.99, ErrorMessage = "Enter a valid price.")]
        [Display(Name = "Asking Price (R)")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please select a course code.")]
        [Display(Name = "Course Code")]
        public int CourseCodeId { get; set; }

        [Display(Name = "Open for Bidding?")]
        public bool IsOpenForBidding { get; set; } = false;

        [Display(Name = "Bid Expiry Date")]
        [DataType(DataType.DateTime)]
        public DateTime? BidExpiresAt { get; set; }

        [Display(Name = "Book Cover Image")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImagePath { get; set; } // Used in Edit to show current image

        // Dropdowns
        public List<DropdownOption> Categories { get; set; } = new();
        public List<DropdownOption> CourseCodes { get; set; } = new();
    }

    public class DropdownOption
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    // ── Browse / Index ─────────────────────────────────────────────────────
    public class ListingIndexViewModel
    {
        public List<ListingCardViewModel> Listings { get; set; } = new();

        // Filter state (preserved across searches)
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? CourseCodeId { get; set; }
        public BookCondition? Condition { get; set; }
        public decimal? MaxPrice { get; set; }
        public string SortBy { get; set; } = "newest";

        // Dropdown data
        public List<DropdownOption> Categories { get; set; } = new();
        public List<DropdownOption> CourseCodes { get; set; } = new();
    }

    public class ListingCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? Edition { get; set; }
        public decimal Price { get; set; }
        public BookCondition Condition { get; set; }
        public BookFormat Format { get; set; }
        public string? ImagePath { get; set; }
        public string SellerUsername { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public bool IsOpenForBidding { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Detail ─────────────────────────────────────────────────────────────
    public class ListingDetailViewModel
    {
        public int Id { get; set; }
        public string ISBN { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? Edition { get; set; }
        public int PublicationYear { get; set; }
        public string Publisher { get; set; } = string.Empty;
        public BookCondition Condition { get; set; }
        public string? ConditionDescription { get; set; }
        public BookFormat Format { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public bool IsOpenForBidding { get; set; }
        public DateTime? BidExpiresAt { get; set; }
        public ListingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Seller info
        public string SellerId { get; set; } = string.Empty;
        public string SellerUsername { get; set; } = string.Empty;
        public string SellerCampus { get; set; } = string.Empty;
        public double SellerRating { get; set; }
        public int SellerReviewCount { get; set; }

        // Viewer context
        public bool IsOwnListing { get; set; }
        public bool IsWatchlisted { get; set; }

        // Bids on this listing (visible to seller)
        public List<BidSummaryViewModel> Bids { get; set; } = new();
    }

    public class BidSummaryViewModel
    {
        public int Id { get; set; }
        public string BidderUsername { get; set; } = string.Empty;
        public decimal OfferAmount { get; set; }
        public int Quantity { get; set; }
        public string? Message { get; set; }
        public BidStatus Status { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}