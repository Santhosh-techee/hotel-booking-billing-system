using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Repositories
{
    public class RoomRepository : Repository<Room>, IRoomRepository
    {
        public RoomRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int guestCount)
        {
            // Get room IDs that are booked during the requested period (strict overlap)
            var bookedRoomIds = await _context.Bookings
                .Where(b =>
                    b.BookingStatus != BookingStatus.Cancelled &&
                    b.BookingStatus != BookingStatus.CheckedOut &&
                    b.CheckInDate < checkOut &&
                    b.CheckOutDate > checkIn)
                .Select(b => b.RoomId)
                .Distinct()
                .ToListAsync();

            return await _context.Rooms
                .Include(r => r.RoomType)
                .Where(r =>
                    r.Status != RoomStatus.Maintenance &&
                    r.RoomType.MaxGuests >= guestCount &&
                    !bookedRoomIds.Contains(r.RoomId))
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();
        }

        public async Task<IEnumerable<Room>> GetAllWithTypeAsync() =>
            await _context.Rooms
                .Include(r => r.RoomType)
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

        public async Task<Room?> GetRoomWithTypeAsync(int roomId) =>
            await _context.Rooms
                .Include(r => r.RoomType)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);
    }
}
