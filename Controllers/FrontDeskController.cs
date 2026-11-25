using System.Security.Claims;
using HotelBookingSystem.Models;
using HotelBookingSystem.Services;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelBookingService = HotelBookingSystem.Models.BookingService;

namespace HotelBookingSystem.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class FrontDeskController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IBookingService _bookingService;
        private readonly IInvoiceService _invoiceService;
        private readonly IPdfService _pdfService;

        public FrontDeskController(IRoomService roomService, IBookingService bookingService,
                                   IInvoiceService invoiceService, IPdfService pdfService)
        {
            _roomService = roomService;
            _bookingService = bookingService;
            _invoiceService = invoiceService;
            _pdfService = pdfService;
        }

        // Live Room Status Board
        public async Task<IActionResult> Index()
        {
            var rooms = await _roomService.GetAllRoomsWithTypeAsync();
            var activeBookings = await _bookingService.GetAllWithDetailsAsync();
            var checkedInBookings = activeBookings.Where(b => b.BookingStatus == BookingStatus.CheckedIn).ToList();
            ViewBag.CheckedInBookings = checkedInBookings;
            return View(rooms);
        }

        // Quick room status toggle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int roomId, RoomStatus status)
        {
            try
            {
                await _roomService.UpdateRoomStatusAsync(roomId, status);
                TempData["Success"] = "Room status updated.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // All bookings pending check-in
        public async Task<IActionResult> Arrivals()
        {
            var bookings = await _bookingService.GetAllWithDetailsAsync();
            var arrivals = bookings.Where(b => b.BookingStatus == BookingStatus.Confirmed ||
                                               b.BookingStatus == BookingStatus.Pending).ToList();
            return View(arrivals);
        }

        // Walk-In Booking + Immediate Check-In
        [HttpGet]
        public async Task<IActionResult> WalkIn(int? roomId = null)
        {
            var rooms = await _roomService.GetAllRoomsWithTypeAsync();
            var available = rooms.Where(r => r.Status != RoomStatus.Occupied && r.Status != RoomStatus.Maintenance).ToList();
            var vm = new WalkInBookingViewModel
            {
                RoomId = roomId ?? 0,
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1),
                GuestCount = 1,
                AvailableRooms = available
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WalkIn(WalkInBookingViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var rooms = await _roomService.GetAllRoomsWithTypeAsync();
                vm.AvailableRooms = rooms.Where(r => r.Status != RoomStatus.Occupied && r.Status != RoomStatus.Maintenance).ToList();
                return View(vm);
            }

            try
            {
                var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var booking = await _bookingService.CreateWalkInBookingAsync(vm, staffId);
                TempData["Success"] = $"Walk-in guest checked in! Ref: {booking.BookingRef}";
                return RedirectToAction(nameof(ActiveStay), new { bookingId = booking.BookingId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                var rooms = await _roomService.GetAllRoomsWithTypeAsync();
                vm.AvailableRooms = rooms.Where(r => r.Status != RoomStatus.Occupied && r.Status != RoomStatus.Maintenance).ToList();
                return View(vm);
            }
        }

        // Check-In form
        [HttpGet]
        public async Task<IActionResult> CheckIn(int bookingId)
        {
            var booking = await _bookingService.GetBookingWithDetailsAsync(bookingId);
            if (booking == null) return NotFound();
            if (booking.BookingStatus != BookingStatus.Confirmed && booking.BookingStatus != BookingStatus.Pending)
            {
                TempData["Error"] = $"Booking {booking.BookingRef} cannot be checked in (Status: {booking.BookingStatus}).";
                return RedirectToAction(nameof(Arrivals));
            }
            var vm = new CheckInViewModel { BookingId = bookingId, Booking = booking };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(CheckInViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Booking = await _bookingService.GetBookingWithDetailsAsync(vm.BookingId);
                return View(vm);
            }
            try
            {
                await _bookingService.CheckInAsync(vm.BookingId, vm.IdProofNumber);
                TempData["Success"] = "Guest checked in successfully!";
                return RedirectToAction(nameof(ActiveStay), new { bookingId = vm.BookingId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Arrivals));
            }
        }

        // Active stay management (AJAX services)
        public async Task<IActionResult> ActiveStay(int bookingId)
        {
            var booking = await _bookingService.GetBookingWithDetailsAsync(bookingId);
            if (booking == null) return NotFound();
            if (booking.BookingStatus != BookingStatus.CheckedIn)
                return RedirectToAction(nameof(Index));

            var services = await _bookingService.GetActiveServicesAsync();
            ViewBag.AllServices = services;
            return View(booking);
        }

        // AJAX: Add service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService([FromBody] AddServiceViewModel vm)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid service selection or quantity." });

            try
            {
                await _bookingService.AddServiceAsync(vm.BookingId, vm.ServiceId, vm.Quantity);
                var summary = await _bookingService.GetServiceSummaryAsync(vm.BookingId);
                return Json(new
                {
                    success = true,
                    serviceTotal = summary.ServiceTotal,
                    roomTotal = summary.RoomTotal,
                    grandEstimate = summary.GrandEstimate,
                    services = summary.Services.Select(s => new
                    {
                        id = s.Id,
                        name = s.Service.ServiceName,
                        qty = s.Quantity,
                        unitPrice = s.UnitPrice,
                        total = s.UnitPrice * s.Quantity
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // AJAX: Remove service
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveService([FromBody] RemoveServiceRequest req)
        {
            try
            {
                await _bookingService.RemoveServiceAsync(req.BookingServiceId);
                var summary = await _bookingService.GetServiceSummaryAsync(req.BookingId);
                return Json(new
                {
                    success = true,
                    serviceTotal = summary.ServiceTotal,
                    roomTotal = summary.RoomTotal,
                    grandEstimate = summary.GrandEstimate,
                    services = summary.Services.Select(s => new
                    {
                        id = s.Id,
                        name = s.Service.ServiceName,
                        qty = s.Quantity,
                        unitPrice = s.UnitPrice,
                        total = s.UnitPrice * s.Quantity
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Checkout preview
        [HttpGet]
        public async Task<IActionResult> Checkout(int bookingId)
        {
            var booking = await _bookingService.GetBookingWithDetailsAsync(bookingId);
            if (booking == null) return NotFound();

            var nights = Math.Max(1, (booking.CheckOutDate - booking.CheckInDate).Days);
            var roomCharges = booking.Room.RoomType.BasePrice * nights;
            var serviceCharges = booking.BookingServices.Sum(bs => bs.UnitPrice * bs.Quantity);
            decimal gstRate = booking.Room.RoomType.BasePrice <= 7500m ? 12m : 18m;
            var tax = Math.Round(roomCharges * gstRate / 100, 2);

            var vm = new CheckoutViewModel
            {
                BookingId = bookingId,
                Booking = booking,
                RoomCharges = roomCharges,
                ServiceCharges = serviceCharges,
                GstRate = gstRate,
                TaxAmount = tax,
                GrandTotal = roomCharges + serviceCharges + tax,
                TotalNights = nights
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel vm)
        {
            try
            {
                var invoice = await _invoiceService.GenerateInvoiceAsync(vm.BookingId, vm.PaymentMode, vm.Discount);
                TempData["Success"] = $"Checkout complete! Invoice {invoice.InvoiceNumber} generated.";
                return RedirectToAction("Details", "Invoice", new { invoiceId = invoice.InvoiceId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Checkout), new { bookingId = vm.BookingId });
            }
        }
    }

    public class RemoveServiceRequest
    {
        public int BookingServiceId { get; set; }
        public int BookingId { get; set; }
    }
}
