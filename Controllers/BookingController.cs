using HotelBookingSystem.Services;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingSystem.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingService _bookingService;
        private readonly IRoomService _roomService;

        public BookingController(IBookingService bookingService, IRoomService roomService)
        {
            _bookingService = bookingService;
            _roomService = roomService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int roomId, DateTime checkIn, DateTime checkOut, int guests)
        {
            var room = await _roomService.GetRoomWithTypeAsync(roomId);
            if (room == null)
            {
                TempData["Error"] = "Room not found.";
                return RedirectToAction("Index", "Home");
            }

            var vm = new CreateBookingViewModel
            {
                RoomId = roomId,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                GuestCount = guests,
                SelectedRoom = room,
                TotalNights = Math.Max(1, (checkOut - checkIn).Days),
                EstimatedTotal = room.RoomType.BasePrice * Math.Max(1, (checkOut - checkIn).Days)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.SelectedRoom = await _roomService.GetRoomWithTypeAsync(vm.RoomId);
                vm.TotalNights = Math.Max(1, (vm.CheckOutDate - vm.CheckInDate).Days);
                if (vm.SelectedRoom != null)
                    vm.EstimatedTotal = vm.SelectedRoom.RoomType.BasePrice * vm.TotalNights;
                return View(vm);
            }

            try
            {
                var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var booking = await _bookingService.CreateBookingAsync(vm, guestId);
                TempData["Success"] = $"Booking confirmed! Reference: {booking.BookingRef}";
                return RedirectToAction(nameof(MyBookings));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                vm.SelectedRoom = await _roomService.GetRoomWithTypeAsync(vm.RoomId);
                vm.TotalNights = Math.Max(1, (vm.CheckOutDate - vm.CheckInDate).Days);
                if (vm.SelectedRoom != null)
                    vm.EstimatedTotal = vm.SelectedRoom.RoomType.BasePrice * vm.TotalNights;
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Receptionist");

            IEnumerable<Models.Booking> bookings;
            if (isStaff)
                bookings = await _bookingService.GetAllBookingsAsync();
            else
                bookings = await _bookingService.GetGuestBookingsAsync(guestId);

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            try
            {
                var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var isStaff = User.IsInRole("Admin") || User.IsInRole("Receptionist");
                await _bookingService.CancelBookingAsync(bookingId, guestId, isStaff);
                TempData["Success"] = "Booking cancelled successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(MyBookings));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _bookingService.GetBookingWithDetailsAsync(id);
            if (booking == null) return NotFound();

            var guestId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Receptionist");
            if (!isStaff && booking.GuestId != guestId)
                return Forbid();

            return View(booking);
        }
    }
}
