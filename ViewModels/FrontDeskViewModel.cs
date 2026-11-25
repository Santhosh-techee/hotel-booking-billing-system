using HotelBookingSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.ViewModels
{
    public class CheckoutViewModel
    {
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Display(Name = "Payment Mode")]
        public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

        [Range(0, 100000)]
        [Display(Name = "Discount Amount (₹)")]
        public decimal Discount { get; set; } = 0;

        // Computed preview
        public decimal RoomCharges { get; set; }
        public decimal ServiceCharges { get; set; }
        public decimal GstRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public int TotalNights { get; set; }
    }

    public class CheckInViewModel
    {
        public int BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required, MaxLength(50)]
        [Display(Name = "ID Proof Number")]
        public string IdProofNumber { get; set; } = string.Empty;
    }

    public class AddServiceViewModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [Display(Name = "Service")]
        public int ServiceId { get; set; }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;
    }

    public class WalkInBookingViewModel
    {
        [Required]
        [Display(Name = "Room")]
        public int RoomId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Guest Name")]
        public string GuestName { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        [Phone]
        [Display(Name = "Guest Phone")]
        public string GuestPhone { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-In Date")]
        public DateTime CheckInDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-Out Date")]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [Range(1, 10)]
        [Display(Name = "Guest Count")]
        public int GuestCount { get; set; } = 1;

        [Required, MaxLength(50)]
        [Display(Name = "ID Proof Number")]
        public string IdProofNumber { get; set; } = string.Empty;

        public IEnumerable<Room> AvailableRooms { get; set; } = new List<Room>();
    }
}
