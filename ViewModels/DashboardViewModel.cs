using HotelBookingSystem.Services;

namespace HotelBookingSystem.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int ActiveGuests { get; set; }
        public decimal OccupancyRate { get; set; }
        public int TodayCheckIns { get; set; }
        public int TotalBookings { get; set; }
        public IEnumerable<MonthlyRevenueData> MonthlyRevenue { get; set; } = new List<MonthlyRevenueData>();
        public IEnumerable<BookingsByRoomTypeData> BookingsByRoomType { get; set; } = new List<BookingsByRoomTypeData>();
    }
}
