using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Configuration.Provider;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static AdminController;
namespace MedicalTourismApp.Controllers
{
    public class TouristController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _usermanger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public TouristController(UserManager<User> usermanger, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, IConfiguration configuration)
        {
            _roleManager = roleManager;
            _context = context;
            _usermanger = usermanger;
            _configuration = configuration;
         
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (User.Identity.IsAuthenticated && User.IsInRole("Freelance"))
            {
                var userId = _usermanger.GetUserId(User);
                var freetype = _context.Freelancers
                    .Where(p => p.ServiceProvider.UserId == userId)
                    .Select(p => p.Typeofserivce)
                    .FirstOrDefault();

                TempData["type"] = freetype;
            }
        }
        public async Task<IActionResult> DoctorIndex(string providerName, string majer, string price, string flow = null, int page = 1)
        {
            int pageSize = 8; 
            var currentDate = DateTime.Now;
            ViewBag.Flow = flow;

            var providers = _context.Doctors
                .Where(p => p.ServiceProvider.Status == "Approved" &&
                            _context.Avaliabledoctors.Any(a => a.DoctorId == p.Id) &&
                            ( p.ServiceProvider.IsFreeMonthActive||p.ServiceProvider.IsSubscribe))
                .AsQueryable();

            // Apply filters before pagination
            if (!string.IsNullOrEmpty(providerName))
            {
                providers = providers.Where(p => p.ServiceProvider.User.FullName.Contains(providerName, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(majer))
            {
                providers = providers.Where(p => p.Specialization == majer);
            }
            if (!string.IsNullOrEmpty(price))
            {
                providers = providers.Where(p => p.price.ToString() == price);
            }

            // Calculate pagination metadata
            var count = await providers.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);
            var paginatedProviders = await providers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    majer = p.Specialization,
                    fullname = p.ServiceProvider.User.FullName,
                    phone = p.ServiceProvider.User.PhoneNumber,
                    email = p.ServiceProvider.User.Email,
                    price1 = p.price,
                    image = p.ServiceProvider.Images.OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg"
                })
                .ToListAsync();

            // Calculate average ratings
            foreach (var guide in providers)
            {
                var averageRating = _context.Reviews
                    .Where(r => r.ServiceProviderId == guide.ServiceProviderId)
                    .Average(r => (double?)r.Rating) ?? 0.0;
                ViewData[$"AverageRating_{guide.Id}"] = averageRating;
            }

            // Pass pagination and filter data to ViewBag
            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.ProviderName = providerName;
            ViewBag.Majer = majer;
            ViewBag.Price = price;

            return View(paginatedProviders);
        }
        public async Task<IActionResult> DetailsDoctors(int id ,string flow =null)
        {
            ViewBag.Flow = flow;
            var doc = await _context.Doctors
              .Include(d => d.ServiceProvider)
              .FirstOrDefaultAsync(d => d.Id == id);
            var provider = _context.Doctors.Where(p => p.Id == id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string viewed = Request.Cookies["viewedDoctors"];
            List<int> viewedIds = !string.IsNullOrEmpty(viewed)
                ? viewed.Split(',').Select(int.Parse).ToList()
                : new List<int>();

            if (User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                if (!viewedIds.Contains(doc.Id))
                {
                    doc.ServiceProvider.NumberView += 1;
                    _context.Update(doc);
                    await _context.SaveChangesAsync();
                    viewedIds.Add(doc.Id);
                    Response.Cookies.Append("viewedDoctors", string.Join(",", viewedIds),
                        new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30) });
                }

            }

            if (provider == null || doc == null)
            {
                return NotFound();
            }
            else
            {

                var doctor = provider
                .Select(p => new
                {
                    p.Id,
                    fullname = p.ServiceProvider.User.FullName,
                    majer = p.Specialization,
                    availalbe = p.available,
                    Experience = p.exprienece,
                    price1 = p.price,
                    Bio1 = p.Bio,
                    Location = p.location,
                    image = p.ServiceProvider.Images.OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg",
                    p.ServiceProviderId

                });

                var guide = await _context.Doctors
                    .Include(g => g.ServiceProvider)
                    .ThenInclude(sp => sp.User)
                    .FirstOrDefaultAsync(g => g.Id == id);

                var reviews = await _context.Reviews
                    .Where(r => r.ServiceProviderId == guide.ServiceProviderId && r.AppointmentType == "Doctor")
                    .Include(r => r.Tourist)
                    .ThenInclude(t => t.User)
                    .Select(r => new
                    {
                        UserName = r.Tourist.User.FullName ?? "Anonymous",
                        r.Rating,
                        CreatedAt = r.CreatedAt.ToString(),
                        r.Comment
                    })
                    .ToListAsync();

                var touristReviews = await _context.Reviews
                    .Where(r => r.ServiceProviderId == guide.ServiceProviderId && r.AppointmentType == "Doctor" && r.Tourist.UserId == userId)
                    .ToListAsync();

                ViewData["Treviews"] = touristReviews;
                ViewData["Reviews"] = reviews;

                ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];


                return View(doctor);
            }
        }
        public class BookingViewModel
        {
            public int DoctorId { get; set; }
            public string DoctorName { get; set; }
            public DateOnly? SelectedDate { get; set; }
            public List<TimeSlot> AvailableSlots { get; set; } = new List<TimeSlot>();
            public List<DateOnly> AvailableDates { get; set; } = new List<DateOnly>();
        }
        public class TimeSlot
        {
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
        }
        public IActionResult App_Doc(int Id, DateOnly date,string flow =null)
        {
            ViewBag.Flow = flow;
            var name = _context.Doctors.Where(r => r.Id == Id)
                       .Select(n => n.ServiceProvider.User.FullName)
                       .FirstOrDefault();
            var selecteddate = date;
            var availableDates = GetAvailableDatesForDoctor(Id, 365);
            if (date == DateOnly.Parse("01/01/0001"))
            {
                selecteddate = DateOnly.FromDateTime(DateTime.Now);
            }
            else
            {
                selecteddate = date;
            }
            var model = new BookingViewModel
            {
                DoctorId = Id,
                DoctorName = name,
                SelectedDate = selecteddate,
                AvailableDates = availableDates

            };

            // Get availability for this day of week
            var dayOfWeek = date.DayOfWeek;
            var availability = _context.Avaliabledoctors
                .FirstOrDefault(a => a.DoctorId == Id && a.DayOfWeek == dayOfWeek);

            if (availability != null)
            {
                // Get existing appointments 
                var existingAppointments = _context.app_Doctors
                    .Where(a => a.DoctorId == Id && a.date == date)
                    .ToList();
                // Generate available slots
                model.AvailableSlots = GenerateTimeSlots(availability.StartTime, availability.EndTime, existingAppointments.ToList());
            }

            return View("App_Doc", model);
        }
        private List<DateOnly> GetAvailableDatesForDoctor(int doctorId, int daysToCheck)
        {
            var availableDates = new List<DateOnly>();
            var today = DateOnly.FromDateTime(DateTime.Now);
            var endDate = today.AddDays(daysToCheck);

            for (var date = today; date <= endDate; date = date.AddDays(1))
            {
                var dayOfWeek = date.DayOfWeek;
                var availability = _context.Avaliabledoctors
                    .FirstOrDefault(a => a.DoctorId == doctorId && a.DayOfWeek == dayOfWeek);

                if (availability != null)
                {
                    // Check if there are any available slots (simplified check)
                    var existingAppointments = _context.app_Doctors
                        .Where(a => a.DoctorId == doctorId && a.date == date)
                        .Count();

                    var totalPossibleSlots = (availability.EndTime - availability.StartTime).TotalHours;
                    if (existingAppointments < totalPossibleSlots)
                    {
                        availableDates.Add(date);
                    }
                }
            }

            return availableDates;
        }
        [HttpPost]
        public async Task<IActionResult> App_Doc(int Id, DateOnly date, TimeSpan startTime, string flow =null)
        {
            ViewBag.Flow = flow;
            var name = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var t = _context.Tourists.Where(p => p.User.Id == name).Select(r => r.Id).FirstOrDefault();
            var test = _context.bookFreelances.Any(f => f.BookingDate == date && f.StartTime == startTime && f.TouristId == t);
            if (test)
            {
                TempData["ErrorMessage"] = "You have an Freelancer appointment in this time for. Please choose a different time.";
                return RedirectToAction("App_Doc", new { id = Id });
            }
            var appointment = new app_doctor
            {
                DoctorId = Id,
                TouristId = t,
                date = date,
                StartTime = startTime,
            };
            _context.app_Doctors.Add(appointment);
            await _context.SaveChangesAsync();
             if (flow =="easy")
             {
                return RedirectToAction("apartmentIndex", new { flow });
             }
            else if (flow=="guide")
            {
                return RedirectToAction("FreeTypes", new { flow });
            }
            else
            {
                return RedirectToAction("DoctorIndex", new { id = appointment.Id, flow });
            }
        }
        private List<TimeSlot> GenerateTimeSlots(TimeSpan start, TimeSpan end, List<app_doctor> existingAppointments)
        {
            var slots = new List<TimeSlot>();
            var current = start;

            while (current < end)
            {
                var slotEnd = current.Add(TimeSpan.FromHours(1));
                if (slotEnd > end) break;

                // Check if slot is available
                var isAvailable = !existingAppointments.Any(a =>
                    a.StartTime == current);

                if (isAvailable)
                {
                    slots.Add(new TimeSlot
                    {
                        StartTime = current,
                        EndTime = slotEnd
                    });
                }

                current = slotEnd;
            }

            return slots;
        }
        public async Task<IActionResult> Hotel(string hotelName, string location, string priceRange, string flow = null, int page = 1)
        {
            int pageSize = 8; 
            ViewBag.Flow = flow;
            var currentDate = DateTime.Now;

            var hotels = _context.hotels
                .Where(p => p.serviceprovider.Status == "Approved" &&
                            ((p.serviceprovider.IsFreeMonthActive &&
                              p.serviceprovider.MonthEndDate.HasValue &&
                              p.serviceprovider.MonthEndDate >= currentDate) ||
                             p.serviceprovider.IsSubscribe))
                .AsQueryable();

            if (!string.IsNullOrEmpty(hotelName))
            {
                hotels = hotels.Where(h => h.Name.Contains(hotelName, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(location))
            {
                hotels = hotels.Where(h => h.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(priceRange) && decimal.TryParse(priceRange, out decimal price))
            {
                hotels = hotels.Where(h => priceRange == "50" ? h.Price < 50 :
                                          priceRange == "100" ? h.Price >= 50 && h.Price <= 100 :
                                          priceRange == "150" ? h.Price > 100 && h.Price <= 150 :
                                          h.Price > 150);
            }

            var count = await hotels.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);
            var paginatedHotels = await hotels
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var hotel in paginatedHotels)
            {
                var firstImage = _context.Images
                    .Where(r => r.ServiceProviderId == hotel.ServiceProviderId)
                    .OrderBy(k => k.UploadDate)
                    .Select(k => k.FilePath)
                    .FirstOrDefault();
                ViewData[$"FirstImage_{hotel.Id}"] = firstImage ?? string.Empty;

                var averageRating = _context.Reviews
                    .Where(r => r.ServiceProviderId == hotel.ServiceProviderId)
                    .Average(r => (double?)r.Rating) ?? 0.0;
                ViewData[$"AverageRating_{hotel.Id}"] = averageRating;
            }

            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.HotelName = hotelName;
            ViewBag.Location = location;
            ViewBag.PriceRange = priceRange;

            return View(paginatedHotels);
        }
        public async Task<IActionResult> HotelDetails(int id,string flow)
        {
            ViewBag.Flow = flow;
            var provider = _context.hotels.Where(p => p.Id == id).Take(1);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!provider.Any())
            {
                return NotFound();
            }

            var guide = await _context.hotels
                .Include(g => g.serviceprovider)
                .ThenInclude(sp => sp.User)
                .Include(g => g.serviceprovider.Images)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guide == null)
            {
                return NotFound();
            }

            var free = provider
                .Select(p => new
                {
                    p.Id,
                    p.Location,
                    p.ContactNumber,
                    email = p.serviceprovider.User.Email,
                    p.Price,
                    p.HasGym,
                    p.HasPool,
                    available = p.Available,
                    des = p.Description,
                    name = p.Name,
                    p.NumberOfRooms,
                    cap = p.CapacityOfPlace,
                    p.WifiAvailability,
                    p.ParkingAvailability,
                    ServiceProviderId = guide.ServiceProviderId
                })
                .ToList();

            var images = guide.serviceprovider.Images
                ?.OrderBy(k => k.UploadDate)
                .Select(k => k.FilePath)
                .ToList() ?? new List<string>();

            var reviews = await _context.Reviews
                .Where(r => r.ServiceProviderId == guide.serviceprovider.Id && r.AppointmentType == "Hotel")
                .Include(r => r.Tourist)
                .ThenInclude(t => t.User)
                .Select(r => new
                {
                    UserName = r.Tourist.User.FullName ?? "Anonymous",
                    r.Rating,
                    CreatedAt = r.CreatedAt.ToString(),
                    r.Comment
                })
                .ToListAsync();

            var touristReviews = await _context.Reviews
                .Where(r => r.ServiceProviderId == guide.serviceprovider.Id && r.AppointmentType == "Hotel" && r.Tourist.UserId == userId)
                .ToListAsync();

            ViewData["Treviews"] = touristReviews;
            ViewData["Reviews"] = reviews;
            ViewData["Images"] = images;
            ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];

            if (User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var tourId = HttpContext.Session.GetString("tourid");
                var doctorId = HttpContext.Session.GetString("doctorid");
                if (tourId != userId || doctorId != guide.Id.ToString())
                {
                    HttpContext.Session.SetString("tourid", userId);
                    HttpContext.Session.SetString("doctorid", guide.Id.ToString());

                    guide.serviceprovider.NumberView += 1;
                    _context.Update(guide.serviceprovider);
                    await _context.SaveChangesAsync();
                }
            }

            return View(free);
        }
        [HttpGet]
        public async Task<IActionResult> GuideIndex(string guideName, string language, string area, string flow = null, int page = 1)
        {
            int pageSize = 8; 
            ViewBag.Flow = flow;
            var currentDate = DateTime.Now;
            var guides = _context.Guides
                .Include(o => o.ServiceProvider)
                .ThenInclude(sp => sp.User)
                .Where(p => p.ServiceProvider.Status == "Approved" &&
                            ((p.ServiceProvider.IsFreeMonthActive &&
                              p.ServiceProvider.MonthEndDate.HasValue &&
                              p.ServiceProvider.MonthEndDate >= currentDate) ||
                             p.ServiceProvider.IsSubscribe))
                .AsQueryable();

            if (!string.IsNullOrEmpty(guideName))
            {
                guides = guides.Where(g => g.ServiceProvider.User.FullName.Contains(guideName));
            }
            if (!string.IsNullOrEmpty(area))
            {
                guides = guides.Where(g => g.supportedArea.Contains(area));
            }
            if (!string.IsNullOrEmpty(language))
            {
                guides = guides.Where(g => g.language.Contains(language));
            }

            var count = await guides.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);
            var paginatedGuides = await guides
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    startPrice = p.startPrice,
                    fullname = p.ServiceProvider.User.FullName,
                    phone = p.ServiceProvider.User.PhoneNumber,
                    email = p.ServiceProvider.User.Email,
                    language = p.language,
                    number = p.ContactNumber,
                    city = p.city,
                    area = p.supportedArea,
                    image = p.ServiceProvider.Images.OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg"
                })
                .ToListAsync();

            foreach (var guide in guides.Take(pageSize).Skip((page - 1) * pageSize))
            {
                var averageRating = _context.Reviews
                    .Where(r => r.ServiceProviderId == guide.ServiceProviderId)
                    .Average(r => (double?)r.Rating) ?? 0.0;
                ViewData[$"AverageRating_{guide.Id}"] = averageRating;
            }

            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.GuideName = guideName;
            ViewBag.Language = language;
            ViewBag.Area = area;
            return View(paginatedGuides);
        }

        public string GoogleMapsApiKey => _configuration["GoogleMapsApiKey"];
        public async Task<IActionResult> GuideDetails(int id,string flow=null)
        {
            ViewBag.Flow=flow;
            var guide = await _context.Guides
                .Include(g => g.ServiceProvider)
                .ThenInclude(sp => sp.User)
                .Include(sp => sp.ServiceProvider)
                .ThenInclude(sp => sp.Images.OrderBy(i => i.UploadDate))
                .FirstOrDefaultAsync(g => g.Id == id);


            if (guide == null)
            {
                return NotFound();
            }
            var provider = _context.Guides.Where(p => p.Id == id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var test = HttpContext.Session.GetString("tourid");
                var test1 = HttpContext.Session.GetString("doctorid");
                if (test != userId || test1 != guide.Id.ToString())
                {
                    HttpContext.Session.SetString("tourid", userId);
                    HttpContext.Session.SetString("doctorid", guide.Id.ToString());

                    guide.ServiceProvider.NumberView += 1;
                    _context.Update(guide);
                    await _context.SaveChangesAsync();

                }
            }
            var images = guide.ServiceProvider.Images
            ?.OrderBy(k => k.UploadDate)
            .Select(k => k.FilePath)
            .ToList() ?? new List<string>();

            var reviews = await _context.Reviews
                .Where(r => r.ServiceProviderId == guide.ServiceProviderId && r.AppointmentType == "Guide")
                .Include(r => r.Tourist)
                .ThenInclude(t => t.User)
                .Select(r => new
                {
                    UserName = r.Tourist.User.FullName ?? "Anonymous",
                    r.Rating,
                    CreatedAt = r.CreatedAt.ToString(),
                    r.Comment
                })
                .ToListAsync();

            var touristReviews = await _context.Reviews
                .Where(r => r.ServiceProviderId == guide.ServiceProviderId && r.AppointmentType == "Guide" && r.Tourist.UserId == userId)
                .ToListAsync();
            ViewData["Images"] = images;
            ViewData["Treviews"] = touristReviews;
            ViewData["Reviews"] = reviews;
            ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
            return View(guide);
        }
        public class GuideBookingViewModel
        {
            public int GuideId { get; set; } // ID of the guide
            public string GuideName { get; set; } // Guide's full name
            public DateOnly? SelectedDate { get; set; } // Date selected for booking
        }
        [HttpGet]
        public IActionResult BookGuide(int id, string area, DateOnly date,string flow=null)
        {
            ViewBag.Flow = flow;
            var name = _context.Guides.Where(g => g.Id == id)
                       .Select(g => g.ServiceProvider.User.FullName)
                       .FirstOrDefault();
            var selecteddate = date;
            if (date == DateOnly.Parse("01/01/0001"))
            {
                selecteddate = DateOnly.FromDateTime(DateTime.Now);
            }
            else
            {
                selecteddate = date;
            }
            var model = new GuideBookingViewModel
            {
                GuideId = id,
                GuideName = name,
                SelectedDate = date
            };
            TempData["SuccessMessage"] = null;
            TempData["area"] = area;
            return View(model);
        }
        [HttpPost]
        public IActionResult BookGuide(int id, DateOnly date,string flow=null)
        {
            ViewBag.Flow = flow;
            var guide = _context.Guides.FirstOrDefault(g => g.Id == id);
            if (guide == null)
            {
                return NotFound();
            }

            var name = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var t = _context.Tourists.Where(p => p.User.Id == name).Select(r => r.Id).FirstOrDefault();

            var isAvailable = !_context.GuideBookings.Any(a =>
                    a.BookingDate == date && guide.Id == id);

            if (isAvailable)
            {
                var booking = new GuideBooking
                {
                    TouristId = t,
                    GuideId = id,
                    AreaBook = TempData["area"].ToString(),
                    BookingDate = date,
                };

                _context.GuideBookings.Add(booking);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Your booking request has been sent to the guide for approval.";
                if (flow==null)
                {
                    return RedirectToAction("GuideIndex");
                }
                else if(flow=="easy")
                {
                    return RedirectToAction("FreeTypes", new {flow});
                }
                else
                {
                    return RedirectToAction("ApartmentIndex", new { flow });

                }
            }
            else
            {
                TempData["SuccessMessage"] = "there is an appointemt in this date.";
                var rebuild = _context.Guides.Where(g => g.Id == id)
                      .Select(g => g.ServiceProvider.User.FullName)
                      .FirstOrDefault();
                var model = new GuideBookingViewModel
                {
                    GuideId = id,
                    GuideName = rebuild,
                    SelectedDate = date
                };
                return View(model);
            }
        }
        public class ApartmentBookingViewModel
        {
            public int ApartmentId { get; set; }
            public List<string> BookedDates { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> ApartmentIndex(string providerName, string location, string cap, string numberofrooms, string price, string flow = null, int page = 1)
        {
            int pageSize = 8; 
            ViewBag.Flow = flow;
            var currentDate = DateTime.Now;

            var providers = _context.apartments
                .Where(p => p.serviceprovider.Status == "Approved" &&
                            ((p.serviceprovider.IsFreeMonthActive &&
                              p.serviceprovider.MonthEndDate.HasValue &&
                              p.serviceprovider.MonthEndDate >= currentDate) ||
                             p.serviceprovider.IsSubscribe))
                .AsQueryable();

            if (!string.IsNullOrEmpty(providerName))
            {
                providers = providers.Where(p => p.Name.Contains(providerName, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(location))
            {
                providers = providers.Where(p => p.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(cap) && int.TryParse(cap, out int capacity))
            {
                providers = providers.Where(p => p.CapacityOfPlace >= capacity);
            }
            if (!string.IsNullOrEmpty(numberofrooms) && int.TryParse(numberofrooms, out int rooms))
            {
                providers = providers.Where(p => p.NumberOfRooms >= rooms);
            }
            if (!string.IsNullOrEmpty(price) && int.TryParse(price, out int maxPrice))
            {
                providers = providers.Where(p => p.Price <= maxPrice);
            }

            // Calculate pagination metadata
            var count = await providers.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);
            var paginatedProviders = await providers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    location = p.Location,
                    phone = p.ContactNumber,
                    email = p.serviceprovider.User.Email,
                    price1 = p.Price,
                    hasbalcony = p.HasBalcony,
                    haskitchen = p.HasKitchen,
                    available = p.Available,
                    des = p.Description,
                    name = p.Name,
                    numberofroom = p.NumberOfRooms,
                    cap = p.CapacityOfPlace
                })
                .ToListAsync();

            foreach (var apartment in providers.Take(pageSize).Skip((page - 1) * pageSize))
            {
                var firstImage = _context.Images
                    .Where(r => r.ServiceProviderId == apartment.ServiceProviderId)
                    .OrderBy(k => k.UploadDate)
                    .Select(k => k.FilePath)
                    .FirstOrDefault();
                ViewData[$"FirstImage_{apartment.Id}"] = firstImage ?? string.Empty;

                var averageRating = _context.Reviews
                    .Where(r => r.ServiceProviderId == apartment.ServiceProviderId)
                    .Average(r => (double?)r.Rating) ?? 0.0;
                ViewData[$"AverageRating_{apartment.Id}"] = averageRating;
            }

            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.ProviderName = providerName;
            ViewBag.Location = location;
            ViewBag.Cap = cap;
            ViewBag.NumberOfRooms = numberofrooms;
            ViewBag.Price = price;

            return View(paginatedProviders);
        }
        public async Task<IActionResult> apartmenDetails(int id,string flow=null)
        {
            ViewBag.Flow = flow;
            var provider = _context.apartments.Where(p => p.Id == id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (provider == null)
            {
                return NotFound();
            }
            var guide = await _context.apartments
            .Include(g => g.serviceprovider)
            .ThenInclude(sp => sp.User)
            .Include(g => g.serviceprovider.Images)
            .FirstOrDefaultAsync(g => g.Id == id);
            var free = provider
            .Select(p => new
            {
                p.Id,
                location = p.Location,
                phone = p.ContactNumber,
                email = p.serviceprovider.User.Email,
                price1 = p.Price,
                hasbalcony = p.HasBalcony,
                haskitchen = p.HasKitchen,
                available = p.Available,
                des = p.Description,
                name = p.Name,
                numberofroom = p.NumberOfRooms,
                cap = p.CapacityOfPlace,
                ServiceProviderId = guide.ServiceProviderId

            });

            var images = guide.serviceprovider.Images
                    ?.OrderBy(k => k.UploadDate)
                    .Select(k => k.FilePath)
                    .ToList() ?? new List<string>();

            var reviews = await _context.Reviews
           .Where(r => r.ServiceProviderId == guide.ServiceProviderId && r.AppointmentType == "Apartment")
           .Include(r => r.Tourist)
           .Select(r => new
           {
               UserName = r.Tourist.User.FullName,
               r.Rating,
               r.CreatedAt,
               r.Comment,
           })
           .ToListAsync();
            var touristreviews = await _context.Reviews
           .Where(r => r.ServiceProviderId == guide.ServiceProviderId && r.AppointmentType == "Apartment" && r.Tourist.User.Id == userId).ToListAsync();
            ViewData["Treviews"] = touristreviews;
            ViewData["Reviews"] = reviews;
            ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
            ViewData["Images"] = images;
            var providerfr = _context.apartments.Where(p => p.Id == id);
            if (User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var test = HttpContext.Session.GetString("tourid");
                var test1 = HttpContext.Session.GetString("doctorid");
                if (test != userId || test1 != guide.Id.ToString())
                {
                    HttpContext.Session.SetString("tourid", userId);
                    HttpContext.Session.SetString("doctorid", guide.Id.ToString());

                    guide.serviceprovider.NumberView += 1;
                    _context.Update(guide);
                    await _context.SaveChangesAsync();

                }
            }
            return View(free);
        }

        public async Task<IActionResult> App_Apar(int Id,string flow =null)
        {
            ViewBag.Flow = flow;
            var bookedDates = _context.calapartments
                .Where(b => b.ApartmentId == Id && b.Enddate.HasValue)
                .AsEnumerable() // Switch to in-memory processing
                .SelectMany(b => Enumerable.Range(0, (b.Enddate.Value - b.Startdate).Days)
                    .Select(offset => b.Startdate.AddDays(offset).ToString("yyyy-MM-dd")))
                .ToList();
            var guide = await _context.apartments
              .Include(g => g.serviceprovider)
              .ThenInclude(sp => sp.User)
              .FirstOrDefaultAsync(g => g.Id == Id);

            var provider = _context.apartments.Where(p => p.Id == Id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var test = HttpContext.Session.GetString("tourid");
                var test1 = HttpContext.Session.GetString("doctorid");
                if (test != userId || test1 != guide.Id.ToString())
                {
                    HttpContext.Session.SetString("tourid", userId);
                    HttpContext.Session.SetString("doctorid", guide.Id.ToString());

                    guide.serviceprovider.NumberView += 1;
                    _context.Update(guide);
                    await _context.SaveChangesAsync();

                }
            }
            var model = new ApartmentBookingViewModel
            {
                ApartmentId = Id,
                BookedDates = bookedDates
            };
            ViewBag.BookedDates = bookedDates;
            ViewBag.ApartmentId = Id;

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> App_Apar(int Id, string bookingDates, int es,string flow=null)
        {
            ViewBag.Flow=flow;
            if (string.IsNullOrEmpty(bookingDates))
            {
                ModelState.AddModelError("", "Please select valid dates.");
                return ReturnWithError(Id);
            }

            var dates = bookingDates.Split(" to "); // Fix the date split issue
            if (dates.Length != 2)
            {
                ModelState.AddModelError("", "Invalid date range.");
                return ReturnWithError(Id);
            }

            DateTime checkIn = DateTime.Parse(dates[0]);
            DateTime checkOut = DateTime.Parse(dates[1]);

            // Check if dates are already booked
            bool isAlreadyBooked = _context.calapartments.Any(b =>
                b.ApartmentId == Id &&
                (b.Startdate < checkOut && b.Enddate > checkIn));

            if (isAlreadyBooked)
            {
                ModelState.AddModelError("", "Selected dates are already booked.");
                return ReturnWithError(Id);
            }

            var name = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var t = _context.Tourists.Where(p => p.UserId == name).Select(r => r.Id).FirstOrDefault();

            var booking = new calapartment
            {
                ApartmentId = Id,
                TouristId = t,
                Startdate = checkIn,
                Enddate = checkOut,
                numberEscorts = es,
            };

            _context.calapartments.Add(booking);
            _context.SaveChanges();
            if (flow!=null)
            {
                return RedirectToAction("Hotel", new { flow });
            }

            else 
            { 
                return RedirectToAction("apartmentIndex");
            }
        }
        private IActionResult ReturnWithError(int Id)
        {
            var bookedDates = _context.calapartments
                .Where(b => b.ApartmentId == Id && b.Enddate.HasValue)
                .AsEnumerable()
                .SelectMany(b => Enumerable.Range(0, (b.Enddate.Value - b.Startdate).Days)
                    .Select(offset => b.Startdate.AddDays(offset).ToString("yyyy-MM-dd")))
                .ToList();

            var model = new ApartmentBookingViewModel
            {
                ApartmentId = Id,
                BookedDates = bookedDates
            };

            return View("App_Apar", model);
        }
        public async Task<IActionResult> FreeTypes(string flow=null)
        {
            ViewBag.Flow=flow;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> FreeTypes(string type,string flow=null)
        {
            ViewBag.Flow=flow;
            TempData["type"] = type;
            return RedirectToAction("FreeIndex");
        }
        [HttpGet]
        public async Task<IActionResult> FreeIndex(string FreeName, string city, string language, string type = null, int page = 1)
        {
            int pageSize = 8; 
            var currentDate = DateTime.Now;

            if (string.IsNullOrEmpty(type))
            {
                type = TempData["type"]?.ToString();
                if (string.IsNullOrEmpty(type))
                {
                    return RedirectToAction("FreeTypes");
                }
            }
            ViewBag.Type = type; 
            TempData["type"] = type; 

            var freelancers = _context.Freelancers
                .Include(p => p.ServiceProvider)
                .ThenInclude(sp => sp.User)
                .Where(g => g.ServiceProvider.Status == "Approved" &&
                            g.Typeofserivce == type &&
                            (type == "nursing" || _context.AvaliableFree.Any(a => a.FreeId == g.Id)) &&
                            ((g.ServiceProvider.IsFreeMonthActive &&
                              g.ServiceProvider.MonthEndDate.HasValue &&
                              g.ServiceProvider.MonthEndDate >= currentDate) ||
                             g.ServiceProvider.IsSubscribe))
                .AsQueryable();

            // Apply filters before pagination
            if (!string.IsNullOrEmpty(FreeName))
            {
                freelancers = freelancers.Where(g => g.ServiceProvider.User.FullName.Contains(FreeName, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(city))
            {
                freelancers = freelancers.Where(g => g.supportedArea.Contains(city, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrEmpty(language))
            {
                freelancers = freelancers.Where(g => g.language.Contains(language, StringComparison.OrdinalIgnoreCase));
            }

            var count = await freelancers.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);
            var paginatedFreelancers = await freelancers
                .OrderBy(g => g.Id) 
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            foreach (var freelancer in paginatedFreelancers)
            {
                var firstImage = _context.Images
                    .Where(r => r.ServiceProviderId == freelancer.ServiceProviderId)
                    .OrderBy(k => k.UploadDate)
                    .Select(k => k.FilePath)
                    .FirstOrDefault();
                ViewData[$"FirstImage_{freelancer.Id}"] = firstImage ?? string.Empty;

                var averageRating = _context.Reviews
                    .Where(r => r.ServiceProviderId == freelancer.ServiceProviderId)
                    .Average(r => (double?)r.Rating) ?? 0.0;
                ViewData[$"AverageRating_{freelancer.Id}"] = averageRating;
            }

            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.FreeName = FreeName;
            ViewBag.City = city;
            ViewBag.Language = language;

            return View(paginatedFreelancers);
        }
        public async Task<IActionResult> FreeDetails(int id)
        {
            var provider = _context.Freelancers.Where(p => p.Id == id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (provider == null)
            {
                return NotFound();
            }

            var free = provider
            .Select(p => new
            {
                p.Id,
                fullname = p.ServiceProvider.User.FullName,
                majer = p.Typeofserivce,
                availalbe = p.available,
                experience = p.exprienece,
                price1 = p.price,
                language1 = p.language,
                location1 = p.supportedArea,
                p.ServiceProviderId


            });

            var guide = await _context.Freelancers
                .Include(g => g.ServiceProvider)
                .ThenInclude(sp => sp.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            var reviews = await _context.Reviews
                .Where(r => r.ServiceProviderId == guide.ServiceProviderId && r.AppointmentType == "Freelancer")
                .Include(r => r.Tourist)
                .ThenInclude(t => t.User)
                .Select(r => new
                {
                    UserName = r.Tourist.User.FullName ?? "Anonymous",
                    r.Rating,
                    CreatedAt = r.CreatedAt.ToString(),
                    r.Comment
                })
                .ToListAsync();

            var touristReviews = await _context.Reviews
                .Where(r => r.ServiceProviderId == guide.ServiceProviderId && r.AppointmentType == "Doctor" && r.Tourist.UserId == userId)
                .ToListAsync();

            ViewData["Treviews"] = touristReviews;
            ViewData["Reviews"] = reviews;


            var providerfr = _context.Freelancers.Where(p => p.Id == id);
            if (User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var test = HttpContext.Session.GetString("tourid");
                var test1 = HttpContext.Session.GetString("doctorid");
                if (test != userId || test1 != guide.Id.ToString())
                {
                    HttpContext.Session.SetString("tourid", userId);
                    HttpContext.Session.SetString("doctorid", guide.Id.ToString());

                    guide.ServiceProvider.NumberView += 1;
                    _context.Update(guide);
                    await _context.SaveChangesAsync();

                }
            }
            return View(free);



        }
        public class FreeBookingViewModel
        {
            public int FreeId { get; set; }
            public string name { get; set; }
            public List<string> BookedDates { get; set; }
        }
        public async Task<IActionResult> FreeNurse(int Id,string flow=null)
        {
            ViewBag.Flow=flow;
            // Handle invalid ID
            if (Id <= 0)
            {
                return NotFound();
            }
            var name = _context.Freelancers.Where(r => r.Id == Id)
                       .Select(n => n.ServiceProvider.User.FullName)
                       .FirstOrDefault();
            var bookedDates = _context.bookFreelances
                .Where(b => b.FreelanceId == Id && b.StartDate.HasValue && b.EndDate.HasValue)
                .AsEnumerable()
                .SelectMany(b => Enumerable.Range(0, (b.EndDate.Value - b.StartDate.Value).Days + 1)
                    .Select(offset => b.StartDate.Value.AddDays(offset).ToString("yyyy-MM-dd")))

                .ToList();

            var model = new FreeBookingViewModel
            {
                FreeId = Id,
                name = name,
                BookedDates = bookedDates
            };


            ViewBag.BookedDates = bookedDates;
            ViewBag.FreeId = Id;

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> FreeNurse(int Id, string bookingDates ,string flow=null)
        {
            ViewBag.Flow = flow;
            if (string.IsNullOrEmpty(bookingDates))
            {
                ModelState.AddModelError("", "Please select valid dates.");
                return await ReloadBookingView(Id);
            }


            try
            {
                DateTime startDate;
                DateTime endDate;

                if (bookingDates.Contains(" to "))
                {
                    var dates = bookingDates.Split(" to ");

                    if (dates.Length != 2 ||
                        !DateTime.TryParse(dates[0], out startDate) ||
                        !DateTime.TryParse(dates[1], out endDate))
                    {
                        ModelState.AddModelError("", "Invalid date format.");
                        return await ReloadBookingView(Id);
                    }
                }
                else
                {
                    if (!DateTime.TryParse(bookingDates, out startDate))
                    {
                        ModelState.AddModelError("", "Invalid date format.");
                        return await ReloadBookingView(Id);
                    }

                    endDate = startDate;
                }

                bool isOverlapping = _context.bookFreelances.Any(b =>
                    b.FreelanceId == Id &&
                    b.StartDate.HasValue && b.EndDate.HasValue &&
                    !(endDate < b.StartDate.Value || startDate > b.EndDate.Value));

                if (isOverlapping)
                {
                    ModelState.AddModelError("", "Selected dates are already booked.");
                    return await ReloadBookingView(Id);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var touristId = _context.Tourists
                    .Where(p => p.UserId == userId)
                    .Select(r => r.Id)
                    .FirstOrDefault();

                if (touristId == 0)
                {
                    ModelState.AddModelError("", "User profile not found.");
                    return await ReloadBookingView(Id);
                }

                var booking = new BookFreelance
                {
                    FreelanceId = Id,
                    TouristId = touristId,
                    StartDate = startDate,
                    EndDate = endDate,
                    BookingDate = DateOnly.FromDateTime(DateTime.Today),

                };

                _context.bookFreelances.Add(booking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your booking request has been submitted successfully!";
                return RedirectToAction("FreeTypes");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while processing your booking.");
                return await ReloadBookingView(Id);
            }
        }
        private async Task<IActionResult> ReloadBookingView(int nurseId,string flow=null)
        {
            ViewBag.Flow = flow;
            var bookedDates = _context.bookFreelances
                .Where(b => b.FreelanceId == nurseId && b.StartDate.HasValue && b.EndDate.HasValue)
                .AsEnumerable()
                .SelectMany(b => Enumerable.Range(0, (b.EndDate.Value - b.StartDate.Value).Days + 1)
                    .Select(offset => b.StartDate.Value.AddDays(offset).ToString("yyyy-MM-dd")))
                .ToList();

            var model = new FreeBookingViewModel
            {
                FreeId = nurseId,
                BookedDates = bookedDates
            };

            ViewBag.BookedDates = bookedDates;
            ViewBag.FreeId = nurseId;

            return View(model);
        }
        public class BookingViewModel1
        {
            public int FreeId { get; set; }
            public string FreeName { get; set; }
            public DateOnly? SelectedDate { get; set; }
            public List<TimeSlot1> AvailableSlots1 { get; set; } = new List<TimeSlot1>();
        }
        public class TimeSlot1
        {
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }
        }
        public IActionResult FreeAp(int Id, DateOnly date,string flow=null)
        {
            ViewBag.Flow = flow;
            var name = _context.Freelancers.Where(r => r.Id == Id)
                       .Select(n => n.ServiceProvider.User.FullName)
                       .FirstOrDefault();
            var selecteddate = date;
            if (date == DateOnly.Parse("01/01/0001"))
            {
                selecteddate = DateOnly.FromDateTime(DateTime.Now);
            }
            else
            {
                selecteddate = date;
            }
            var model = new BookingViewModel1
            {
                FreeId = Id,
                FreeName = name,
                SelectedDate = date
            };

            // Get availability for this day of week
            var dayOfWeek = date.DayOfWeek;
            var availability = _context.AvaliableFree
                .FirstOrDefault(a => a.FreeId == Id && a.DayOfWeek == dayOfWeek);

            if (availability != null)
            {
                // Get existing appointments 
                var existingAppointments = _context.bookFreelances
                    .Where(a => a.FreelanceId == Id && a.BookingDate == date)
                    .ToList();
                // Generate available slots
                model.AvailableSlots1 = GenerateTimeSlots(availability.StartTime, availability.EndTime, existingAppointments.ToList());
            }

            return View("FreeAp", model);
        }
        [HttpPost]
        public async Task<IActionResult> FreeAp(int Id, DateOnly date, TimeSpan startTime ,string flow=null)
        {
            ViewBag.Flow=flow;
            var name = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var t = _context.Tourists.Where(p => p.User.Id == name).Select(r => r.Id).FirstOrDefault();
            var test = _context.app_Doctors.Any(f => f.date == date && f.StartTime == startTime && f.TouristId == t);
            if (test)
            {
                TempData["ErrorMessage"] = "You have an Doctor appointment in this time. Please choose a different time.";
                return RedirectToAction("FreeAp", new { id = Id });
            }
            var appointment = new BookFreelance
            {
                FreelanceId = Id,
                TouristId = t,
                BookingDate = date,
                StartTime = startTime,
            };

            _context.bookFreelances.Add(appointment);
            await _context.SaveChangesAsync();
            if (flow==null)
            {
                return RedirectToAction("FreeIndex");
            }
            else { 
                return RedirectToPage("FreeTypes", new { id = appointment.Id, flow });
            }
        }
        private List<TimeSlot1> GenerateTimeSlots(TimeSpan start, TimeSpan end, List<BookFreelance> existingAppointments)
        {
            var slots = new List<TimeSlot1>();
            var current = start;

            while (current < end)
            {
                var slotEnd = current.Add(TimeSpan.FromHours(1));
                if (slotEnd > end) break;

                // Check if slot is available
                var isAvailable = !existingAppointments.Any(a =>
                    a.StartTime == current);

                if (isAvailable)
                {
                    slots.Add(new TimeSlot1
                    {
                        StartTime = current,
                        EndTime = slotEnd
                    });
                }

                current = slotEnd;
            }

            return slots;
        }
        public async Task<IActionResult> carIndex(string modelcar, string city, string numberseat, string price, bool driveravl,string flow=null, int page = 1)
        {
            ViewBag.Flow=flow;
            int pageSize = 8;

            var currentDate = DateTime.Now;
            var car = _context.Transportations  
                .Include(g => g.ServiceProvider)
                .ThenInclude(sp => sp.User)
                .Where(g => g.ServiceProvider.Status == "Approved" &&
                            (g.ServiceProvider.IsFreeMonthActive &&
                             g.ServiceProvider.MonthEndDate.HasValue &&
                             g.ServiceProvider.MonthEndDate >= currentDate) ||
                            g.ServiceProvider.IsSubscribe)
                .AsQueryable();
            if (!string.IsNullOrEmpty(modelcar))
            {
                car = car.Where(g => g.ServiceProvider.User.FullName.Contains(modelcar));
            }
            if (!string.IsNullOrEmpty(city))
            {
                car = car.Where(g => g.Location.Contains(city));
            }
            if (!string.IsNullOrEmpty(numberseat))
            {
                car = car.Where(p => p.Numberofseats <= int.Parse(numberseat));
            }
            if (!string.IsNullOrEmpty(price))
            {
                car = car.Where(p => p.PricePerDay >= int.Parse(price));
            }
            if (driveravl != false)
            {
                car = car.Where(p => p.DriverAvalablity == driveravl);
            }
            foreach (var guide in car)
            {
                var firstImage = _context.Images
                .Where(r => r.ServiceProviderId == guide.ServiceProviderId)?.OrderBy(k => k.UploadDate)
                .Select(k => k.FilePath)
                .FirstOrDefault();
                ViewData[$"FirstImage_{guide.Id}"] = firstImage ?? string.Empty;

                var averageRating = _context.Reviews
                    .Where(r => r.ServiceProviderId == guide.ServiceProviderId)
                    .Average(r => (double?)r.Rating) ?? 0.0;
                ViewData[$"AverageRating_{guide.Id}"] = averageRating;
            }
            var count = await car.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);



            var paginatedProviders =  car
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();

            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.modelcar = modelcar;
            ViewBag.city = city;
            ViewBag.numberseat = numberseat;
            ViewBag.driveravl = driveravl;
            ViewBag.price = price;
            return View(paginatedProviders);
        }
        public async Task<IActionResult> carDetails(int id,string flow =null)
        {
            ViewBag.Flow = flow;
            var provider = _context.Transportations.Where(p => p.Id == id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (provider == null)
            {
                return NotFound();
            }
            var guide = await _context.Transportations
            .Include(g => g.ServiceProvider)
            .ThenInclude(sp => sp.User)
            .Include(g => g.ServiceProvider.Images)
            .FirstOrDefaultAsync(g => g.Id == id);
            var car = provider
            .Select(p => new
            {
                p.Id,
                fullname = p.FullName,
                model = p.modelocar,
                availalbe = p.DriverAvalablity,
                contact = p.ContactNumber,
                price1 = p.PricePerDay,
                Bio1 = p.Bio,
                location1 = p.Location,
                number = p.Numberofseats,
                ServiceProviderId = guide.ServiceProviderId
            });

            // Get all images ordered by UploadDate
            var images = guide.ServiceProvider.Images
                ?.OrderBy(k => k.UploadDate)
                .Select(k => k.FilePath)
                .ToList() ?? new List<string>();

            var reviews = await _context.Reviews
                .Where(r => r.ServiceProviderId == guide.ServiceProvider.Id && r.AppointmentType == "Car")
                .Include(r => r.Tourist)
                .ThenInclude(t => t.User)
                .Select(r => new
                {
                    UserName = r.Tourist.User.FullName ?? "Anonymous",
                    r.Rating,
                    CreatedAt = r.CreatedAt.ToString(),
                    r.Comment
                })
                .ToListAsync();

            var touristReviews = await _context.Reviews
                .Where(r => r.ServiceProviderId == guide.ServiceProvider.Id && r.AppointmentType == "Car" && r.Tourist.UserId == userId)
                .ToListAsync();

            ViewData["Treviews"] = touristReviews;
            ViewData["Reviews"] = reviews;
            ViewData["GoogleMapsApiKey"] = _configuration["GoogleMapsApiKey"];
            ViewData["Images"] = images;
            var providercar = _context.Transportations.Where(p => p.Id == id);
            if (User.Identity.IsAuthenticated && User.IsInRole("Patient"))
            {
                var test = HttpContext.Session.GetString("tourid");
                var test1 = HttpContext.Session.GetString("doctorid");
                if (test != userId || test1 != guide.Id.ToString())
                {
                    HttpContext.Session.SetString("tourid", userId);
                    HttpContext.Session.SetString("doctorid", guide.Id.ToString());

                    guide.ServiceProvider.NumberView += 1;
                    _context.Update(guide);
                    await _context.SaveChangesAsync();

                }
            }


            return View(car);



        }
        public class carBookingViewModel
        {
            public int carId { get; set; }
            public List<string> BookedDates { get; set; }
        }
        public async Task<IActionResult> carBook(int Id,string flow=null)
        {
            ViewBag.Flow=flow;
            var bookedDates = _context.bookCars
                .Where(b => b.carId == Id && b.Enddate.HasValue)
                .AsEnumerable() // Switch to in-memory processing
                .SelectMany(b => Enumerable.Range(0, (b.Enddate.Value - b.Startdate).Days)
                    .Select(offset => b.Startdate.AddDays(offset).ToString("yyyy-MM-dd")))
                .ToList();
            var model = new carBookingViewModel
            {
                carId = Id,
                BookedDates = bookedDates
            };
            ViewBag.BookedDates = bookedDates;
            ViewBag.ApartmentId = Id;
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> carBook(int Id, string bookingDates,string flow=null)
        {
            ViewBag.Flow=flow;
            if (string.IsNullOrEmpty(bookingDates))
            {
                ModelState.AddModelError("", "Please select valid dates.");
                return View(Id);
            }
            var dates = bookingDates.Split(" to "); // Fix the date split issue
            if (dates.Length != 2)
            {
                ModelState.AddModelError("", "Invalid date range.");
                return View(Id);
            }
            DateTime checkIn = DateTime.Parse(dates[0]);
            DateTime checkOut = DateTime.Parse(dates[1]);
            // Check if dates are already booked
            bool isAlreadyBooked = _context.bookCars.Any(b =>
                b.carId == Id &&
                (b.Startdate < checkOut && b.Enddate > checkIn));
            if (isAlreadyBooked)
            {
                ModelState.AddModelError("", "Selected dates are already booked.");
                return View(Id);
            }
            var name = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var t = _context.Tourists.Where(p => p.UserId == name).Select(r => r.Id).FirstOrDefault();

            var cars = new BookCar
            {
                carId = Id,
                TouristId = t,
                Startdate = checkIn,
                Enddate = checkOut,

            };          
            _context.bookCars.Add(cars);
            _context.SaveChanges();
            if (flow=="easy")
            {
                return RedirectToAction("GuideIndex", new { flow });

            }
            else if (flow=="guide")
            {
                return RedirectToAction("DoctorIndex", new { flow });
            }
            else
            {
                return RedirectToAction("carIndex");
            }
        }
        [HttpPost]
        public async Task<IActionResult> SubmitReview(string comment, int serviceProviderId, int rating, string appointmentType, string returnUrl)
        {
            try
            {
                if (!User.Identity.IsAuthenticated || !User.IsInRole("Patient"))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return StatusCode(403, new { success = false, message = "Please log in to submit a review." });
                    }
                    return RedirectToAction("Login", "Account", new { area = "Identity", returnUrl });
                }
                if (string.IsNullOrWhiteSpace(comment))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return StatusCode(400, new { success = false, message = "Comment cannot be empty." });
                    }
                    TempData["ReviewError"] = "Comment cannot be empty.";
                    return Redirect(returnUrl);
                }
                var detector = new ProfanityFilter.ProfanityFilter();
                if (detector.ContainsProfanity(comment) || ContainsArabicProfanity(comment))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return StatusCode(400, new { success = false, message = "Your review contains inappropriate language." });
                    }
                    TempData["ReviewError"] = "Your review contains inappropriate language.";
                    return Redirect(returnUrl);
                }
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var tourist = await _context.Tourists
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(p => p.UserId == userId);
                if (tourist == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return StatusCode(400, new { success = false, message = "User profile not found. Please complete your profile." });
                    }
                    TempData["ReviewError"] = "User profile not found. Please complete your profile.";
                    return Redirect(returnUrl);
                }
                var serviceProvider = await _context.Serviceproviders.FindAsync(serviceProviderId);
                if (serviceProvider == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return StatusCode(404, new { success = false, message = "Service provider not found." });
                    }
                    TempData["ReviewError"] = "Service provider not found.";
                    return Redirect(returnUrl);
                }
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.ServiceProviderId == serviceProviderId && r.TouristId == tourist.Id && r.AppointmentType == appointmentType);
                if (existingReview != null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return StatusCode(400, new { success = false, message = "You have already reviewed this service." });
                    }
                    TempData["ReviewError"] = "You have already reviewed this service.";
                    return Redirect(returnUrl);
                }
                var review = new Review
                {
                    Comment = comment,
                    TouristId = tourist.Id,
                    ServiceProviderId = serviceProviderId,
                    Rating = rating,
                    AppointmentType = appointmentType,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.Reviews.AddAsync(review);
                await _context.SaveChangesAsync();
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = true,
                        message = "Review submitted successfully.",
                        review = new
                        {
                            UserName = tourist.User.FullName ?? "Anonymous",
                            Rating = rating,
                            Comment = comment,
                            CreatedAt = review.CreatedAt.ToString()
                        }
                    });
                }
                TempData["SuccessMessage"] = "Review submitted successfully.";
                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return StatusCode(500, new { success = false, message = "An unexpected error occurred. Please try again." });
                }
                TempData["ReviewError"] = "An unexpected error occurred. Please try again.";
                return Redirect(returnUrl);
            }
        }
        private static readonly List<string> ArabicProfanityWords = new List<string>
        {
            // Basic Arabic profanity words
            "كس", "طيز", "زب", "خرا", "كسم", "نيك", "متناك",
            "خول", "عرص", "شرموط", "قحبة", "منيوك", "زبر", "ابن العاهرة",
            "ميتين", "يلعن", "حيوان", "كلب", "حمار", "زق", "خرة",
               "غبي", "حقير", "سخيف", "تافه", "لعنة", "مغفل", "قذر", "أحمق",
                "كسول", "فاشل", "حيوان", "وسخ", "قليل الأدب", "تخلف", "غبية",
            "👉👌", "🖕", " احا", " احه", " اير ", " لعين", " واطي", "ابن ال", "ابن المرا", "ابن المرة", "ابن النيك",
            "ابن عاهر", "ابن كلب", "ابو شخة", "ابو شخه", "ابو فص", "اجا معي", "اجري فيك", "احلي كث", "احيه",
            "اخو ال", "اخو القحبه", "افسخك", "اقلب وجهك", "الخرائ", "الزب", "السافل", "الساقط", "العايب",
            "العربان", "العرص", "العمى", "القحبة", "الكحبة", "الكحبه", "الكس", "الكلب", "الله ياخ", "انت عبيط",
            "انت غبي", "انذال", "انذل", "انعل ابو", "انكح", "انيك", "انيكك", "اهبل", "اونطة", "اونطه", "اونطي",
            "ايري ب", "ايري ف", "ايري", "ايور", "بزاز", "بعبص", "بعص", "بغاي", "بندوق", "بهيمة", "تافه",
            "تجليخ", "ترهيط", "تزغيب", "تسد بوزك", "تفو", "جلخ", "جلق", "حرامي", "حقير", "حلبتها", "حلبتو",
            "حلمات", "حمير", "حيوان", "خرا", "خراء", "خراي عل", "خراي", "خرة", "خرى", "خري", "خسيس", "خنيث",
            "خوازيق", "خول", "داشر", "داعر", "دعارة", "دلخ", "ديوث", "ديود", "زامل", " زب", "زبار", "زبالة",
            "زباله", "زبر", "زبه", "زبي", "زراط", "زق", "زناة", "زناطير", "ساذج", "سارموتا", "سافل", "سربوط",
            "سرموتا", "سفالة", "سكس", "سكسي", "سيكس", "سيكسي", "شرمها", "شرموط", "شرموطة", "شرموطه", "شلقة",
            "شلكة", "صايع", "صياعة", "ضرب عشرة", "طز في", "طيز", "عاهر", "عاهرة", "عايبة", "عبيط", "عديم الشرف",
            "عرص", "عكروت", "عيال الحرام", "غبي", "غتصب", "فاجر", "فاسق", "فجور", "فسختها", "قحاب", "قحب",
            "قحبة", "قذر", "قضيب كبير", "قضيبي", "كحبة", "كذاب", "كس ", "كس اختك", "كس امك", "كس عرضك", "كسا",
            "كسمك", "كسمكم", "كسها", "كل خرا", "كل خرة", "كل زق", "كلاب", "كلب", "كلخر", "كلكم اولاد",
            "كلكم ولاد", "كول خر", "لحس", "لعنه", "لقحاب", "لوطي", "مأجور", "مبعوص", "متخوزق", "متناك",
            "مجنون", "مخانيث", "مخنث", "مدلس", "معوهر", "مفسوخ", "مكسكس", "مكوتها", "ملعون", "ممحون", "منايك",
            "منيك", "منيوك", "ناكك", "نجس", "نذل", "نفضك", "نفظك", "نكت اخته", "نكت امه", "نياكة", "نياكه",
            "هاذي اختك", "هاذي امك", "هذي اختك", "هذي امك", "واحد اهبل", "وسخ", "ولد القحبة", "ولد القحبه",
            "يا ابن ال", "يا اخوات ال", "يا خوات ال", "يا رخيص", "يا زنديق", "يا غبي", "يا كافر", "يا هبيلة",
            "يا ولاد ال", "يتناك", "يجيب ضهرو", "يخلع نيعك", "يسود وجه", "يزغب", "يفضح", "يفظح", "يولاد ال",
            "يلعن","حمار","منيك","ك س", "ط ي ز", "ز ب", "خ ر ا", "ك س م", "ن ي ك",
            "kos", "teez", "zeb", "khara", "kosomak", "neek", "metnak"
        };
        public static bool ContainsArabicProfanity(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            string normalizedText = text.ToLower();

            foreach (var word in ArabicProfanityWords)
            {
                string pattern = $@"\b{Regex.Escape(word)}\b";
                if (Regex.IsMatch(normalizedText, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }

                if (normalizedText.Contains(word))
                {
                    return true;
                }
            }

            // No profanity found
            return false;
        }
    }
}