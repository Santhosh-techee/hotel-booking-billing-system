using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Repositories
{
    public class BookingRepository : Repository<Booking>, IBookingRepository
    {
        public BookingRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Booking?> GetBookingWithDetailsAsync(int bookingId) =>
            await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(b => b.Guest)
                .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
                .Include(b => b.Invoice)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

        public async Task<IEnumerable<Booking>> GetGuestBookingsAsync(string guestId) =>
            await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(b => b.Invoice)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Booking>> GetActiveBookingsAsync() =>
            await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(b => b.Guest)
                .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
                .Where(b => b.BookingStatus == BookingStatus.CheckedIn)
                .OrderBy(b => b.CheckInDate)
                .ToListAsync();

        public async Task<IEnumerable<Booking>> GetAllWithDetailsAsync() =>
            await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(b => b.Guest)
                .Include(b => b.Invoice)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

        public async Task<IEnumerable<Booking>> GetBookingsByDateRangeAsync(DateTime from, DateTime to) =>
            await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(b => b.Guest)
                .Include(b => b.Invoice)
                .Where(b => b.CheckInDate >= from && b.CheckInDate <= to)
                .OrderByDescending(b => b.CheckInDate)
                .ToListAsync();

        public async Task<int> GetTodayCheckInsCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Bookings
                .CountAsync(b => b.CheckInDate.Date == today &&
                                 (b.BookingStatus == BookingStatus.Confirmed ||
                                  b.BookingStatus == BookingStatus.CheckedIn));
        }

        public async Task<int> GetActiveGuestsCountAsync() =>
            await _context.Bookings
                .CountAsync(b => b.BookingStatus == BookingStatus.CheckedIn);
    }
}
