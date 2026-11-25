using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.Repositories;
using HotelBookingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using ModelBookingService = HotelBookingSystem.Models.BookingService;

namespace HotelBookingSystem.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly ApplicationDbContext _context;

        public BookingService(IBookingRepository bookingRepo, IRoomRepository roomRepo, ApplicationDbContext context)
        {
            _bookingRepo = bookingRepo;
            _roomRepo = roomRepo;
            _context = context;
        }

        public async Task<Booking> CreateBookingAsync(CreateBookingViewModel vm, string guestId)
        {
            if (vm.CheckOutDate.Date <= vm.CheckInDate.Date)
                throw new InvalidOperationException("Check-out date must be after check-in date.");

            var room = await _roomRepo.GetRoomWithTypeAsync(vm.RoomId)
                       ?? throw new InvalidOperationException("Room not found.");

            if (room.Status == RoomStatus.Maintenance)
                throw new InvalidOperationException("This room is currently under maintenance.");

            if (vm.GuestCount > room.RoomType.MaxGuests)
                throw new InvalidOperationException($"Room {room.RoomNumber} accommodates a maximum of {room.RoomType.MaxGuests} guests.");

            // Strict date-overlap double-booking check (allowing same-day turnover)
            var hasOverlap = await _context.Bookings.AnyAsync(b =>
                b.RoomId == vm.RoomId &&
                b.BookingStatus != BookingStatus.Cancelled &&
                b.BookingStatus != BookingStatus.CheckedOut &&
                vm.CheckInDate < b.CheckOutDate &&
                vm.CheckOutDate > b.CheckInDate);

            if (hasOverlap)
                throw new InvalidOperationException("This room is already booked for the selected dates.");

            var bookingRef = await GenerateUniqueBookingRefAsync();

            var booking = new Booking
            {
                BookingRef = bookingRef,
                GuestId = guestId,
                GuestName = vm.GuestName,
                GuestPhone = vm.GuestPhone,
                RoomId = vm.RoomId,
                CheckInDate = vm.CheckInDate,
                CheckOutDate = vm.CheckOutDate,
                GuestCount = vm.GuestCount,
                BookingStatus = BookingStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            if (room.Status == RoomStatus.Available)
            {
                room.Status = RoomStatus.Booked;
                await _roomRepo.UpdateAsync(room);
            }

            await _bookingRepo.AddAsync(booking);
            await _bookingRepo.SaveChangesAsync();
            return booking;
        }

        public async Task<Booking> CreateWalkInBookingAsync(WalkInBookingViewModel vm, string staffUserId)
        {
            if (vm.CheckOutDate.Date <= vm.CheckInDate.Date)
                throw new InvalidOperationException("Check-out date must be after check-in date.");

            var room = await _roomRepo.GetRoomWithTypeAsync(vm.RoomId)
                       ?? throw new InvalidOperationException("Room not found.");

            if (room.Status == RoomStatus.Maintenance || room.Status == RoomStatus.Occupied)
                throw new InvalidOperationException($"Room {room.RoomNumber} is currently {room.Status} and cannot be checked into.");

            if (vm.GuestCount > room.RoomType.MaxGuests)
                throw new InvalidOperationException($"Room {room.RoomNumber} accommodates a maximum of {room.RoomType.MaxGuests} guests.");

            var hasOverlap = await _context.Bookings.AnyAsync(b =>
                b.RoomId == vm.RoomId &&
                b.BookingStatus != BookingStatus.Cancelled &&
                b.BookingStatus != BookingStatus.CheckedOut &&
                vm.CheckInDate < b.CheckOutDate &&
                vm.CheckOutDate > b.CheckInDate);

            if (hasOverlap)
                throw new InvalidOperationException("This room already has an overlapping booking for the selected dates.");

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bookingRef = await GenerateUniqueBookingRefAsync();

                var booking = new Booking
                {
                    BookingRef = bookingRef,
                    GuestId = staffUserId,
                    GuestName = vm.GuestName,
                    GuestPhone = vm.GuestPhone,
                    RoomId = vm.RoomId,
                    CheckInDate = vm.CheckInDate,
                    CheckOutDate = vm.CheckOutDate,
                    GuestCount = vm.GuestCount,
                    IdProofNumber = vm.IdProofNumber,
                    BookingStatus = BookingStatus.CheckedIn,
                    CreatedAt = DateTime.UtcNow
                };

                room.Status = RoomStatus.Occupied;
                _context.Rooms.Update(room);
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return booking;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<string> GenerateUniqueBookingRefAsync()
        {
            var today = DateTime.UtcNow;
            var prefix = $"BK-{today:yyyyMMdd}-";
            var existingRefs = await _context.Bookings
                .Where(b => b.BookingRef.StartsWith(prefix))
                .Select(b => b.BookingRef)
                .ToListAsync();

            var maxSeq = 0;
            foreach (var r in existingRefs)
            {
                var suffix = r.Substring(prefix.Length);
                if (int.TryParse(suffix, out var num) && num > maxSeq)
                    maxSeq = num;
            }
            return $"{prefix}{(maxSeq + 1):D3}";
        }

        public async Task<Booking?> GetBookingWithDetailsAsync(int bookingId)
            => await _bookingRepo.GetBookingWithDetailsAsync(bookingId);

        public async Task<IEnumerable<Booking>> GetGuestBookingsAsync(string guestId)
            => await _bookingRepo.GetGuestBookingsAsync(guestId);

        public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
            => await _bookingRepo.GetAllWithDetailsAsync();

        public async Task<IEnumerable<Booking>> GetAllWithDetailsAsync()
            => await _bookingRepo.GetAllWithDetailsAsync();

        public async Task CheckInAsync(int bookingId, string idProofNumber)
        {
            var booking = await _bookingRepo.GetBookingWithDetailsAsync(bookingId)
                          ?? throw new InvalidOperationException("Booking not found.");

            if (booking.BookingStatus != BookingStatus.Confirmed && booking.BookingStatus != BookingStatus.Pending)
                throw new InvalidOperationException($"Booking {booking.BookingRef} cannot be checked in from status {booking.BookingStatus}.");

            booking.BookingStatus = BookingStatus.CheckedIn;
            booking.IdProofNumber = idProofNumber;
            booking.Room.Status = RoomStatus.Occupied;

            await _bookingRepo.UpdateAsync(booking);
            await _bookingRepo.SaveChangesAsync();
        }

        public async Task CancelBookingAsync(int bookingId, string requestingUserId, bool isStaff)
        {
            var booking = await _bookingRepo.GetBookingWithDetailsAsync(bookingId)
                          ?? throw new InvalidOperationException("Booking not found.");

            if (!isStaff && booking.GuestId != requestingUserId)
                throw new UnauthorizedAccessException("Not authorized to cancel this booking.");

            if (booking.BookingStatus == BookingStatus.CheckedIn ||
                booking.BookingStatus == BookingStatus.CheckedOut)
                throw new InvalidOperationException("Cannot cancel an in-progress or completed booking.");

            booking.BookingStatus = BookingStatus.Cancelled;
            if (booking.Room.Status == RoomStatus.Booked)
            {
                var hasOtherConfirmed = await _context.Bookings.AnyAsync(b =>
                    b.RoomId == booking.RoomId &&
                    b.BookingId != booking.BookingId &&
                    (b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.Pending));
                if (!hasOtherConfirmed)
                    booking.Room.Status = RoomStatus.Available;
            }

            await _bookingRepo.UpdateAsync(booking);
            await _bookingRepo.SaveChangesAsync();
        }

        public async Task AddServiceAsync(int bookingId, int serviceId, int quantity)
        {
            if (quantity < 1)
                throw new InvalidOperationException("Quantity must be at least 1.");

            var booking = await _context.Bookings.FindAsync(bookingId)
                          ?? throw new InvalidOperationException("Booking not found.");
            if (booking.BookingStatus != BookingStatus.CheckedIn)
                throw new InvalidOperationException("Services can only be added to a checked-in stay.");

            var service = await _context.Services.FindAsync(serviceId)
                          ?? throw new InvalidOperationException("Service not found.");

            var bs = new Models.BookingService
            {
                BookingId = bookingId,
                ServiceId = serviceId,
                Quantity = quantity,
                UnitPrice = service.Price,
                AddedDate = DateTime.UtcNow
            };

            _context.BookingServices.Add(bs);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveServiceAsync(int bookingServiceId)
        {
            var bs = await _context.BookingServices.FindAsync(bookingServiceId)
                     ?? throw new InvalidOperationException("Booking service not found.");
            _context.BookingServices.Remove(bs);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Service>> GetActiveServicesAsync() =>
            await _context.Services.Where(s => s.IsActive).OrderBy(s => s.ServiceName).ToListAsync();

        public async Task<BookingServiceSummary> GetServiceSummaryAsync(int bookingId)
        {
            var booking = await _bookingRepo.GetBookingWithDetailsAsync(bookingId)
                          ?? throw new InvalidOperationException("Booking not found.");

            var nights = Math.Max(1, (booking.CheckOutDate - booking.CheckInDate).Days);
            var roomTotal = booking.Room.RoomType.BasePrice * nights;
            var serviceTotal = booking.BookingServices.Sum(bs => bs.UnitPrice * bs.Quantity);

            return new BookingServiceSummary
            {
                Services = booking.BookingServices.ToList(),
                ServiceTotal = serviceTotal,
                RoomTotal = roomTotal,
                GrandEstimate = roomTotal + serviceTotal
            };
        }
    }
}
