using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MedicalTourismApp.Models;
using MedicalTourismApp.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MedicalTourismApp.Controllers
{

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _usermanger;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        //omar///
        // Constructor for Dependency Injection
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration, UserManager<User> usermanger, EmailService emailService)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
            _usermanger = usermanger;
            _emailService = emailService;
           
          
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
        public async Task<IActionResult> Index()
        {
           
            var testimonials = await _context.testimonials
                       .Include(t => t.User)
                       .Where(t => t.Status == true) 
                       .OrderByDescending(t => t.Date)
                       .ToListAsync();

            return View(testimonials);
        }
        public IActionResult WadiRum()
        {
            return View();
        }
        public IActionResult DeadSea()
        {
            return View();
        }
        public IActionResult homaOrdoonia()
        {
            return View();
        }
        public IActionResult maein()
        {
            return View();
        }
        public IActionResult Afra()
        {
            return View();
        }
        public IActionResult contact()
        {
            return View();
        }

        public class TouristAppointmentsViewModel
        {
            public List<app_doctor> DoctorAppointments { get; set; }
            public List<calapartment> Apartments { get; set; }
            public List<BookFreelance> Freelances { get; set; }
            public List<GuideBooking> Guides { get; set; }
            public List<BookCar> Cars { get; set; }
            public List<Review> Reviews { get; set; } = new List<Review>();

            public bool HasReview(DateTime date, TimeSpan? time, int serviceProviderId, string appointmentType)
            {
                return Reviews.Any(r =>
                    r.ServiceProviderId == serviceProviderId &&
                    r.AppointmentType == appointmentType &&
                    r.AppointmentDate == date &&
                    (time == null || r.AppointmentTime == time));
            }
        }
        public static class ArabicProfanityChecker
        {
            private static readonly HashSet<string> ArabicBadWords = new HashSet<string>
          {
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
            "يلعن","حمار","منيك"

    };

            public static bool ContainsArabicProfanity(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return false;

                // Normalize input: remove diacritics, extra spaces, and convert to lowercase
                input = Regex.Replace(input.Normalize(NormalizationForm.FormD), @"\p{M}", "").ToLower();
                input = Regex.Replace(input, @"\s+", " ").Trim();

                foreach (var badWord in ArabicBadWords)
                {
                    var normalizedBadWord = Regex.Replace(badWord.Normalize(NormalizationForm.FormD), @"\p{M}", "").ToLower();
                    var regex = $@"\b{Regex.Escape(normalizedBadWord)}\b";
                    if (Regex.IsMatch(input, regex))
                        return true;
                }
                return false;
            }
        }
        public class TestimonialViewModel
        {
            [Required]
            [Display(Name = "Your Testimonial")]
            [StringLength(500, ErrorMessage = "Testimonial must be less than 500 characters")]
            public string Message { get; set; }
        }
        public IActionResult Appointments()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tourist = _context.Tourists
                .Where(p => p.UserId == userid)
                .Select(r => r.Id)
                .FirstOrDefault();
            if (tourist == 0) 
            {
                return View(new TouristAppointmentsViewModel());
            }
        
            var viewModel = new TouristAppointmentsViewModel
            {
                DoctorAppointments = _context.app_Doctors
                    .Where(p => p.TouristId == tourist)
                        .Include(d => d.Doctor)
                        .ThenInclude(d => d.ServiceProvider)
                            .ThenInclude(sp => sp.User)
                    .ToList(),
                Apartments = _context.calapartments
                    .Where(p => p.TouristId == tourist)
                    .Include(d => d.apartment)
                        .ThenInclude(d => d.serviceprovider)
                            .ThenInclude(sp => sp.User)
                    .ToList(),
                Freelances = _context.bookFreelances
                    .Where(p => p.TouristId == tourist)
                    .Include(d => d.Freelancer)
                        .ThenInclude(d => d.ServiceProvider)
                            .ThenInclude(sp => sp.User)
                    .ToList(),
                Guides = _context.GuideBookings
                    .Where(p => p.TouristId == tourist)
                    .Include(d => d.Guide)
                        .ThenInclude(d => d.ServiceProvider)
                            .ThenInclude(sp => sp.User)
                    .ToList(),
                Cars = _context.bookCars
                    .Where(p => p.TouristId == tourist)
                    .Include(d => d.Transportation)
                        .ThenInclude(d => d.ServiceProvider)
                            .ThenInclude(sp => sp.User)
                    .ToList(),
                Reviews = _context.Reviews
                     .Where(r => r.TouristId == tourist)
                     .ToList()
            };
            
            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> Appointments(string comment, int serviceid, int rating, string appointmentDate, string appointmentTime, string appointmentType)
        {
            var detector = new ProfanityFilter.ProfanityFilter();

            if (detector.ContainsProfanity(comment) || ArabicProfanityChecker.ContainsArabicProfanity(comment))
            {
                _logger.LogWarning("Profanity detected in comment by user {UserId}: {Comment}", User.FindFirstValue(ClaimTypes.NameIdentifier), comment);
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Your review contains inappropriate language." });
                }
                TempData["ReviewError"] = "Your review contains inappropriate language.";
                return RedirectToAction("Appointments");
            }

            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var tourist = _context.Tourists.Where(p => p.UserId == userid).FirstOrDefault();
            if (tourist == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "User not found." });
                }
                TempData["ReviewError"] = "User not found.";
                return RedirectToAction("Appointments");
            }

            var review = new Review
            {
                Comment = comment,
                TouristId = tourist.Id,
                ServiceProviderId = serviceid,
                Rating = rating,
                AppointmentDate = DateTime.Parse(appointmentDate),
                AppointmentType = appointmentType,
                AppointmentTime = string.IsNullOrEmpty(appointmentTime) ? null : TimeSpan.Parse(appointmentTime),
            };

            await _context.AddAsync(review);
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }

            return RedirectToAction("Appointments");
        }
        
        public IActionResult Offers()
        {
            var plans = _context.planDetails
                       .Where(p => p.IsPopular == true) // Add IsActive to your model if needed
                       .OrderBy(p => p.Months)
                       .AsQueryable();

            var model = plans
                 .Select(p => new
                 {
                     p.Id,
                     p.Price,
                     p.IsPopular,
                     p.SavingsPercentage,
                     p.SavingsText,
                     p.Months,

                 }).ToList();


            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                var user = await _usermanger.GetUserAsync(User);

                var testimonial = new Testimonial
                {
                    UserId = user.Id,
                    Message = message,
                    Name= user.FullName,
                    Status = false, // Needs admin approval
                    Date = DateTime.Now
                };

                _context.testimonials.Add(testimonial);
                await _context.SaveChangesAsync();

                TempData["AlertMessage"] = "Thank you for your testimonial! It will be reviewed by our team.";
                TempData["AlertType"] = "success";
            }
            else
            {
                TempData["AlertMessage"] = "Please enter a testimonial message.";
                TempData["AlertType"] = "danger";
            }

            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
        public IActionResult SelectPlan(int selectedPlan)
        {
            var plan = _context.planDetails.FirstOrDefault(p => p.Months == selectedPlan);
            if (plan == null)
            {
                return BadRequest("Invalid plan selected.");
            }

            HttpContext.Session.SetString("id", plan.Id.ToString());
            HttpContext.Session.SetString("price", plan.Price.ToString());
            HttpContext.Session.SetString("Month", plan.Months.ToString());

            return RedirectToAction("Pay");
        }
        public async Task<IActionResult> Pay()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var check = _context.Serviceproviders.Where(p => p.UserId == userId);
            if (check == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Pay(string cardName, string cardNumber, int expMonth, int expYear, string cardCvc)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _usermanger.GetUserAsync(User);
            var check = await _context.Serviceproviders
                        .Include(d => d.User)
                        .FirstOrDefaultAsync(d => d.UserId == user.Id);
            var planId = int.Parse(HttpContext.Session.GetString("id"));
            var planPrice = HttpContext.Session.GetString("price");
            var plan = _context.planDetails.FirstOrDefault(p => p.Id == planId);
            if (plan == null)
            {
                ViewData["Error"] = "Invalid plan selected.";
                return View("Pay");
            }
            var bankAccount = _context.BankAccounts.FirstOrDefault(b => b.CardNumber == cardNumber);
            if (bankAccount == null)
            {
                ViewData["Error"] = "No bank account found. Please contact support.";
                return View("Pay");
            }

            if (bankAccount.Name != cardName ||
            bankAccount.CardNumber != cardNumber ||
            bankAccount.Month != expMonth ||
            bankAccount.Year != expYear ||
            bankAccount.Cvc != int.Parse(cardCvc))
            {
                ViewData["Error"] = "Invalid card details. Please check your information.";
                return View("Pay");
            }
            if (cardNumber.Length != 16 || cardCvc.Length < 3 || expYear < DateTime.Now.Year || (expYear == DateTime.Now.Year && expMonth < DateTime.Now.Month))
            {
                ViewData["Error"] = "Invalid card details.";
                return View("Pay");
            }
            if (bankAccount.Amount < double.Parse(planPrice.ToString()))
            {
                ViewData["Error"] = "Insufficient funds in your account.";
                return View("Pay");
            }
            bankAccount.Amount -= double.Parse(planPrice.ToString());
            await _context.SaveChangesAsync();
            int month = int.Parse(HttpContext.Session.GetString("Month").ToString());
            check.MonthEndDate = DateTime.Now.AddMonths(month);
            check.IsSubscribe = true;
            check.IsFreeMonthActive = false;
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AboutUs(string name, string email, string subject, string message)
        {
            var body = $"<p><strong>Name:</strong> {name}</p>" +
                       $"<p><strong>Email:</strong> {email}</p>" +
                       $"<p><strong>Subject:</strong> {subject}</p>" +
                       $"<p><strong>Message:</strong><br>{message}</p>";

            await _emailService.SendEmailAsync("tripmedical88@gmail.com", "New Contact Message", body);

            TempData["Message"] = "Message sent successfully!";
            return RedirectToAction("Contact");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
