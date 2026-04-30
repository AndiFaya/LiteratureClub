using System.ComponentModel.DataAnnotations;

namespace BookSwap.Models
{
    public class PickupPoint
    {
        public int Id { get; set; }

        public int CampusId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;      // e.g. "Main Library Entrance"

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Campus Campus { get; set; } = null!;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<PickupPointReview> Reviews { get; set; } = new List<PickupPointReview>();
    }
}