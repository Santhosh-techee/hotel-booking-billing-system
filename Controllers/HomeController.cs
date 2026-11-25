using System.Diagnostics;
using HotelBookingSystem.Models;
using HotelBookingSystem.Services;
using HotelBookingSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRoomService _roomService;

        public HomeController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        public IActionResult Index()
        {
            var vm = new RoomSearchViewModel
            {
                CheckInDate = DateTime.Today,
                CheckOutDate = DateTime.Today.AddDays(1),
                GuestCount = 1
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(RoomSearchViewModel vm)
        {
            if (!ModelState.IsValid)
                return View("Index", vm);

            if (vm.CheckOutDate <= vm.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Check-out must be after check-in.");
                return View("Index", vm);
            }

            var rooms = await _roomService.SearchAvailableRoomsAsync(vm.CheckInDate, vm.CheckOutDate, vm.GuestCount);
            var results = new RoomResultsViewModel
            {
                SearchCriteria = vm,
                AvailableRooms = rooms
            };

            return View("RoomResults", results);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
