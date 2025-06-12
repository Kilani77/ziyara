using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MedicalTourismApp.Areas.Identity.Pages.Account.Forms
{
    public class RegisterApartmentModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public RegisterApartmentModel(ApplicationDbContext context, IConfiguration configuration ,UserManager<User> userManager,SignInManager<User> signInManager)
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
            public string Name { get; set; }
            public string ContactNumber { get; set; }
            public int NumberOfRooms { get; set; }
            public bool HasKitchen { get; set; }
            public bool HasBalcony { get; set; }
            [RegularExpression(@"^-?\d+\.\d+,-?\d+\.\d+$", ErrorMessage = "Location must be in the format 'latitude,longitude' (e.g., 31.9539,35.9106).")]
            public string Location { get; set; }
            public bool Available { get; set; }
            public int CapacityOfPlace { get; set; }
            public string Description { get; set; } // Description of the apartment
            public double Price { get; set; } // Price of the apartment stay
            public List<IFormFile> Images { get; set; }
        }
        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
                // Get logged-in user's ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.FindByIdAsync(userId);
                if (userId == null)
                {
                    return RedirectToPage("/Account/Login");
                }
                // Get the corresponding service provider
                var service = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == userId);
                if (service == null)
                {
                    ModelState.AddModelError(string.Empty, "You must be registered as a service provider.");
                    return RedirectToPage("/Account/Register");
                }
            if (ModelState.IsValid)
            {
                var Apar = new Apartment
                {
                    ServiceProviderId = service.Id,
                    Name = Input.Name,
                    Location = Input.Location,
                    ContactNumber = Input.ContactNumber,
                    CapacityOfPlace = Input.CapacityOfPlace,
                    NumberOfRooms = Input.NumberOfRooms,
                    Price = Input.Price,
                    Available = Input.Available.ToString(),
                    Description = Input.Description,
                    HasBalcony = Input.HasBalcony,
                    HasKitchen = Input.HasKitchen,
                };

                _context.apartments.Add(Apar);
                await _userManager.AddToRoleAsync(user, "Apartment"); // Note: Use uppercase to match your DB
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
            return Page();
        }
    }
}
