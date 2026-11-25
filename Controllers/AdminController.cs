using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.Services;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IRoomService _roomService;
        private readonly IBookingService _bookingService;
        private readonly IInvoiceService _invoiceService;
        private readonly ApplicationDbContext _context;

        public AdminController(IRoomService roomService, IBookingService bookingService,
                               IInvoiceService invoiceService, ApplicationDbContext context)
        {
            _roomService = roomService;
            _bookingService = bookingService;
            _invoiceService = invoiceService;
            _context = context;
        }

        // ========== Dashboard ==========
        public async Task<IActionResult> Dashboard()
        {
            var allBookings = await _bookingService.GetAllWithDetailsAsync();
            var vm = new DashboardViewModel
            {
                TotalRevenue = await _invoiceService.GetTotalRevenueAsync(),
                ActiveGuests = allBookings.Count(b => b.BookingStatus == BookingStatus.CheckedIn),
                OccupancyRate = await _roomService.GetOccupancyRateAsync(),
                TodayCheckIns = allBookings.Count(b => b.CheckInDate.Date == DateTime.UtcNow.Date &&
                                                        (b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.CheckedIn)),
                TotalBookings = allBookings.Count(),
                MonthlyRevenue = await _invoiceService.GetMonthlyRevenueAsync(),
                BookingsByRoomType = await _invoiceService.GetBookingsByRoomTypeAsync()
            };
            return View(vm);
        }

        // ========== RoomTypes CRUD ==========
        public async Task<IActionResult> RoomTypes()
        {
            var roomTypes = await _roomService.GetAllRoomTypesAsync();
            return View(roomTypes);
        }

        [HttpGet]
        public IActionResult CreateRoomType() => View(new RoomType());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoomType(RoomType model, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (ext is ".jpg" or ".jpeg" or ".png" or ".webp")
                {
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "roomtypes");
                    Directory.CreateDirectory(uploadPath);
                    var filePath = Path.Combine(uploadPath, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    model.ImageUrl = $"/uploads/roomtypes/{fileName}";
                }
            }

            ModelState.Remove("imageFile");
            ModelState.Remove("Rooms");
            if (!ModelState.IsValid) return View(model);

            await _roomService.CreateRoomTypeAsync(model);
            TempData["Success"] = "Room type created successfully!";
            return RedirectToAction(nameof(RoomTypes));
        }

        [HttpGet]
        public async Task<IActionResult> EditRoomType(int id)
        {
            var rt = await _roomService.GetRoomTypeByIdAsync(id);
            if (rt == null) return NotFound();
            return View(rt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoomType(RoomType model, IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (ext is ".jpg" or ".jpeg" or ".png" or ".webp")
                {
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "roomtypes");
                    Directory.CreateDirectory(uploadPath);
                    var filePath = Path.Combine(uploadPath, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    model.ImageUrl = $"/uploads/roomtypes/{fileName}";
                }
            }

            ModelState.Remove("imageFile");
            ModelState.Remove("Rooms");
            if (!ModelState.IsValid) return View(model);

            await _roomService.UpdateRoomTypeAsync(model);
            TempData["Success"] = "Room type updated!";
            return RedirectToAction(nameof(RoomTypes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoomType(int id)
        {
            try
            {
                await _roomService.DeleteRoomTypeAsync(id);
                TempData["Success"] = "Room type deleted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Cannot delete: {ex.Message}";
            }
            return RedirectToAction(nameof(RoomTypes));
        }

        // ========== Rooms CRUD ==========
        public async Task<IActionResult> Rooms()
        {
            var rooms = await _roomService.GetAllRoomsWithTypeAsync();
            return View(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> CreateRoom()
        {
            await PopulateRoomTypeDropdown();
            return View(new Room());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRoom(Room model)
        {
            ModelState.Remove("RoomType");
            ModelState.Remove("Bookings");
            if (!ModelState.IsValid)
            {
                await PopulateRoomTypeDropdown();
                return View(model);
            }
            try
            {
                await _roomService.CreateRoomAsync(model);
                TempData["Success"] = $"Room {model.RoomNumber} created!";
                return RedirectToAction(nameof(Rooms));
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                await PopulateRoomTypeDropdown();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditRoom(int id)
        {
            var room = await _roomService.GetRoomWithTypeAsync(id);
            if (room == null) return NotFound();
            await PopulateRoomTypeDropdown(room.RoomTypeId);
            return View(room);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRoom(Room model)
        {
            ModelState.Remove("RoomType");
            ModelState.Remove("Bookings");
            if (!ModelState.IsValid)
            {
                await PopulateRoomTypeDropdown(model.RoomTypeId);
                return View(model);
            }
            await _roomService.UpdateRoomAsync(model);
            TempData["Success"] = $"Room {model.RoomNumber} updated!";
            return RedirectToAction(nameof(Rooms));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            try
            {
                await _roomService.DeleteRoomAsync(id);
                TempData["Success"] = "Room deleted.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Cannot delete: {ex.Message}";
            }
            return RedirectToAction(nameof(Rooms));
        }

        // ========== Services CRUD ==========
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services.OrderBy(s => s.ServiceName).ToListAsync();
            return View(services);
        }

        [HttpGet]
        public IActionResult CreateService() => View(new Service());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(Service model)
        {
            ModelState.Remove("BookingServices");
            if (!ModelState.IsValid) return View(model);
            _context.Services.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Service created!";
            return RedirectToAction(nameof(Services));
        }

        [HttpGet]
        public async Task<IActionResult> EditService(int id)
        {
            var svc = await _context.Services.FindAsync(id);
            if (svc == null) return NotFound();
            return View(svc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(Service model)
        {
            ModelState.Remove("BookingServices");
            if (!ModelState.IsValid) return View(model);
            _context.Services.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Service updated!";
            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var svc = await _context.Services.FindAsync(id);
            if (svc != null)
            {
                _context.Services.Remove(svc);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Service deleted.";
            }
            return RedirectToAction(nameof(Services));
        }

        // ========== Bookings list with search/filter/pagination ==========
        public async Task<IActionResult> Bookings(string? search, string? statusFilter, DateTime? from, DateTime? to, int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;
            var allBookings = await _bookingService.GetAllWithDetailsAsync();
            var query = allBookings.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(b =>
                    b.BookingRef.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    b.GuestName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    b.GuestPhone.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (b.Room != null && b.Room.RoomNumber.Contains(term, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<BookingStatus>(statusFilter, out var parsedStatus))
                query = query.Where(b => b.BookingStatus == parsedStatus);

            if (from.HasValue) query = query.Where(b => b.CheckInDate.Date >= from.Value.Date);
            if (to.HasValue) query = query.Where(b => b.CheckInDate.Date <= to.Value.Date);

            var total = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            if (page > totalPages) page = totalPages;
            var bookings = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = total;
            ViewBag.Search = search;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.StatusOptions = Enum.GetNames(typeof(BookingStatus));

            return View(bookings);
        }

        // ========== Invoices list with search/filter/pagination ==========
        public async Task<IActionResult> Invoices(string? search, string? paymentFilter, DateTime? from, DateTime? to, int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;
            var allInvoices = await _invoiceService.GetAllInvoicesAsync();
            var query = allInvoices.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(i =>
                    i.InvoiceNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (i.Booking != null && (
                        i.Booking.BookingRef.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                        i.Booking.GuestName.Contains(term, StringComparison.OrdinalIgnoreCase))));
            }

            if (!string.IsNullOrEmpty(paymentFilter) && Enum.TryParse<PaymentStatus>(paymentFilter, out var parsedStatus))
                query = query.Where(i => i.PaymentStatus == parsedStatus);

            if (from.HasValue) query = query.Where(i => i.CreatedAt.Date >= from.Value.Date);
            if (to.HasValue) query = query.Where(i => i.CreatedAt.Date <= to.Value.Date);

            var total = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            if (page > totalPages) page = totalPages;
            var invoices = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalRecords = total;
            ViewBag.TotalRevenueFiltered = query.Where(i => i.PaymentStatus == PaymentStatus.Paid).Sum(i => i.GrandTotal);
            ViewBag.Search = search;
            ViewBag.PaymentFilter = paymentFilter;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(invoices);
        }

        private async Task PopulateRoomTypeDropdown(int? selectedId = null)
        {
            var types = await _roomService.GetAllRoomTypesAsync();
            ViewBag.RoomTypes = new SelectList(types, "RoomTypeId", "Name", selectedId);
        }
    }
}
