using HotelBookingSystem.Models;

namespace HotelBookingSystem.Services
{
    public interface IPdfService
    {
        byte[] GenerateInvoicePdf(Invoice invoice);
    }
}
