using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public enum RequestStatus
    {
        Open,
        Fulfilled,
        Closed
    }

    public class TextbookRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequesterId { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? ISBN { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Author { get; set; }

        [MaxLength(10)]
        public string? Edition { get; set; }

        [MaxLength(500)]
        public string? AdditionalNotes { get; set; }

        public RequestStatus Status { get; set; } = RequestStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ApplicationUser Requester { get; set; } = null!;
    }
}