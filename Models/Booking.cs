using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        CheckedIn,
        CheckedOut,
        Cancelled
    }

    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required, MaxLength(20)]
        [Display(Name = "Booking Reference")]
        public string BookingRef { get; set; } = string.Empty;

        [Required]
        public string GuestId { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        [Display(Name = "Guest Name")]
        public string GuestName { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        [Display(Name = "Guest Phone")]
        [Phone]
        public string GuestPhone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [Required]
        [Display(Name = "Check-In Date")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required]
        [Display(Name = "Check-Out Date")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Display(Name = "Number of Guests")]
        [Range(1, 10)]
        public int GuestCount { get; set; }

        [Required]
        [Display(Name = "Booking Status")]
        public BookingStatus BookingStatus { get; set; } = BookingStatus.Pending;

        [MaxLength(50)]
        [Display(Name = "ID Proof Number")]
        public string? IdProofNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("GuestId")]
        public ApplicationUser Guest { get; set; } = null!;

        [ForeignKey("RoomId")]
        public Room Room { get; set; } = null!;

        public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
        public Invoice? Invoice { get; set; }
    }
}
