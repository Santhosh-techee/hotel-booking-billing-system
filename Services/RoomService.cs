using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Services
{
    public class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepo;
        private readonly ApplicationDbContext _context;

        public RoomService(IRoomRepository roomRepo, ApplicationDbContext context)
        {
            _roomRepo = roomRepo;
            _context = context;
        }

        public async Task<IEnumerable<Room>> SearchAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int guestCount)
            => await _roomRepo.GetAvailableRoomsAsync(checkIn, checkOut, guestCount);

        public async Task<IEnumerable<Room>> GetAllRoomsWithTypeAsync()
            => await _roomRepo.GetAllWithTypeAsync();

        public async Task<Room?> GetRoomWithTypeAsync(int roomId)
            => await _roomRepo.GetRoomWithTypeAsync(roomId);

        public async Task UpdateRoomStatusAsync(int roomId, RoomStatus status)
        {
            var room = await _roomRepo.GetByIdAsync(roomId)
                       ?? throw new InvalidOperationException($"Room {roomId} not found.");
            room.Status = status;
            await _roomRepo.UpdateAsync(room);
            await _roomRepo.SaveChangesAsync();
        }

        public async Task<IEnumerable<RoomType>> GetAllRoomTypesAsync()
            => await _context.RoomTypes.OrderBy(rt => rt.BasePrice).ToListAsync();

        public async Task<RoomType?> GetRoomTypeByIdAsync(int id)
            => await _context.RoomTypes.FindAsync(id);

        public async Task CreateRoomTypeAsync(RoomType roomType)
        {
            _context.RoomTypes.Add(roomType);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRoomTypeAsync(RoomType roomType)
        {
            _context.RoomTypes.Update(roomType);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRoomTypeAsync(int id)
        {
            var rt = await _context.RoomTypes.FindAsync(id)
                     ?? throw new InvalidOperationException("Room type not found.");
            _context.RoomTypes.Remove(rt);
            await _context.SaveChangesAsync();
        }

        public async Task CreateRoomAsync(Room room)
        {
            await _roomRepo.AddAsync(room);
            await _roomRepo.SaveChangesAsync();
        }

        public async Task UpdateRoomAsync(Room room)
        {
            await _roomRepo.UpdateAsync(room);
            await _roomRepo.SaveChangesAsync();
        }

        public async Task DeleteRoomAsync(int id)
        {
            var room = await _roomRepo.GetByIdAsync(id)
                       ?? throw new InvalidOperationException("Room not found.");
            await _roomRepo.DeleteAsync(room);
            await _roomRepo.SaveChangesAsync();
        }

        public async Task<decimal> GetOccupancyRateAsync()
        {
            var totalRooms = await _context.Rooms.CountAsync();
            if (totalRooms == 0) return 0;
            var occupiedRooms = await _context.Rooms
                .CountAsync(r => r.Status == RoomStatus.Occupied || r.Status == RoomStatus.Booked);
            return Math.Round((decimal)occupiedRooms / totalRooms * 100, 1);
        }
    }
}
