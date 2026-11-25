using HotelBookingSystem.Models;

namespace HotelBookingSystem.Repositories
{
    public interface IRoomRepository : IRepository<Room>
    {
        Task<IEnumerable<Room>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int guestCount);
        Task<IEnumerable<Room>> GetAllWithTypeAsync();
        Task<Room?> GetRoomWithTypeAsync(int roomId);
    }
}
