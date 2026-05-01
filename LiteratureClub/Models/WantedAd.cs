using System.ComponentModel.DataAnnotations;

namespace LiteratureClub.Models
{
    
       public enum WantedAdStatus
        {
            Open,      
            Fulfilled,  
            Closed     
        }

        public enum PreferredCondition
        {
            Any,
            New,
            Used
        }

       
        public enum PreferredFormat
        {
            Any,
            Hardcover,
            Paperback,
            SpiralBound,
            RingBound,
            Other
        }

        public class WantedAd
        {
            public int Id { get; set; }

            [Required]
            public string RequesterId { get; set; } = string.Empty;

            

            [MaxLength(20)]
            [Display(Name = "ISBN")]
            public string? ISBN { get; set; }

            [Required]
            [MaxLength(300)]
            [Display(Name = "Book Title")]
            public string Title { get; set; } = string.Empty;

            [Required]
            [MaxLength(200)]
            [Display(Name = "Author(s)")]
            public string Author { get; set; } = string.Empty;

            [MaxLength(10)]
            [Display(Name = "Edition")]
            public string? Edition { get; set; }

            [Required]
            [Range(1900, 2100)]
            [Display(Name = "Publication Year")]
            public int PublicationYear { get; set; } = DateTime.Now.Year;

            [Required]
            [MaxLength(200)]
            [Display(Name = "Publisher")]
            public string Publisher { get; set; } = string.Empty;

           
            [Display(Name = "Preferred Condition")]
            public PreferredCondition PreferredCondition { get; set; } = PreferredCondition.Any;

            [Display(Name = "Preferred Format")]
            public PreferredFormat PreferredFormat { get; set; } = PreferredFormat.Any;

            [Required]
            [Display(Name = "Category")]
            public int CategoryId { get; set; }

            [Required]
            [Display(Name = "Course Code")]
            public int CourseCodeId { get; set; }

            [MaxLength(1000)]
            [Display(Name = "Additional Notes")]
            public string? AdditionalNotes { get; set; }

           

            public WantedAdStatus Status { get; set; } = WantedAdStatus.Open;

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }

            

            public ApplicationUser Requester { get; set; } = null!;
            public TextbookCategory Category { get; set; } = null!;
            public CourseCode CourseCode { get; set; } = null!;
        }
    }
