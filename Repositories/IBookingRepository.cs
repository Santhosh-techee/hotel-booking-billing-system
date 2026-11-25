using HotelBookingSystem.Models;

namespace HotelBookingSystem.Repositories
{
    public interface IBookingRepository : IRepository<Booking>
    {
        Task<Booking?> GetBookingWithDetailsAsync(int bookingId);
        Task<IEnumerable<Booking>> GetGuestBookingsAsync(string guestId);
        Task<IEnumerable<Booking>> GetActiveBookingsAsync();
        Task<IEnumerable<Booking>> GetAllWithDetailsAsync();
        Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime from, DateTime to);
        Task<int> GetTodayCheckInsCountAsync();
        Task<int> GetActiveGuestsCountAsync();
    }
}
