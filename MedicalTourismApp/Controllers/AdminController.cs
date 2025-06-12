using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Configuration.Provider;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Numerics;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<User> _usermanger;
    private readonly ApplicationDbContext _context;

    public AdminController(UserManager<User> usermanger , RoleManager<IdentityRole> roleManager,ApplicationDbContext context)
    {
        _roleManager = roleManager;
        _context = context;
        _usermanger = usermanger;
    }
    [Authorize(Roles = "Admin")] 
    public IActionResult Dashboard()
    {
        ViewBag.count = _context.Serviceproviders.Where(tbl=>tbl.Status== "Approved").Count();
        ViewBag.countuser = _context.Tourists.Count();
        ViewBag.pending=_context.Serviceproviders.Where(tbl=>tbl.Status=="Pending").Count();
        ViewBag.test=_context.testimonials.Where(tbl=>tbl.Status== true).Count();
        var currentYear = DateTime.Now.Year;
        var currentMonth = DateTime.Now.Month;
        var lastMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var lastMonthYear = currentMonth == 1 ? currentYear - 1 : currentYear;

        int currentMonthCount = _context.Tourists
            .Where(u => u.User.RegisteredAt.Month == currentMonth && u.User.RegisteredAt.Year == currentYear)
            .Count();

        int lastMonthCount = _context.Tourists
            .Where(u => u.User.RegisteredAt.Month == lastMonth && u.User.RegisteredAt.Year == lastMonthYear)
            .Count();

        // Avoid division by zero
        double percentChange = 0;
        if (lastMonthCount > 0)
            percentChange = ((double)(currentMonthCount - lastMonthCount) / lastMonthCount) * 100;

        ViewBag.PatientData = GetMonthlyData(); // Your counts[] array
        ViewBag.PercentChange = Math.Round(percentChange, 1);
        ViewBag.IsUp = percentChange >= 0;

        return View();
    }
    private int[] GetMonthlyData()
    {
        var currentYear = DateTime.Now.Year;
        var monthlyCounts = _context.Tourists
            .Where(u => u.User.RegisteredAt.Year == currentYear)
            .GroupBy(u => u.User.RegisteredAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToList();

        int[] counts = new int[12];
        foreach (var item in monthlyCounts)
            counts[item.Month - 1] = item.Count;

        return counts; 
    }
    [HttpGet]
    public async Task<IActionResult> ApproveList(string providerName, string serviceType, string status, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        int pageSize = 8;
        var providers = _context.Serviceproviders
            .Include(p => p.User)
            .OrderBy(p => p.Status != "pending")
            .AsQueryable();
        
        //search filters
        if (!string.IsNullOrEmpty(providerName))
        {
            providers = providers.Where(p => p.User.FullName.Contains(providerName));
        }
        if (!string.IsNullOrEmpty(serviceType))
        {
            providers = providers.Where(p => p.ServiceType == serviceType);
        }
        if (!string.IsNullOrEmpty(status))
        {
            providers = providers.Where(p => p.Status == status);
        }
        if (fromDate.HasValue)
        {
            providers = providers.Where(p => p.RegistrationTime >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            providers = providers.Where(p => p.RegistrationTime <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        var count = await providers.CountAsync();
        var totalPages = (int)Math.Ceiling(count / (double)pageSize);
        var paginatedProviders = await providers
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                Id = p.Id, 
                ServiceType = p.ServiceType,
                RegistrationTime = p.RegistrationTime,
                Status = p.Status,
                fullname = p.User.FullName
            })
            .ToListAsync();

        ViewBag.PageIndex = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.HasPreviousPage = page > 1;
        ViewBag.HasNextPage = page < totalPages;
        ViewBag.ProviderName = providerName;
        ViewBag.ServiceType = serviceType;
        ViewBag.Status = status;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd"); 
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd"); 

        ViewBag.types = await _roleManager.Roles
            .Where(r => r.Name != "Admin" && r.Name != "ServiceProvider" && r.Name != "Patient")
            .Select(r => r.Name)
            .ToListAsync();

        return View(paginatedProviders);
    }
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult ApproveProvider(int id )
    {
        var provider = _context.Serviceproviders.Find(id);
        if (provider != null)
        {
            provider.Status = "Approved";
            _context.SaveChanges();
        }
        
        return RedirectToAction("ApproveList");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public IActionResult RejectProvider(int id)
    {

        var provider = _context.Serviceproviders.Find(id);
        if (provider != null)
        {
            provider.Status = "Rejected";
            _context.SaveChanges();
        }
        return RedirectToAction("ApproveList");
    }
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public IActionResult BlockProvider(int id)
    {
        var provider = _context.Serviceproviders.Find(id);
        if (provider != null)
        {
            provider.Status = "Blocked";
            _context.SaveChanges();
            
        }
        
        return RedirectToAction("ApproveList");
    }
    [HttpGet]
    public async Task<IActionResult> ProviderList(string providerName, string serviceType,string email,string phone, int page = 1)
    
    {
        int pageSize = 6;
        var providers1 = _context.Serviceproviders
            .Include(p => p.User)
            .AsQueryable();
        if (!string.IsNullOrEmpty(providerName))
        {
            providers1 = providers1.Where(p => p.User.FullName.Contains(providerName));
        }

        if (!string.IsNullOrEmpty(serviceType))
        {
            providers1 = providers1.Where(p => p.ServiceType == serviceType);
        }
        if (!string.IsNullOrEmpty(email))
        {
            providers1 = providers1.Where(p => p.User.Email == email);
        }
        if (!string.IsNullOrEmpty(phone))
        {
            providers1 = providers1.Where(p => p.User.PhoneNumber == phone);
        }
        var count = await providers1.CountAsync();
        var totalPages = (int)Math.Ceiling(count / (double)pageSize);
        var paginatedProviders = await providers1
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                Id = p.UserId, 
                ServiceType = p.ServiceType,
                fullname = p.User.FullName,
                phone = p.User.PhoneNumber,
                email = p.User.Email
            })
            .ToListAsync();
        ViewBag.PageIndex = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.HasPreviousPage = page > 1;
        ViewBag.HasNextPage = page < totalPages;
        ViewBag.ProviderName = providerName;
        ViewBag.ServiceType = serviceType;
        ViewBag.Email = email;
        ViewBag.Phone = phone;
        var AvailableServices = _roleManager.Roles.Where(r => r.Name != "Admin" && r.Name != "ServiceProvider" && r.Name != "Patient")
        .Select(r => r.Name).ToList();
        ViewBag.types = AvailableServices;
        return View(paginatedProviders.ToList());
    }
    [HttpPost]
    public IActionResult ProviderList(int id)
    {
        var provider = _context.Serviceproviders.Find(id);
        if (provider != null)
        {
            _context.Serviceproviders.Remove(provider);
            _context.SaveChanges(); 

        }

        return RedirectToAction("ProviderList");
    }
    [HttpGet]
    public async Task<IActionResult> Testimonial(bool status, DateTime? fromDate, DateTime? toDate)
    {
        ViewBag.Status = status;
        var testimonials = _context.testimonials.AsQueryable();
        if (status != null)
        {
            testimonials = testimonials.Where(p => p.Status == status);
        }
        if (fromDate.HasValue)
        {
            testimonials = testimonials.Where(p => p.Date >= fromDate.Value);
        }
        if (toDate.HasValue)
        {
            testimonials  = testimonials.Where(p => p.Date <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        }
        return View(testimonials);
    

    }
    [HttpPost]
    public async Task<IActionResult> DeleteTestimonial(int id)
    {
        var testimonial = await _context.testimonials.FindAsync(id);
        if (testimonial != null)
        {
            testimonial.Status = false;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Testimonial));
    }
    [HttpPost]
    public async Task<IActionResult> PostTestimonial(int id)
    {
        var testimonial = await _context.testimonials.FindAsync(id);
        if (testimonial != null)
        {
            testimonial.Status = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Testimonial));
    }
    public class ProviderDetailsViewModel
    {
        public Serviceprovider Provider { get; set; }
        public object ServiceDetails { get; set; }
    }
    [HttpGet]
    public IActionResult Details(int id)
    {

        var provider = _context.Serviceproviders.FirstOrDefault(p => p.Id == id);

        if (provider == null)
        {
            return NotFound();
        }

        object serviceDetails = null;

        switch (provider.ServiceType)
        {
            case "Doctor":
                serviceDetails = _context.Doctors.FirstOrDefault(d => d.ServiceProviderId == id);
                break;
            case "Hospital":
                serviceDetails = _context.Hospitals.FirstOrDefault(h => h.ServiceProviderId == id);
                break;
            case "Guide":
                serviceDetails = _context.Guides.FirstOrDefault(g => g.ServiceProviderId == id);
                break;
            case "Freelance":
                serviceDetails = _context.Freelancers.FirstOrDefault(g => g.ServiceProviderId == id);
                break;
            case "Clinic":
                serviceDetails = _context.Clinics.FirstOrDefault(g => g.ServiceProviderId == id);
                break;
            case "Transportation":
                serviceDetails = _context.Transportations.FirstOrDefault(g => g.ServiceProviderId == id);
                break;
            case "Apartment":
                serviceDetails = _context.apartments.FirstOrDefault(g => g.ServiceProviderId == provider.Id);
                break;
            case "Hotel":
                serviceDetails = _context.hotels.FirstOrDefault(g => g.ServiceProviderId == id);
                break;

            default:
                break;
        }

        var viewModel = new ProviderDetailsViewModel
        {
            Provider = provider,
            ServiceDetails = serviceDetails
        };

        return View(viewModel);


    }
    public async Task<IActionResult> Plans()
    {
        var plans = _context.planDetails.OrderBy(p => p.Months).ToList();
        return View(plans);

    }
    public async Task<IActionResult> AddPlans()
    {
         return View();
    }
    [HttpPost]
    public async Task<IActionResult> AddPlans(PlanDetails plan)
    {

        if (ModelState.IsValid)
        {
            plan.CreatedDate = DateTime.Now;
            _context.planDetails.Add(plan);
            _context.SaveChanges();
            TempData["Success"] = "Plan created successfully!";
            return RedirectToAction("Plans");
        }

        return View(plan);

    }
    [HttpGet]
    public async Task<IActionResult> DeletePlans(int id)
    {
        var testimonial = await _context.planDetails.FindAsync(id);
        if (testimonial != null)
        {
            _context.planDetails.Remove(testimonial);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Plans"); 
    } 
      public ActionResult EditPlan(int id)
    {
        var plan = _context.planDetails.Find(id);
        
        return View(plan);
    }

    // POST: Admin/Plan/Edit/5
    [HttpPost]
    public ActionResult EditPlan(PlanDetails plan)
    {
        if (ModelState.IsValid)
        {
            var existingPlan = _context.planDetails.Find(plan.Id);
            if (existingPlan != null)
            {
                existingPlan.Months = plan.Months;
                existingPlan.Price = plan.Price;
                existingPlan.IsPopular = plan.IsPopular;
                existingPlan.UpdatedDate = DateTime.Now;
                
                _context.SaveChanges();
                TempData["Success"] = "Plan updated successfully!" ;
                return RedirectToAction("Plans");
            }
        }
        return View(plan);
    }
    [HttpGet] 
    public async Task<IActionResult> PostPlans(int id) 
    {
        var testimonial = await _context.planDetails.FindAsync(id);
        if (testimonial != null)
        {
            testimonial.IsPopular = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Plans"); 
    }
    [HttpGet] 
    public async Task<IActionResult> HidePlans(int id) 
    {
        var testimonial = await _context.planDetails.FindAsync(id);
        if (testimonial != null)
        {
            testimonial.IsPopular = false;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Plans"); 
    }
    //[HttpPost]
    //public async Task<IActionResult> CreateCheckoutSession(int selectedPlan)
    //{
    //    var user = await _usermanger.GetUserAsync(User);
    //    var plan = _context.planDetails.FirstOrDefault(p => p.Months == selectedPlan);

    //    if (plan == null) return NotFound();

    //    var options = new SessionCreateOptions
    //    {
    //        PaymentMethodTypes = new List<string> { "card" },
    //        Mode = "subscription",
    //        CustomerEmail = user.Email,
    //        LineItems = new List<SessionLineItemOptions>
    //        {
    //            new SessionLineItemOptions
    //            {
    //                Price = plan.StripePriceId, // Use the Stripe Price ID
    //                Quantity = 1,
    //            },
    //        },
    //        SuccessUrl = Url.Action("Success", "Subscription", null, Request.Scheme),
    //        CancelUrl = Url.Action("ChoosePlan", "Subscription", null, Request.Scheme),
    //    };

    //    var service = new SessionService();
    //    var session = await service.CreateAsync(options);

    //    return Json(new { id = session.Id });
    //}
}

