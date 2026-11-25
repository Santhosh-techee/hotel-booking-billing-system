using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<BookingService> BookingServices { get; set; }
        public DbSet<Invoice> Invoices { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // RoomType
            builder.Entity<RoomType>(entity =>
            {
                entity.Property(e => e.BasePrice).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Room
            builder.Entity<Room>(entity =>
            {
                entity.HasIndex(e => e.RoomNumber).IsUnique();
                entity.Property(e => e.Status).HasConversion<string>();
                entity.HasOne(e => e.RoomType)
                    .WithMany(rt => rt.Rooms)
                    .HasForeignKey(e => e.RoomTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Booking
            builder.Entity<Booking>(entity =>
            {
                entity.HasIndex(e => e.BookingRef).IsUnique();
                entity.Property(e => e.BookingStatus).HasConversion<string>();
                entity.HasOne(e => e.Room)
                    .WithMany(r => r.Bookings)
                    .HasForeignKey(e => e.RoomId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Guest)
                    .WithMany(u => u.Bookings)
                    .HasForeignKey(e => e.GuestId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // BookingService
            builder.Entity<BookingService>(entity =>
            {
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.HasOne(e => e.Booking)
                    .WithMany(b => b.BookingServices)
                    .HasForeignKey(e => e.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Service)
                    .WithMany(s => s.BookingServices)
                    .HasForeignKey(e => e.ServiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Invoice
            builder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(e => e.InvoiceNumber).IsUnique();
                entity.Property(e => e.PaymentStatus).HasConversion<string>();
                entity.Property(e => e.PaymentMode).HasConversion<string>();
                entity.Property(e => e.RoomCharges).HasColumnType("decimal(18,2)");
                entity.Property(e => e.ServiceCharges).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TaxAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Discount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.GrandTotal).HasColumnType("decimal(18,2)");
                entity.Property(e => e.PaidAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CgstAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SgstAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.GstRate).HasColumnType("decimal(5,2)");
                entity.HasOne(e => e.Booking)
                    .WithOne(b => b.Invoice)
                    .HasForeignKey<Invoice>(e => e.BookingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
