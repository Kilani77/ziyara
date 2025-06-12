using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Net;
using System.Net.Mail;
using System.Numerics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MedicalTourismApp.Controllers
{
    [Authorize(Roles = "Guide")]

    public class GuideController : Controller
    {

        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _usermanger;
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public GuideController(UserManager<User> usermanger, ISubscriptionService subscriptionService, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _roleManager = roleManager;
            _context = context;
            _usermanger = usermanger;
            _subscriptionService = subscriptionService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Dashboarder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var guide = await _context.Guides.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            ViewBag.guiedId = guide.Id;
            ViewBag.view = await _context.Serviceproviders.Where(p => p.UserId == userId).Select(p => p.NumberView).FirstOrDefaultAsync();
            ViewBag.app = await _context.GuideBookings.Where(p => p.GuideId == guide.Id && p.Approve == "Approved").CountAsync();
            ViewBag.pending = await _context.GuideBookings.Where(p => p.GuideId == guide.Id && p.Approve == "Pending").CountAsync();
            ViewBag.review = await _context.Reviews.AnyAsync(p => p.ServiceProviderId == service.Id) ? await _context.Reviews.Where(p => p.ServiceProviderId == service.Id).Select(p => p.Rating).AverageAsync() : 0;
            ViewBag.name = await _context.Guides.Where(p => p.Id == guide.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            var needsSubscription = await _subscriptionService.NeedsSubscriptionAsync(userId);


            // Pass this to view
            ViewBag.NeedsSubscription = needsSubscription;

            // Get subscription details for display
            var provider = await _context.Serviceproviders
                .FirstOrDefaultAsync(sp => sp.UserId == userId);

            if (provider != null)
            {
                ViewBag.IsFreeMonthActive = provider.IsFreeMonthActive;
                ViewBag.MonthEndDate = provider.MonthEndDate;
                ViewBag.IsSubscribe = provider.IsSubscribe;
            }
            return View();
        }
        [HttpPost]
        public IActionResult Dashboarder()
        {

            return View();
        }
        [HttpGet]
        public async Task<IActionResult> aprovel()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = _context.Serviceproviders.FirstOrDefault(p => p.UserId == userId);
            var doctor = _context.Guides.FirstOrDefault(p => p.ServiceProviderId == service.Id);
            var providers1 = _context.GuideBookings
            .Include(p => p.Tourist)
            .Where(p => p.Approve != "Approved" && p.GuideId == doctor.Id)
            .AsQueryable();
            ViewBag.name = await _context.Guides.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";


            if (providers1.IsNullOrEmpty())
            {
                return RedirectToAction("Dashboarder");
            }
            var providers2 = providers1
            .Select(p => new

            {
                p.Id,
                fullname = p.Tourist.User.FullName,
                nati = p.Tourist.Nationality,
                p.BookingDate,
                p.AreaBook,
                p.Approve,



            }).ToList();

            return View(providers2.ToList());
        }
        [HttpPost]
        public async Task<IActionResult> aprovel(int id, string status)
        {

            var provider = await _context.GuideBookings.FindAsync(id);
            if (provider != null)
            {
                provider.Approve = status;
                _context.Update(provider);
                _context.SaveChanges();
            }
            var appointment = await _context.GuideBookings
           .Include(a => a.Tourist)
           .ThenInclude(t => t.User)
           .FirstOrDefaultAsync(a => a.Id == id);
            var name = appointment.Tourist.User.FullName;
            var email = appointment.Tourist.User.Email;
            var fullName = appointment.Tourist.User.FullName;
            var date = appointment.BookingDate.ToString("yyyy-MM-dd");

            string subject = "Appointment Apprved";
            string message = $"Dear {name},\r\n\r\nWe’re happy to inform you that your Guide request  on {date} has been approved.\r\n\r\n📅 Date: {date}\r\n\r\nPlease make sure to be prepared and arrive on time.\r\n\r\nIf you have any questions or need assistance, feel free to contact us anytime.\r\n\r\nThank you for using [Medical Trip] — we’re here to make your journey in Jordan safe, smooth, and enriching.\r\n\r\nWarm regards,\r\n[Your App Name] Team\r\n📧 support@Medicaltrip.com\r\n🌐 [yourapp].com\r\n\r\n";

            await SendEmailAsync(email, subject, message);

            return RedirectToAction("Aprovel");
        }
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("tripmedical88@gmail.com", "cqmyspjxolsrkfgt"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("tripmedical88@gmail.com"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }

        [HttpGet]
        public async Task<IActionResult> ProfileD()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var doctor1 = await _context.Guides.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            var doc = await _context.Guides
              .Include(d => d.ServiceProvider)
              .FirstOrDefaultAsync(d => d.Id == doctor1.Id);
            ViewBag.name = await _context.Guides.Where(p => p.Id == doctor1.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            var provider = _context.Guides.Where(p => p.Id == doctor1.Id);
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
                    Experience = p.exprienece,
                    price1 = p.startPrice,
                    language1 = p.language,
                    area = p.supportedArea,
                    email = p.ServiceProvider.User.Email,
                    location1 = p.location,
                    Number = p.ContactNumber,
                    city = p.city,
                    image1 = p.ServiceProvider.Images.OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg"
                });


                return View(doctor);
            }
        }
        [HttpGet]
        public async Task<IActionResult> editd()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var doctor = await _context.Guides.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            ViewBag.name = await _context.Guides.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";
            ViewBag.city = await _context.Guides.Where(p => p.Id == doctor.Id).Select(p => p.city).FirstOrDefaultAsync();
            ViewBag.price = await _context.Guides.Where(p => p.Id == doctor.Id).Select(p => p.startPrice).FirstOrDefaultAsync();
            ViewBag.experience = await _context.Guides.Where(p => p.Id == doctor.Id).Select(p => p.exprienece).FirstOrDefaultAsync();
            ViewBag.Email = await _context.Guides.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.Email).FirstOrDefaultAsync();
            ViewBag.lanaguage = await _context.Guides.Where(p => p.Id == doctor.Id).Select(p => p.language).FirstOrDefaultAsync();
            ViewBag.area = await _context.Guides.Where(p => p.Id == doctor.Id).Select(p => p.supportedArea).FirstOrDefaultAsync();
            var images = await _context.Images
                .Where(i => i.ServiceProviderId == service.Id)
                .ToListAsync();

            ViewBag.Images = images;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> editd(string fullname, string number, string area, string location, string experience, string price, string email, string lanaguage, string city, List<IFormFile> apartmentImages, List<int> deleteImages)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var user = await _usermanger.GetUserAsync(User);
            var doctor1 = await _context.Guides.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            var doc = await _context.Guides
              .Include(d => d.ServiceProvider)
              .FirstOrDefaultAsync(d => d.Id == doctor1.Id);
            if (!string.IsNullOrEmpty(fullname))
            {
                user.FullName = fullname;
                _context.Update(doc);


            }
            if (!string.IsNullOrEmpty(city))
            {
                doc.city = city;
                _context.Update(doc);


            }
            if (!string.IsNullOrEmpty(email))
            {
                user.Email = email;
                _context.Update(doc);


            }


            if (!string.IsNullOrEmpty(location))
            {
                doc.supportedArea = location;
                _context.Update(doc);

            }


            if (!string.IsNullOrEmpty(experience))
            {
                doc.exprienece = experience;
                _context.Update(doc);

            }
            if (!string.IsNullOrEmpty(price))
            {
                doc.startPrice = int.Parse(price);
                _context.Update(doc);

            }
            if (!string.IsNullOrEmpty(lanaguage))
            {
                doc.language = lanaguage;
                _context.Update(doc);

            }
            if (!string.IsNullOrEmpty(area))
            {
                doc.supportedArea = area;
                _context.Update(doc);

            }
            if (!string.IsNullOrEmpty(number))
            {
                user.PhoneNumber = number;
                _context.Update(doc);

            }
            if (deleteImages != null && deleteImages.Any())
            {
                foreach (var imageId in deleteImages)
                {
                    var imageToDelete = await _context.Images.FindAsync(imageId);
                    if (imageToDelete != null && imageToDelete.ServiceProviderId == service.Id)
                    {
                        // Delete physical file if needed
                        if (!string.IsNullOrEmpty(imageToDelete.FilePath))
                        {
                            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, imageToDelete.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(filePath))
                            {
                                System.IO.File.Delete(filePath);
                            }
                        }

                        _context.Images.Remove(imageToDelete);
                    }
                }
            }

            // Handle new image uploads
            if (apartmentImages != null && apartmentImages.Count > 0)
            {
                foreach (var image in apartmentImages)
                {
                    if (image.Length > 0)
                    {
                        // Create unique filename
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "Home", "img");

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        var filePath = Path.Combine(uploadsFolder, fileName);

                        // Save file to disk
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }

                        // Add image record to database
                        _context.Images.Add(new Models.Image
                        {
                            ServiceProviderId = service.Id,
                            FilePath = "/Home/img/" + fileName,
                            UploadDate = DateTime.Now
                        });
                    }
                }
            }



            await _context.SaveChangesAsync();

            return RedirectToAction("ProfileD");
        }
        [HttpGet]
        public async Task<IActionResult> History(string prname, string status, int page = 1)
        {
            ViewBag.prname = prname;
            ViewBag.status = status;
            int pageSize = 8;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = _context.Serviceproviders.FirstOrDefault(p => p.UserId == userId);
            var doctor = _context.Guides.FirstOrDefault(p => p.ServiceProviderId == service.Id);
            var providers1 = _context.GuideBookings
            .Include(p => p.Tourist)
            .Where(p => p.Approve != "Pending" && p.GuideId == doctor.Id)
            .AsQueryable();

            if (!string.IsNullOrEmpty(prname))
            {
                providers1 = providers1.Where(p => p.Tourist.User.FullName.Contains(prname));
            }
            if (!string.IsNullOrEmpty(status))
            {
                providers1 = providers1.Where(p => p.Approve == status);
            }

            var count = await providers1.CountAsync();
            var totalPages = (int)Math.Ceiling(count / (double)pageSize);
            var paginatedProviders = await providers1
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    fullname = p.Tourist.User.FullName,
                    nati = p.Tourist.Nationality,
                    p.BookingDate,
                    p.AreaBook,
                    p.Approve,

                })
                .ToListAsync();
            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.name = await _context.Guides.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            return View(paginatedProviders);
        }
    }
}
