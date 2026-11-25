using HotelBookingSystem.Models;

namespace HotelBookingSystem.Services
{
    public interface IRoomService
    {
        Task<IEnumerable<Room>> SearchAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int guestCount);
        Task<IEnumerable<Room>> GetAllRoomsWithTypeAsync();
        Task<Room?> GetRoomWithTypeAsync(int roomId);
        Task UpdateRoomStatusAsync(int roomId, RoomStatus status);
        Task<IEnumerable<RoomType>> GetAllRoomTypesAsync();
        Task<RoomType?> GetRoomTypeByIdAsync(int id);
        Task CreateRoomTypeAsync(RoomType roomType);
        Task UpdateRoomTypeAsync(RoomType roomType);
        Task DeleteRoomTypeAsync(int id);
        Task CreateRoomAsync(Room room);
        Task UpdateRoomAsync(Room room);
        Task DeleteRoomAsync(int id);
        Task<decimal> GetOccupancyRateAsync();
    }
}
