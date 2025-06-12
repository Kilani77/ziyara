using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using System.Numerics;
using Microsoft.AspNetCore.Authorization;

namespace MedicalTourismApp.Controllers
{
    [Authorize(Roles = "Freelance")]
    public class PhsyController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _usermanger;
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;

        public PhsyController(UserManager<User> usermanger, ISubscriptionService subscriptionService, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _roleManager = roleManager;
            _context = context;
            _usermanger = usermanger;
            _subscriptionService = subscriptionService;
        }
        public async Task<IActionResult> Dashboard(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var doctor = await _context.Freelancers.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            ViewBag.view = await _context.Serviceproviders.Where(p => p.UserId == userId).Select(p => p.NumberView).FirstOrDefaultAsync();
            ViewBag.app = await _context.bookFreelances.Where(p => p.FreelanceId == doctor.Id && p.Approve == true).CountAsync();
            ViewBag.pending = await _context.bookFreelances.Where(p => p.FreelanceId == doctor.Id && p.Approve == null).CountAsync();
            ViewBag.review = await _context.Reviews.AnyAsync(p => p.ServiceProviderId == service.Id) ? await _context.Reviews.Where(p => p.ServiceProviderId == service.Id).Select(p => p.Rating).AverageAsync() : 0;
            var needsSubscription = await _subscriptionService.NeedsSubscriptionAsync(userId);
            ViewBag.name = await _context.Freelancers.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

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
        public IActionResult Dashboard()
        {

            return View();
        }
        [HttpGet]
        public async Task<IActionResult> aprovel()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = _context.Serviceproviders.FirstOrDefault(p => p.UserId == userId);
            var doctor = _context.Freelancers.FirstOrDefault(p => p.ServiceProviderId == service.Id);
            var providers1 = _context.bookFreelances
            .Include(p => p.Tourist)
            .Where(p => p.Approve != true && p.FreelanceId == doctor.Id)
            .AsQueryable();

            ViewBag.name = await _context.Freelancers.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            if (providers1.IsNullOrEmpty())
            {
                return RedirectToAction("Dashboard");
            }
            var providers2 = providers1
            .Select(p => new

            {
                p.Id,
                fullname = p.Tourist.User.FullName,
                nati = p.Tourist.Nationality,
                p.BookingDate,
                p.StartTime,
                p.Approve,
                p.EndTime,

            }).ToList();

            return View(providers2.ToList());
        }
        [HttpPost]
        public async Task<IActionResult> aprovel(int id, bool status)
        {

            var provider = await _context.bookFreelances.FindAsync(id);
            if (provider != null)
            {
                provider.Approve = status;
                _context.Update(provider);
                _context.SaveChanges();
            }
            var appointment = await _context.bookFreelances
               .Include(a => a.Tourist)
               .ThenInclude(t => t.User)
               .FirstOrDefaultAsync(a => a.Id == id);
            var name = appointment.Tourist.User.FullName;
            var email = appointment.Tourist.User.Email;
            var fullName = appointment.Tourist.User.FullName;
            var date = appointment.BookingDate;

            string subject = "Appointment Apprved";
            string message = $"Dear {name},\r\n\r\nWe’re happy to inform you that your physical therapy request  on {date} has been approved.\r\n\r\n📅 Date: {date}\r\n\r\nPlease make sure to be prepared and arrive on time.\r\n\r\nIf you have any questions or need assistance, feel free to contact us anytime.\r\n\r\nThank you for using [Medical Trip] — we’re here to make your journey in Jordan safe, smooth, and enriching.\r\n\r\nWarm regards,\r\n[Your App Name] Team\r\n📧 support@Medicaltrip.com\r\n🌐 [yourapp].com\r\n\r\n";

            await SendEmailAsync(email, subject, message);

            return RedirectToAction("Aprovel");
        }

        public async Task<IActionResult> ProfileD()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var doctor1 = await _context.Freelancers.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            var doc = await _context.Freelancers
              .Include(d => d.ServiceProvider)
              .FirstOrDefaultAsync(d => d.Id == doctor1.Id);
            ViewBag.name = await _context.Freelancers.Where(p => p.Id == doctor1.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            var provider = _context.Freelancers.Where(p => p.Id == doctor1.Id);
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
                    majer = p.Typeofserivce,
                    Experience = p.exprienece,
                    price1 = p.price,
                    language1 = p.language,
                    ava = p.available,
                    location1 = p.supportedArea,
                    email = p.ServiceProvider.User.Email,
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
            var doctor1 = await _context.Freelancers.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            ViewBag.name = await _context.Freelancers.Where(p => p.Id == doctor1.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";
            ViewBag.majer = await _context.Freelancers.Where(p => p.Id == doctor1.Id).Select(p => p.Typeofserivce).FirstOrDefaultAsync();
            ViewBag.price = await _context.Freelancers.Where(p => p.Id == doctor1.Id).Select(p => p.price).FirstOrDefaultAsync();
            ViewBag.experience = await _context.Freelancers.Where(p => p.Id == doctor1.Id).Select(p => p.exprienece).FirstOrDefaultAsync();
            ViewBag.Email = await _context.Freelancers.Where(p => p.Id == doctor1.Id).Select(p => p.ServiceProvider.User.Email).FirstOrDefaultAsync();
            ViewBag.lanaguage = await _context.Freelancers.Where(p => p.Id == doctor1.Id).Select(p => p.language).FirstOrDefaultAsync();
            ViewBag.available = await _context.Freelancers.Where(p => p.Id == doctor1.Id).Select(p => p.available).FirstOrDefaultAsync();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> editd(string fullname, string location, string experience, string price, string email, string lanaguage, string ava, IFormFile image)
        {


            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var user = await _usermanger.GetUserAsync(User);
            var doctor1 = await _context.Freelancers.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            var doc = await _context.Freelancers
              .Include(d => d.ServiceProvider)
              .FirstOrDefaultAsync(d => d.Id == doctor1.Id);

            if (!string.IsNullOrEmpty(fullname))
            {
                user.FullName = fullname;
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
                doc.price = int.Parse(price);
                _context.Update(doc);

            }
            if (!string.IsNullOrEmpty(lanaguage))
            {
                doc.language = lanaguage;
                _context.Update(doc);

            }
            if (!string.IsNullOrEmpty(ava))
            {
                doc.available = ava;
                _context.Update(doc);


            }
            if (image != null && image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("image", "Invalid file type. Only JPG, PNG, GIF are allowed.");
                    return View("ProfileD", doctor1);
                }

                if (image.Length > 2 * 1024 * 1024) // 2MB
                {
                    ModelState.AddModelError("image", "File size exceeds 2MB limit.");
                    return View("ProfileD", doctor1);
                }

                // Get or create image record
                var imageRecord = await _context.Images
                    .FirstOrDefaultAsync(d => d.ServiceProviderId == service.Id)
                    ?? new Image { ServiceProviderId = service.Id };

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadsFolder = Path.Combine("wwwroot", "Home", "img");
                Directory.CreateDirectory(uploadsFolder);
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Update database record
                imageRecord.FilePath = $"/Home/img/{fileName}";
                imageRecord.UploadDate = DateTime.UtcNow;

                if (imageRecord.Id == 0)
                {
                    _context.Images.Add(imageRecord);
                }
                else
                {
                    _context.Images.Update(imageRecord);
                }
            }



            await _context.SaveChangesAsync();

            return RedirectToAction("ProfileD");
        }
        public async Task<IActionResult> Available()
        {
            var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var serviceid = _context.Serviceproviders.FirstOrDefault(p => p.UserId == user);
            var docid = _context.Freelancers.FirstOrDefault(p => p.ServiceProviderId == serviceid.Id);
            ViewBag.name = await _context.Freelancers.Where(p => p.Id == docid.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == serviceid.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            // Get existing availability records for this doctor
            var availabilities = _context.AvaliableFree
                .Where(a => a.FreeId == docid.Id)
                .ToList();

            List<AvailabilityViewModel> viewModel = new List<AvailabilityViewModel>();

            foreach (var availability in availabilities)
            {
                viewModel.Add(new AvailabilityViewModel
                {
                    Id = availability.Id,
                    Day = availability.DayOfWeek,
                    Start = availability.StartTime,
                    End = availability.EndTime
                });
            }

            ViewBag.DoctorId = docid.Id;
            return View(viewModel);
        }
        [HttpPost]
        public IActionResult Available(Dictionary<string, string> formData)
        {
            int doctorId = 0;
            if (formData.ContainsKey("doctorId") && int.TryParse(formData["doctorId"], out doctorId))
            {
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var service = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == userId);
                if (service != null)
                {
                    var doctor = _context.Freelancers.FirstOrDefault(sp => sp.ServiceProviderId == service.Id);
                    if (doctor != null)
                    {
                        doctorId = doctor.Id;
                    }
                }
            }

            if (doctorId == 0)
            {
                TempData["Error"] = "Could not determine doctor ID. Please try again.";
                return RedirectToAction("Available", new { id = doctorId });
            }



            bool savedAny = false;

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                string dayName = day.ToString();

                if (formData.ContainsKey($"available{dayName}") && formData[$"available{dayName}"] == "on")
                {
                    // Process primary time slot
                    if (formData.ContainsKey($"startTime_{dayName}") && formData.ContainsKey($"endTime_{dayName}") &&
                        !string.IsNullOrEmpty(formData[$"startTime_{dayName}"]) && !string.IsNullOrEmpty(formData[$"endTime_{dayName}"]))
                    {
                        // Parse times
                        TimeSpan startTime = TimeSpan.Parse(formData[$"startTime_{dayName}"]);
                        TimeSpan endTime = TimeSpan.Parse(formData[$"endTime_{dayName}"]);

                        // Skip if end time is before or equal to start time
                        if (endTime <= startTime)
                        {
                            continue;
                        }

                        // Check if this time slot already exists
                        var existingAvailability = _context.AvaliableFree
                            .FirstOrDefault(a => a.FreeId == doctorId &&
                                              a.DayOfWeek == day &&
                                              a.StartTime == startTime &&
                                              a.EndTime == endTime);

                        // If not exists, add it
                        if (existingAvailability == null)
                        {
                            var doctorData = new avaliableFree
                            {
                                FreeId = doctorId,
                                DayOfWeek = day,
                                StartTime = startTime,
                                EndTime = endTime
                            };
                            _context.AvaliableFree.Add(doctorData);
                            savedAny = true;
                        }
                    }

                }
            }

            // Save changes and set message
            if (savedAny)
            {
                _context.SaveChanges();
                TempData["Success"] = "Availability saved successfully!";
            }
            else
            {
                TempData["Error"] = "No new availability was added. Please select The Day before entering the Time.";
            }

            return RedirectToAction("Available", new { id = doctorId });
        }
        [HttpGet]
        public async Task<IActionResult> History(string prname, bool status, int page = 1)
        {
            ViewBag.prname = prname;
            ViewBag.status = status;
            int pageSize = 8;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = _context.Serviceproviders.FirstOrDefault(p => p.UserId == userId);
            var doctor = _context.Freelancers.FirstOrDefault(p => p.ServiceProviderId == service.Id);
            var providers1 = _context.bookFreelances
            .Include(p => p.Tourist)
            .Where(p => p.Approve != null && p.FreelanceId == doctor.Id)
            .AsQueryable();

            if (!string.IsNullOrEmpty(prname))
            {
                providers1 = providers1.Where(p => p.Tourist.User.FullName.Contains(prname));
            }
            if (status != null)
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
                    p.StartTime,
                    p.Approve,
                    endtime = p.EndTime,
                })
                .ToListAsync();
            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.name = await _context.Freelancers.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            return View(paginatedProviders);
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

    }
}
