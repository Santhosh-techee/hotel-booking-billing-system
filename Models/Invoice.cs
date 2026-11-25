using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingSystem.Models
{
    public enum PaymentStatus
    {
        Pending,
        Paid
    }

    public enum PaymentMode
    {
        Cash,
        UPI,
        Card
    }

    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required, MaxLength(20)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int BookingId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Room Charges (₹)")]
        public decimal RoomCharges { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Service Charges (₹)")]
        public decimal ServiceCharges { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tax Amount (₹)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Grand Total (₹)")]
        public decimal GrandTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Paid Amount (₹)")]
        public decimal PaidAmount { get; set; }

        [Required]
        [Display(Name = "Payment Status")]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [Display(Name = "Payment Mode")]
        public PaymentMode PaymentMode { get; set; } = PaymentMode.Cash;

        // GST breakdown fields
        [Column(TypeName = "decimal(18,2)")]
        public decimal CgstAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SgstAmount { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal GstRate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey("BookingId")]
        public Booking Booking { get; set; } = null!;
    }
}
