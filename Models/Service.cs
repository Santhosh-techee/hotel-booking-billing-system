using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required, MaxLength(100)]
        [Display(Name = "Service Name")]
        public string ServiceName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Price (₹)")]
        public decimal Price { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
    }
}
