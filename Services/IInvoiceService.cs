using HotelBookingSystem.Models;
using HotelBookingSystem.ViewModels;

namespace HotelBookingSystem.Services
{
    public interface IInvoiceService
    {
        Task<Invoice> GenerateInvoiceAsync(int bookingId, PaymentMode paymentMode, decimal discount);
        Task<Invoice?> GetInvoiceByBookingIdAsync(int bookingId);
        Task<Invoice?> GetInvoiceByIdAsync(int invoiceId);
        Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<IEnumerable<MonthlyRevenueData>> GetMonthlyRevenueAsync();
        Task<IEnumerable<BookingsByRoomTypeData>> GetBookingsByRoomTypeAsync();
    }

    public class MonthlyRevenueData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
    }

    public class BookingsByRoomTypeData
    {
        public string RoomTypeName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
