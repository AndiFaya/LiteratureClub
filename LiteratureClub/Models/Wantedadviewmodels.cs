using LiteratureClub.Models;
using LiteratureClub.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace LiteratureClub.ViewModels
{
    //Create / Edit form
    public class WantedAdFormViewModel
    {
        public int Id { get; set; }

        [MaxLength(20)]
        [Display(Name = "ISBN")]
        public string? ISBN { get; set; }

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

        [Required(ErrorMessage = "Please select a category.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please select a course code.")]
        [Display(Name = "Course Code")]
        public int CourseCodeId { get; set; }

        [Display(Name = "Preferred Condition")]
        public PreferredCondition PreferredCondition { get; set; } = PreferredCondition.Any;

        [Display(Name = "Preferred Format")]
        public PreferredFormat PreferredFormat { get; set; } = PreferredFormat.Any;

        [MaxLength(1000)]
        [Display(Name = "Additional Notes")]
        public string? AdditionalNotes { get; set; }

        // Dropdown data
        public List<DropdownOption> Categories { get; set; } = new();
        public List<DropdownOption> CourseCodes { get; set; } = new();
    }

    //Browse
    public class WantedAdCardViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? Edition { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string RequesterUsername { get; set; } = string.Empty;
        public string RequesterCampus { get; set; } = string.Empty;
        public PreferredCondition PreferredCondition { get; set; }
        public PreferredFormat PreferredFormat { get; set; }
        public WantedAdStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    //Index page
    public class WantedAdIndexViewModel
    {
        public List<WantedAdCardViewModel> Ads { get; set; } = new();

        // Filters
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public int? CourseCodeId { get; set; }

        // Dropdown data
        public List<DropdownOption> Categories { get; set; } = new();
        public List<DropdownOption> CourseCodes { get; set; } = new();
    }

    //Detail page
    public class WantedAdDetailViewModel
    {
        public int Id { get; set; }
        public string? ISBN { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string? Edition { get; set; }
        public int PublicationYear { get; set; }
        public string Publisher { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public PreferredCondition PreferredCondition { get; set; }
        public PreferredFormat PreferredFormat { get; set; }
        public string? AdditionalNotes { get; set; }
        public WantedAdStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // Requester info
        public string RequesterId { get; set; } = string.Empty;
        public string RequesterUsername { get; set; } = string.Empty;
        public string RequesterCampus { get; set; } = string.Empty;

        // Viewer context
        public bool IsOwnAd { get; set; }

        // Matching listings found automatically
        public List<WantedAdMatchViewModel> MatchingListings { get; set; } = new();
    }

    // Matching listing suggestion shown on detail page
    public class WantedAdMatchViewModel
    {
        public int ListingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public BookCondition Condition { get; set; }
        public string SellerUsername { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
    }

    // Dashboard row
    public class DashboardWantedAdRow
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public WantedAdStatus Status { get; set; }
        public int Matches { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
