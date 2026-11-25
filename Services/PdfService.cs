using HotelBookingSystem.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HotelBookingSystem.Services
{
    public class PdfService : IPdfService
    {
        public byte[] GenerateInvoicePdf(Invoice invoice)
        {
            var booking = invoice.Booking;
            var nights = Math.Max(1, (booking.CheckOutDate - booking.CheckInDate).Days);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI", "Lato").FontSize(10));

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("GRAND HORIZON HOTEL")
                                    .FontSize(22).Bold().FontColor("#1B263B");
                                c.Item().Text("Luxury Redefined, Every Stay")
                                    .FontSize(10).FontColor("#C5A059").Italic();
                                c.Item().Text("123 Palace Road, Chennai - 600001")
                                    .FontSize(9).FontColor("#555555");
                                c.Item().Text("GSTIN: 33AABCG1234F1Z5 | Ph: +91 44 1234 5678")
                                    .FontSize(9).FontColor("#555555");
                            });
                            row.ConstantItem(140).Column(c =>
                            {
                                c.Item().Background("#1B263B").Padding(10).Column(inner =>
                                {
                                    inner.Item().Text("TAX INVOICE")
                                        .FontSize(14).Bold().FontColor("#C5A059").AlignCenter();
                                    inner.Item().Text(invoice.InvoiceNumber)
                                        .FontSize(11).Bold().FontColor(Colors.White).AlignCenter();
                                    inner.Item().Text(invoice.CreatedAt.ToString("dd MMM yyyy"))
                                        .FontSize(9).FontColor(Colors.White).AlignCenter();
                                });
                            });
                        });
                        col.Item().PaddingTop(5).LineHorizontal(2).LineColor("#C5A059");
                    });

                    // Content
                    page.Content().PaddingTop(15).Column(col =>
                    {
                        // Guest & Booking Info
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor("#DDDDDD").Padding(10).Column(c =>
                            {
                                c.Item().Text("BILLED TO").Bold().FontColor("#1B263B").FontSize(9);
                                c.Item().PaddingTop(4).Text(booking.GuestName).Bold().FontSize(11);
                                c.Item().Text($"Phone: {booking.GuestPhone}").FontColor("#555555");
                                c.Item().Text($"Email: {booking.Guest?.Email ?? "N/A"}").FontColor("#555555");
                                if (!string.IsNullOrEmpty(booking.IdProofNumber))
                                    c.Item().Text($"ID Proof: {booking.IdProofNumber}").FontColor("#555555");
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Border(1).BorderColor("#DDDDDD").Padding(10).Column(c =>
                            {
                                c.Item().Text("BOOKING DETAILS").Bold().FontColor("#1B263B").FontSize(9);
                                c.Item().PaddingTop(4).Row(r =>
                                {
                                    r.RelativeItem().Text("Booking Ref:").FontColor("#555555");
                                    r.RelativeItem().Text(booking.BookingRef).Bold();
                                });
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Room:").FontColor("#555555");
                                    r.RelativeItem().Text($"Room {booking.Room.RoomNumber} - {booking.Room.RoomType.Name}").Bold();
                                });
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Check-In:").FontColor("#555555");
                                    r.RelativeItem().Text(booking.CheckInDate.ToString("dd MMM yyyy")).Bold();
                                });
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Check-Out:").FontColor("#555555");
                                    r.RelativeItem().Text(booking.CheckOutDate.ToString("dd MMM yyyy")).Bold();
                                });
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Duration:").FontColor("#555555");
                                    r.RelativeItem().Text($"{nights} Night(s)").Bold();
                                });
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Guests:").FontColor("#555555");
                                    r.RelativeItem().Text(booking.GuestCount.ToString()).Bold();
                                });
                            });
                        });

                        col.Item().PaddingTop(15).Text("CHARGE DETAILS").Bold().FontColor("#1B263B");
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(5);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background("#1B263B").Padding(8)
                                    .Text("Description").FontColor(Colors.White).Bold();
                                header.Cell().Background("#1B263B").Padding(8)
                                    .Text("Qty").FontColor(Colors.White).Bold().AlignCenter();
                                header.Cell().Background("#1B263B").Padding(8)
                                    .Text("Unit Price (₹)").FontColor(Colors.White).Bold().AlignRight();
                                header.Cell().Background("#1B263B").Padding(8)
                                    .Text("Amount (₹)").FontColor(Colors.White).Bold().AlignRight();
                            });

                            // Room charge row
                            table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(8)
                                .Text($"Room {booking.Room.RoomNumber} — {booking.Room.RoomType.Name} ({nights} Night(s))");
                            table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(8)
                                .Text(nights.ToString()).AlignCenter();
                            table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(8)
                                .Text($"₹{booking.Room.RoomType.BasePrice:N2}").AlignRight();
                            table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(8)
                                .Text($"₹{invoice.RoomCharges:N2}").AlignRight();

                            // Service rows
                            foreach (var bs in booking.BookingServices)
                            {
                                table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(8)
                                    .Text(bs.Service.ServiceName);
                                table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(8)
                                    .Text(bs.Quantity.ToString()).AlignCenter();
                                table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(8)
                                    .Text($"₹{bs.UnitPrice:N2}").AlignRight();
                                table.Cell().BorderBottom(1).BorderColor("#EEEEEE").Padding(8)
                                    .Text($"₹{bs.UnitPrice * bs.Quantity:N2}").AlignRight();
                            }
                        });

                        // Totals section
                        col.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem();
                            row.ConstantItem(280).Column(c =>
                            {
                                void AddRow(string label, string value, bool bold = false, string? color = null)
                                {
                                    c.Item().Row(r =>
                                    {
                                        var labelSpan = r.RelativeItem().Padding(4).Text(label).FontColor(color ?? "#333333");
                                        if (bold) labelSpan.Bold();

                                        var valueSpan = r.ConstantItem(100).Padding(4).Text(value).AlignRight().FontColor(color ?? "#333333");
                                        if (bold) valueSpan.Bold();
                                    });
                                }

                                AddRow("Room Charges:", $"₹{invoice.RoomCharges:N2}");
                                AddRow("Service Charges:", $"₹{invoice.ServiceCharges:N2}");
                                if (invoice.Discount > 0)
                                    AddRow("Discount:", $"-₹{invoice.Discount:N2}", color: "#2E7D32");
                                AddRow($"CGST ({invoice.GstRate / 2:0.##}%):", $"₹{invoice.CgstAmount:N2}");
                                AddRow($"SGST ({invoice.GstRate / 2:0.##}%):", $"₹{invoice.SgstAmount:N2}");

                                c.Item().LineHorizontal(1).LineColor("#C5A059");
                                c.Item().Background("#1B263B").Padding(8).Row(r =>
                                {
                                    r.RelativeItem().Text("GRAND TOTAL").Bold().FontColor("#C5A059").FontSize(12);
                                    r.ConstantItem(100).Text($"₹{invoice.GrandTotal:N2}")
                                        .Bold().FontColor(Colors.White).FontSize(12).AlignRight();
                                });

                                c.Item().PaddingTop(5).Row(r =>
                                {
                                    r.RelativeItem().Text("Payment Mode:").FontColor("#555555");
                                    r.ConstantItem(100).Text(invoice.PaymentMode.ToString()).Bold().AlignRight();
                                });
                                c.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Payment Status:").FontColor("#555555");
                                    r.ConstantItem(100).Text("PAID ✓").Bold().FontColor("#2E7D32").AlignRight();
                                });
                            });
                        });

                        // Note
                        col.Item().PaddingTop(20).Background("#FFF8E7").Padding(10).Column(c =>
                        {
                            c.Item().Text("Thank you for choosing Grand Horizon Hotel!")
                                .Bold().FontColor("#C5A059").FontSize(11);
                            c.Item().Text("This is a computer-generated invoice and does not require a signature.")
                                .FontSize(8).FontColor("#777777").Italic();
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Grand Horizon Hotel | www.grandhorizon.com | ").FontColor("#777777").FontSize(8);
                        text.Span("Page ").FontColor("#777777").FontSize(8);
                        text.CurrentPageNumber().FontColor("#777777").FontSize(8);
                        text.Span(" of ").FontColor("#777777").FontSize(8);
                        text.TotalPages().FontColor("#777777").FontSize(8);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
