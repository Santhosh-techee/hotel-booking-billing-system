using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models
{
    public enum RoomStatus
    {
        Available,
        Booked,
        Occupied,
        Cleaning,
        Maintenance
    }

    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        [Required, MaxLength(10)]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Room Type")]
        public int RoomTypeId { get; set; }

        [Required]
        [Range(1, 50)]
        public int Floor { get; set; }

        [Required]
        public RoomStatus Status { get; set; } = RoomStatus.Available;

        // Navigation
        [ForeignKey("RoomTypeId")]
        public RoomType RoomType { get; set; } = null!;

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
