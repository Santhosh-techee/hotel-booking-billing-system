using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models
{
    public class RoomType
    {
        [Key]
        public int RoomTypeId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Room Type Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Base Price (₹/Night)")]
        public decimal BasePrice { get; set; }

        [Required]
        [Display(Name = "Max Guests")]
        [Range(1, 10)]
        public int MaxGuests { get; set; }

        [MaxLength(500)]
        public string Amenities { get; set; } = string.Empty;

        [MaxLength(300)]
        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        // Navigation
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
