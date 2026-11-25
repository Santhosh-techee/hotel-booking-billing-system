using HotelBookingSystem.Data;
using HotelBookingSystem.Models;
using HotelBookingSystem.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBookingRepository _bookingRepo;

        public InvoiceService(ApplicationDbContext context, IBookingRepository bookingRepo)
        {
            _context = context;
            _bookingRepo = bookingRepo;
        }

        public async Task<Invoice> GenerateInvoiceAsync(int bookingId, PaymentMode paymentMode, decimal discount)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var booking = await _bookingRepo.GetBookingWithDetailsAsync(bookingId)
                              ?? throw new InvalidOperationException("Booking not found.");

                if (booking.BookingStatus != BookingStatus.CheckedIn)
                    throw new InvalidOperationException("Can only checkout a checked-in booking.");

                // Calculate nights (minimum 1)
                var nights = Math.Max(1, (booking.CheckOutDate - booking.CheckInDate).Days);
                var roomCharges = booking.Room.RoomType.BasePrice * nights;
                var serviceCharges = booking.BookingServices.Sum(bs => bs.UnitPrice * bs.Quantity);

                if (discount < 0 || discount > roomCharges + serviceCharges)
                    throw new InvalidOperationException("Discount must be between ₹0 and total charges.");

                var subtotal = roomCharges + serviceCharges - discount;

                // Slab-based GST on room tariff per night
                decimal gstRate;
                if (booking.Room.RoomType.BasePrice <= 7500m)
                    gstRate = 12m;  // 6% CGST + 6% SGST
                else
                    gstRate = 18m;  // 9% CGST + 9% SGST

                var taxableAmount = roomCharges - discount;
                if (taxableAmount < 0) taxableAmount = 0;
                var totalTax = Math.Round(taxableAmount * gstRate / 100, 2);
                var cgst = Math.Round(totalTax / 2, 2);
                var sgst = totalTax - cgst;
                var grandTotal = subtotal + totalTax;

                // Generate unique invoice number: INV-YYYY-NNNN
                var year = DateTime.UtcNow.Year;
                var prefix = $"INV-{year}-";
                var existingInvoices = await _context.Invoices
                    .Where(i => i.InvoiceNumber.StartsWith(prefix))
                    .Select(i => i.InvoiceNumber)
                    .ToListAsync();
                var maxSeq = 0;
                foreach (var invNum in existingInvoices)
                {
                    if (int.TryParse(invNum.Substring(prefix.Length), out var n) && n > maxSeq)
                        maxSeq = n;
                }
                var invoiceNumber = $"{prefix}{(maxSeq + 1):D4}";

                var invoice = new Invoice
                {
                    InvoiceNumber = invoiceNumber,
                    BookingId = bookingId,
                    RoomCharges = roomCharges,
                    ServiceCharges = serviceCharges,
                    TaxAmount = totalTax,
                    CgstAmount = cgst,
                    SgstAmount = sgst,
                    GstRate = gstRate,
                    Discount = discount,
                    GrandTotal = grandTotal,
                    PaidAmount = grandTotal,
                    PaymentStatus = PaymentStatus.Paid,
                    PaymentMode = paymentMode,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Invoices.Add(invoice);

                // Update booking and room status atomically
                booking.BookingStatus = BookingStatus.CheckedOut;
                booking.Room.Status = RoomStatus.Cleaning;
                _context.Bookings.Update(booking);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return invoice;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Invoice?> GetInvoiceByBookingIdAsync(int bookingId) =>
            await _context.Invoices
                .Include(i => i.Booking).ThenInclude(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(i => i.Booking).ThenInclude(b => b.Guest)
                .Include(i => i.Booking).ThenInclude(b => b.BookingServices).ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(i => i.BookingId == bookingId);

        public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId) =>
            await _context.Invoices
                .Include(i => i.Booking).ThenInclude(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(i => i.Booking).ThenInclude(b => b.Guest)
                .Include(i => i.Booking).ThenInclude(b => b.BookingServices).ThenInclude(bs => bs.Service)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync() =>
            await _context.Invoices
                .Include(i => i.Booking).ThenInclude(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(i => i.Booking).ThenInclude(b => b.Guest)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

        public async Task<decimal> GetTotalRevenueAsync() =>
            await _context.Invoices
                .Where(i => i.PaymentStatus == PaymentStatus.Paid)
                .SumAsync(i => i.GrandTotal);

        public async Task<IEnumerable<MonthlyRevenueData>> GetMonthlyRevenueAsync()
        {
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-11);
            var data = await _context.Invoices
                .Where(i => i.PaymentStatus == PaymentStatus.Paid && i.CreatedAt >= twelveMonthsAgo)
                .GroupBy(i => new { i.CreatedAt.Year, i.CreatedAt.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(i => i.GrandTotal),
                    Count = g.Count()
                })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            return data.Select(d => new MonthlyRevenueData
            {
                Month = new DateTime(d.Year, d.Month, 1).ToString("MMM yy"),
                Revenue = d.Revenue,
                BookingCount = d.Count
            });
        }

        public async Task<IEnumerable<BookingsByRoomTypeData>> GetBookingsByRoomTypeAsync()
        {
            var data = await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .Where(b => b.BookingStatus != BookingStatus.Cancelled)
                .GroupBy(b => b.Room.RoomType.Name)
                .Select(g => new BookingsByRoomTypeData
                {
                    RoomTypeName = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return data;
        }
    }
}
