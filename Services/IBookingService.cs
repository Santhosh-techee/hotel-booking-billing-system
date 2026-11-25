using HotelBookingSystem.Models;
using HotelBookingSystem.ViewModels;
using ModelBookingService = HotelBookingSystem.Models.BookingService;

namespace HotelBookingSystem.Services
{
    public interface IBookingService
    {
        Task<Booking> CreateBookingAsync(CreateBookingViewModel vm, string guestId);
        Task<Booking> CreateWalkInBookingAsync(WalkInBookingViewModel vm, string staffUserId);
        Task<Booking?> GetBookingWithDetailsAsync(int bookingId);
        Task<IEnumerable<Booking>> GetGuestBookingsAsync(string guestId);
        Task<IEnumerable<Booking>> GetAllBookingsAsync();
        Task CheckInAsync(int bookingId, string idProofNumber);
        Task CancelBookingAsync(int bookingId, string requestingUserId, bool isStaff);
        Task AddServiceAsync(int bookingId, int serviceId, int quantity);
        Task RemoveServiceAsync(int bookingServiceId);
        Task<IEnumerable<Service>> GetActiveServicesAsync();
        Task<BookingServiceSummary> GetServiceSummaryAsync(int bookingId);
        Task<IEnumerable<Booking>> GetAllWithDetailsAsync();
    }

    public class BookingServiceSummary
    {
        public List<ModelBookingService> Services { get; set; } = new();
        public decimal ServiceTotal { get; set; }
        public decimal RoomTotal { get; set; }
        public decimal GrandEstimate { get; set; }
    }
}
