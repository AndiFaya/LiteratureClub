using System.ComponentModel.DataAnnotations;

namespace LiteratureClub.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int TransactionId { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string ReceiverId { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public bool IsFlagged { get; set; } = false;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Transaction Transaction { get; set; } = null!;
        public ApplicationUser Sender { get; set; } = null!;
        public ApplicationUser Receiver { get; set; } = null!;
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}