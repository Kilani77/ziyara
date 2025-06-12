using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MedicalTourismApp.Areas.Identity.Pages.Account.Forms
{
    public class TransportationModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public TransportationModel(ApplicationDbContext context, IConfiguration configuration ,UserManager<User> userManager,SignInManager<User> signInManager)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public string GoogleMapsApiKey => _configuration["GoogleMapsApiKey"];
        public class InputModel
        {
            public string FullName { get; set; }
            public int Numberofseats { get; set; }
            public string modelocar { get; set; }
            public string LicensePlate { get; set; }
            public IFormFile License { get; set; }
            public string ContactNumber { get; set; }
            public string Location { get; set; } // City or Area
            public double PricePerDay { get; set; }
            public bool DriverAvalablity { get; set; }
            public string Bio { get; set; } // Short description about the driver
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
            var service = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == userId);
            if (service == null)
            {
                ModelState.AddModelError(string.Empty, "You must be registered as a service provider.");
                return RedirectToPage("/Account/Register");
            }


            var Transportation = new Transportation
            {
                ServiceProviderId = service.Id,
                FullName = Input.FullName,
                Location = Input.Location,
                ContactNumber = Input.ContactNumber,
                License = Input.License.ToString(),
                DriverAvalablity = Input.DriverAvalablity,
                modelocar = Input.modelocar,
                Bio = Input.Bio,
                LicensePlate = Input.LicensePlate,
                PricePerDay = Input.PricePerDay,
                Numberofseats = Input.Numberofseats,

            };

            _context.Transportations.Add(Transportation);
            await _userManager.AddToRoleAsync(user, "Transportation");
            await _context.SaveChangesAsync();
            if (Input.Images != null && Input.Images.Count > 0)
            {
                // Validate images
                if (Input.Images.Count > 10)
                {
                    ModelState.AddModelError("Input.Images", "Cannot upload more than 10 images.");
                    return Page();
                }

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
                            ServiceProviderId = service.Id
                        };

                        _context.Images.Add(imageRecord);
                    }
                }
                await _signInManager.SignOutAsync();

                await _context.SaveChangesAsync();
            }
            return RedirectToPage("/Account/Login");
        }
    }
}
