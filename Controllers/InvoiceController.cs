using HotelBookingSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelBookingSystem.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPdfService _pdfService;

        public InvoiceController(IInvoiceService invoiceService, IPdfService pdfService)
        {
            _invoiceService = invoiceService;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> Details(int invoiceId)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Receptionist");
            if (!isStaff && invoice.Booking.GuestId != userId)
                return Forbid();

            return View(invoice);
        }

        public async Task<IActionResult> DownloadPdf(int invoiceId)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isStaff = User.IsInRole("Admin") || User.IsInRole("Receptionist");
            if (!isStaff && invoice.Booking.GuestId != userId)
                return Forbid();

            var pdfBytes = _pdfService.GenerateInvoicePdf(invoice);
            var fileName = $"{invoice.InvoiceNumber}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
