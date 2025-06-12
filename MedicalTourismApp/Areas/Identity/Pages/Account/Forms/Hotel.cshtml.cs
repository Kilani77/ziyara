using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;

namespace MedicalTourismApp.Areas.Identity.Pages.Account.Forms
{


    public class RegisterHotelModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;


        public RegisterHotelModel(ApplicationDbContext context, IConfiguration configuration,UserManager<User> userManager, SignInManager<User> signInManager)
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
            [Required(ErrorMessage = "Hotel name is required.")]
            public string Name { get; set; }

            [Required(ErrorMessage = "Location is required.")]
            public string Location { get; set; }

            public int numberOfroom { get; set; }
            public string ContactNumber { get; set; }
            public bool Haspool { get; set; }
            public bool HasGym { get; set; }
            public string capacityOfPlace { get; set; }
            public string availablete { get; set; }

            public bool roomService { get; set; }
            public bool wifiAvailablity { get; set; }
            public bool parkingAvailablity { get; set; }
            public double StartingPrice { get; set; }
            public bool breakFastIncluded { get; set; }
            public string Bio { get; set; }
            public IFormFile LicenseDocuments { get; set; }
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
            var service = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == userId);
            if (service == null)
            {
                ModelState.AddModelError(string.Empty, "You must be registered as a service provider.");
                return RedirectToPage("/Account/Register");
            }

            // Create new hospital entry
            var hotel = new Hotel
            {
                ServiceProviderId = service.Id,
                Name = Input.Name,
                Location = Input.Location,
                ContactNumber = Input.ContactNumber,
                WifiAvailability = Input.wifiAvailablity,
                Available = Input.availablete,
                HasGym = Input.HasGym,
                HasPool = Input.Haspool,
                CapacityOfPlace = Input.capacityOfPlace,
                NumberOfRooms = Input.numberOfroom,
                ParkingAvailability = Input.parkingAvailablity,
                Price = Input.StartingPrice,
                BreakfastIncluded = Input.breakFastIncluded,
                Description = Input.Bio,
                RoomService = Input.roomService,


            };

            _context.hotels.Add(hotel);
            await _userManager.AddToRoleAsync(user, "Hotel");

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
