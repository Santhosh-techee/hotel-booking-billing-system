using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Apply pending migrations
            await context.Database.MigrateAsync();

            // --- Seed Roles ---
            string[] roles = { "Admin", "Receptionist", "Guest" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // --- Seed Admin User ---
            var adminEmail = "admin@hotel.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Hotel Administrator",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // --- Seed Receptionist User ---
            var staffEmail = "staff@hotel.com";
            if (await userManager.FindByEmailAsync(staffEmail) == null)
            {
                var staff = new ApplicationUser
                {
                    UserName = staffEmail,
                    Email = staffEmail,
                    FullName = "Front Desk Staff",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(staff, "Staff@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(staff, "Receptionist");
            }

            // --- Seed RoomTypes ---
            if (!await context.RoomTypes.AnyAsync())
            {
                var roomTypes = new List<RoomType>
                {
                    new RoomType
                    {
                        Name = "Standard AC",
                        BasePrice = 2500,
                        MaxGuests = 2,
                        Amenities = "Air Conditioning, Wi-Fi, TV, Mini Bar, 24hr Room Service",
                        ImageUrl = "/images/standard-ac.jpg"
                    },
                    new RoomType
                    {
                        Name = "Deluxe Suite",
                        BasePrice = 6500,
                        MaxGuests = 3,
                        Amenities = "Air Conditioning, Wi-Fi, Smart TV, Mini Bar, Jacuzzi, City View, 24hr Butler",
                        ImageUrl = "/images/deluxe-suite.jpg"
                    },
                    new RoomType
                    {
                        Name = "Royal Executive",
                        BasePrice = 12000,
                        MaxGuests = 4,
                        Amenities = "Air Conditioning, Wi-Fi, 65\" Smart TV, Mini Bar, Private Jacuzzi, Panoramic View, Private Chef, Limo Pickup",
                        ImageUrl = "/images/royal-executive.jpg"
                    }
                };
                context.RoomTypes.AddRange(roomTypes);
                await context.SaveChangesAsync();
            }

            // --- Seed Rooms ---
            if (!await context.Rooms.AnyAsync())
            {
                var standardId = (await context.RoomTypes.FirstAsync(rt => rt.Name == "Standard AC")).RoomTypeId;
                var deluxeId = (await context.RoomTypes.FirstAsync(rt => rt.Name == "Deluxe Suite")).RoomTypeId;
                var royalId = (await context.RoomTypes.FirstAsync(rt => rt.Name == "Royal Executive")).RoomTypeId;

                var rooms = new List<Room>
                {
                    new Room { RoomNumber = "101", RoomTypeId = standardId, Floor = 1, Status = RoomStatus.Available },
                    new Room { RoomNumber = "102", RoomTypeId = standardId, Floor = 1, Status = RoomStatus.Available },
                    new Room { RoomNumber = "201", RoomTypeId = deluxeId,   Floor = 2, Status = RoomStatus.Available },
                    new Room { RoomNumber = "202", RoomTypeId = deluxeId,   Floor = 2, Status = RoomStatus.Available },
                    new Room { RoomNumber = "301", RoomTypeId = royalId,    Floor = 3, Status = RoomStatus.Available },
                };
                context.Rooms.AddRange(rooms);
                await context.SaveChangesAsync();
            }

            // --- Seed Services ---
            if (!await context.Services.AnyAsync())
            {
                var services = new List<Service>
                {
                    new Service { ServiceName = "Breakfast Buffet",  Price = 650,   IsActive = true },
                    new Service { ServiceName = "Laundry",           Price = 350,   IsActive = true },
                    new Service { ServiceName = "Airport Pickup",    Price = 1200,  IsActive = true },
                    new Service { ServiceName = "Extra Bed",         Price = 800,   IsActive = true },
                };
                context.Services.AddRange(services);
                await context.SaveChangesAsync();
            }
        }
    }
}
