using HotelBookingSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.ViewModels
{
    public class CreateBookingViewModel
    {
        [Required]
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
        [Range(1, 10)]
        [Display(Name = "Number of Guests")]
        public int GuestCount { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Full Name")]
        public string GuestName { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        [Display(Name = "Phone Number")]
        [Phone]
        public string GuestPhone { get; set; } = string.Empty;

        // For display only
        public Room? SelectedRoom { get; set; }
        public int TotalNights { get; set; }
        public decimal EstimatedTotal { get; set; }
    }

    public class RoomResultsViewModel
    {
        public RoomSearchViewModel SearchCriteria { get; set; } = new();
        public IEnumerable<Room> AvailableRooms { get; set; } = new List<Room>();
    }
}
