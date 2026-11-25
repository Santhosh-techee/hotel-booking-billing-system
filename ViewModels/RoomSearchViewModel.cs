using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.ViewModels
{
    public class RoomSearchViewModel
    {
        [Required]
        [Display(Name = "Check-In Date")]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Check-Out Date")]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(1);

        [Required]
        [Range(1, 10)]
        [Display(Name = "Number of Guests")]
        public int GuestCount { get; set; } = 1;
    }
}
