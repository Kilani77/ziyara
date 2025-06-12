using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MedicalTourismApp.Areas.Identity.Pages.Account.Forms
{


    public class FreeLanceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public FreeLanceModel(ApplicationDbContext context , IConfiguration configuration,UserManager<User> userManager ,SignInManager<User> signInManager)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public string GoogleMapsApiKey => _configuration["GoogleMapsApiKey"];
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {


            [Required(ErrorMessage = "Location is required.")]

            public string Availabile { get; set; }
            public string language { get; set; }
            public string Typeofservice { get; set; }
            public string supportArea { get; set; }
            public string expirenece { get; set; }
            public double StartingPrice { get; set; }
            public List<IFormFile> Images { get; set; }

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
            if (userId == null)
            {
                return RedirectToPage("/Account/Login");
            }
            var user = await _userManager.FindByIdAsync(userId);

            // Get the corresponding service provider
            var serviceProvider = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == userId);
            if (serviceProvider == null)
            {
                ModelState.AddModelError(string.Empty, "You must be registered as a service provider.");
                return RedirectToPage("/Account/Register");
            }

            // Create new hospital entry
            var FreeLance = new Freelancer
            {
                ServiceProviderId = serviceProvider.Id,
                price = Input.StartingPrice,
                available = Input.Availabile,
                language = Input.language,
                supportedArea = Input.supportArea,
                exprienece = Input.expirenece,
                Typeofserivce = Input.Typeofservice,

            };
            if (ModelState.IsValid)
            {

                _context.Freelancers.Add(FreeLance);
                await _userManager.AddToRoleAsync(user,"Freelance");
                await _context.SaveChangesAsync();


                foreach (var image in Input.Images)
                {
                    if (image.Length > 0)
                    {

                        if (!image.ContentType.StartsWith("image/"))
                        {
                            ModelState.AddModelError("Input.Images", "Only image files are allowed.");
                            return Page();
                        }
                        if (image.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("Input.Images", "Image size must be under 5MB.");
                            return Page();
                        }

                        var fileName = Path.GetRandomFileName() + Path.GetExtension(image.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Home/img", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }

                        // Add image to Images table
                        var imageRecord = new Image
                        {
                            FilePath = "/Home/img/" + fileName,
                            UploadDate = DateTime.Now,
                            ServiceProviderId = serviceProvider.Id
                        };

                        _context.Images.Add(imageRecord);
                        await _context.SaveChangesAsync();

                    }
                }
                await _signInManager.SignOutAsync();

                return RedirectToPage("/Account/Login"); 

            }
            return Page();
        }
    }
}