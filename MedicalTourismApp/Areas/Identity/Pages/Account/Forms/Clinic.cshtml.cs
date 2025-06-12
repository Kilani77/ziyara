using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
namespace MedicalTourismApp.Areas.Identity.Pages.Account.Forms
{
      public class ClinicModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;


        public ClinicModel(ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Hospital name is required.")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Location is required.")]
            public string Location { get; set; }

            public string ContactNumber { get; set; }
            public string Email { get; set; }
            public string Website { get; set; }

            public string Image { get; set; } // File upload handling can be added later

            public int YearEstablished { get; set; }

            public string Specialties { get; set; }
            public int MedicalStaffCount { get; set; }
            public string Equipment { get; set; }
            public double StartingPrice { get; set; }
            public bool EmergencyAvailable { get; set; }
            public bool Open24Hours { get; set; }

            public string LanguagesSpoken { get; set; }

            public string LicenseDocuments { get; set; } // Path for uploaded files


        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Get logged-in user's ID
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Get the corresponding service provider
            var serviceProvider = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == userId);
            if (serviceProvider == null)
            {
                ModelState.AddModelError(string.Empty, "You must be registered as a service provider.");
                return RedirectToPage("/Account/Register");
            }

            // Create new hospital entry
            var clinic = new Clinic
            {
                ServiceProviderId = serviceProvider.Id,
                Name = Input.Name,
                Location = Input.Location,
                ContactNumber = Input.ContactNumber,
                Email = Input.Email,
                
                Image = Input.Image,
                YearEstablished = Input.YearEstablished,
                Specialties = Input.Specialties,
                MedicalStaffCount = Input.MedicalStaffCount,
                Equipment = Input.Equipment,
                StartingPrice = Input.StartingPrice,
                EmergencyAvailable = Input.EmergencyAvailable,
                Open24Hours = Input.Open24Hours,
                LanguagesSpoken = Input.LanguagesSpoken,
                LicenseDocuments = Input.LicenseDocuments,

            };

            _context.Clinics.Add(clinic);
            await _userManager.AddToRoleAsync(user, "Clinic");
            await _context.SaveChangesAsync();
            await _signInManager.SignOutAsync();

            return RedirectToPage("/Account/Login");
        }
    }

}
