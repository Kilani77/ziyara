using Azure;
using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Configuration.Provider;
using System.Net;
using System.Net.Mail;
using System.Numerics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MedicalTourismApp.Controllers
{
    [Authorize(Roles = "Doctor")]

    public class DoctorController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _usermanger;
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        public DoctorController(UserManager<User> usermanger, ISubscriptionService subscriptionService, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
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
            var doctor = await _context.Doctors.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            ViewBag.view = await _context.Serviceproviders.Where(p => p.UserId == userId).Select(p => p.NumberView).FirstOrDefaultAsync();
            ViewBag.app = await _context.app_Doctors.Where(p => p.DoctorId == doctor.Id && p.Approve == "Approved").CountAsync();
            ViewBag.pending = await _context.app_Doctors.Where(p => p.DoctorId == doctor.Id && p.Approve == "Pending").CountAsync();
            ViewBag.review = await _context.Reviews.AnyAsync(p => p.ServiceProviderId == service.Id) ? await _context.Reviews.Where(p => p.ServiceProviderId == service.Id).Select(p => p.Rating).AverageAsync() : 0;
            ViewBag.name = await _context.Doctors.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            var needsSubscription = await _subscriptionService.NeedsSubscriptionAsync(userId);

            ViewBag.NeedsSubscription = needsSubscription;

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
            var doctor = _context.Doctors.FirstOrDefault(p => p.ServiceProviderId == service.Id);
            var providers1 = _context.app_Doctors
            .Include(p => p.Tourist)
            .Where(p => p.Approve != "Approved" && p.DoctorId == doctor.Id)
            .AsQueryable();
            ViewBag.name = await _context.Doctors.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
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
                p.date,
                p.StartTime,
                p.Approve,
                endtime = p.StartTime.Add(TimeSpan.FromHours(1)),

            }).ToList();

            return View(providers2.ToList());
        }
        [HttpPost]
        public async Task<IActionResult> aprovel(int id, string status)
        {

            var provider = await _context.app_Doctors.FindAsync(id);
            if (provider != null)
            {
                provider.Approve = status;
                _context.Update(provider);
                _context.SaveChanges();
            }
            var appointment = await _context.app_Doctors
               .Include(a => a.Tourist)
               .ThenInclude(t => t.User)
               .FirstOrDefaultAsync(a => a.Id == id);
            var name = appointment.Tourist.User.FullName;
            var email = appointment.Tourist.User.Email;
            var fullName = appointment.Tourist.User.FullName;
            var date = appointment.date.ToString("yyyy-MM-dd");

            string subject = "Appointment Apprved";
            string message = $"Dear {name},\r\n\r\nWe’re happy to inform you that your Doctor request  on {date} has been approved.\r\n\r\n📅 Date: {date}\r\n\r\nPlease make sure to be prepared and arrive on time.\r\n\r\nIf you have any questions or need assistance, feel free to contact us anytime.\r\n\r\nThank you for using [Medical Trip] — we’re here to make your journey in Jordan safe, smooth, and enriching.\r\n\r\nWarm regards,\r\n[Your App Name] Team\r\n📧 support@Medicaltrip.com\r\n🌐 [yourapp].com\r\n\r\n";

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
            var doctor1 = await _context.Doctors.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            var doc = await _context.Doctors
              .Include(d => d.ServiceProvider)
              .FirstOrDefaultAsync(d => d.Id == doctor1.Id);
            ViewBag.name = await _context.Doctors.Where(p => p.Id == doctor1.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            var provider = _context.Doctors.Where(p => p.Id == doctor1.Id);
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
                    Experience = p.exprienece,
                    price1 = p.price,
                    Bio1 = p.Bio,
                    location1 = p.location,
                    email = p.ServiceProvider.User.Email,
                    image1= p.ServiceProvider.Images.OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg"
                });


                return View(doctor);
            }
        }
        [HttpGet]
        public async Task<IActionResult> editd()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            ViewBag.name = await _context.Doctors.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";
            ViewBag.majer = await _context.Doctors.Where(p => p.Id == doctor.Id).Select(p => p.Specialization).FirstOrDefaultAsync();
            ViewBag.price = await _context.Doctors.Where(p => p.Id == doctor.Id).Select(p => p.price).FirstOrDefaultAsync();
            ViewBag.experience = await _context.Doctors.Where(p => p.Id == doctor.Id).Select(p => p.exprienece).FirstOrDefaultAsync();
            ViewBag.Email = await _context.Doctors.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.Email).FirstOrDefaultAsync();
            ViewBag.bio = await _context.Doctors.Where(p => p.Id == doctor.Id).Select(p => p.Bio).FirstOrDefaultAsync();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> editd(string fullname, string majer, string location, string experience, string price, string email, string bio, IFormFile image)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _usermanger.GetUserAsync(User);

            if (user == null)
            {
                return NotFound("User not found");
            }

            var service = await _context.Serviceproviders
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (service == null)
            {
                return NotFound("Service provider not found");
            }

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);

            if (doctor == null)
            {
                return NotFound("Doctor profile not found");
            }

            if (image != null && image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("image", "Invalid file type. Only JPG, PNG, GIF are allowed.");
                    return View("ProfileD", doctor);
                }

                if (image.Length > 2 * 1024 * 1024) // 2MB
                {
                    ModelState.AddModelError("image", "File size exceeds 2MB limit.");
                    return View("ProfileD", doctor);
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
            var doctor1 = await _context.Doctors.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);
            var doc = await _context.Doctors
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
            if (!string.IsNullOrEmpty(bio))
            {
                doc.Bio = bio;
                _context.Update(doc);

            }

            if (!string.IsNullOrEmpty(location))
            {
                doc.location = location;
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
            if (!string.IsNullOrEmpty(majer))
            {
                doc.Specialization = majer;
                _context.Update(doc);

            }


            await _context.SaveChangesAsync();

            return RedirectToAction("ProfileD");
        }
        public async Task<IActionResult> Available()
        {
            var user= User.FindFirstValue(ClaimTypes.NameIdentifier);
            var serviceid = _context.Serviceproviders.FirstOrDefault(p => p.UserId == user);
            var docid = _context.Doctors.FirstOrDefault(p => p.ServiceProviderId == serviceid.Id);
            ViewBag.name = await _context.Doctors.Where(p => p.Id == docid.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == serviceid.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            // Get existing availability records for this doctor
            var availabilities = _context.Avaliabledoctors
                .Where(a => a.DoctorId == docid.Id)
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
        [HttpGet]
        public async Task<IActionResult> History(string name,string status, int page = 1)
        {
            ViewBag.prname = name;
            ViewBag.status = status;
            int pageSize = 8;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = _context.Serviceproviders.FirstOrDefault(p => p.UserId == userId);
            var doctor = _context.Doctors.FirstOrDefault(p => p.ServiceProviderId == service.Id);
            var providers1 = _context.app_Doctors
            .Include(p => p.Tourist)
            .Where(p => p.Approve != "Pending" && p.DoctorId == doctor.Id)
            .AsQueryable();
            
            if (!string.IsNullOrEmpty(name))
            {
                providers1 = providers1.Where(p => p.Tourist.User.FullName.Contains(name));
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
                    p.date,
                    p.StartTime,
                    p.Approve,
                    endtime = p.StartTime.Add(TimeSpan.FromHours(1)),
                })
                .ToListAsync();
            ViewBag.PageIndex = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.HasPreviousPage = page > 1;
            ViewBag.HasNextPage = page < totalPages;
            ViewBag.name = await _context.Doctors.Where(p => p.Id == doctor.Id).Select(p => p.ServiceProvider.User.FullName).FirstOrDefaultAsync();
            ViewBag.img = _context.Images.Where(p => p.ServiceProviderId == service.Id).OrderBy(i => i.UploadDate).Select(i => i.FilePath).FirstOrDefault() ?? "/images/default-doctor.jpg";

            return View(paginatedProviders);
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
                    var doctor = _context.Doctors.FirstOrDefault(sp => sp.ServiceProviderId == service.Id);
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

                // Check if this day was marked as available
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
                        var existingAvailability = _context.Avaliabledoctors
                            .FirstOrDefault(a => a.DoctorId == doctorId &&
                                              a.DayOfWeek == day &&
                                              a.StartTime == startTime &&
                                              a.EndTime == endTime);

                        // If not exists, add it
                        if (existingAvailability == null)
                        {
                            var doctorData = new Avaliabledoctor
                            {
                                DoctorId = doctorId,
                                DayOfWeek = day,
                                StartTime = startTime,
                                EndTime = endTime
                            };
                            _context.Avaliabledoctors.Add(doctorData);
                            savedAny = true;
                        }
                    }

                    // Process additional time slots (up to 3)
                    for (int i = 0; i < 3; i++)
                    {
                        if (formData.ContainsKey($"startTime_{dayName}_{i}") && formData.ContainsKey($"endTime_{dayName}_{i}") &&
                            !string.IsNullOrEmpty(formData[$"startTime_{dayName}_{i}"]) && !string.IsNullOrEmpty(formData[$"endTime_{dayName}_{i}"]))
                        {
                            // Parse times
                            TimeSpan startTime = TimeSpan.Parse(formData[$"startTime_{dayName}_{i}"]);
                            TimeSpan endTime = TimeSpan.Parse(formData[$"endTime_{dayName}_{i}"]);

                            // Skip if end time is before or equal to start time
                            if (endTime <= startTime)
                            {
                                continue;
                            }

                            // Check if this time slot already exists
                            var existingAvailability = _context.Avaliabledoctors
                                .FirstOrDefault(a => a.DoctorId == doctorId &&
                                                  a.DayOfWeek == day &&
                                                  a.StartTime == startTime &&
                                                  a.EndTime == endTime);

                            // If not exists, add it
                            if (existingAvailability == null)
                            {
                                var doctorData = new Avaliabledoctor
                                {
                                    DoctorId = doctorId,
                                    DayOfWeek = day,
                                    StartTime = startTime,
                                    EndTime = endTime
                                };
                                _context.Avaliabledoctors.Add(doctorData);
                                savedAny = true;
                            }
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
        
    }
    
}

