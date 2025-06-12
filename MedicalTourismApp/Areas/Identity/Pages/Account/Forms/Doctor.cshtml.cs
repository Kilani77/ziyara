using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MedicalTourismApp.Areas.Identity.Pages.ServiceProvider
{
    public class RegisterDoctorModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<User> _signInManager;

        public RegisterDoctorModel(UserManager<User> userManager, ApplicationDbContext context, IConfiguration configuration, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public string GoogleMapsApiKey => _configuration["GoogleMapsApiKey"];
        public class InputModel
        {
            [Required(ErrorMessage = "Experience is required.")]
            public string Experience { get; set; }

            [Required(ErrorMessage = "Specialization is required.")]
            public string Specialization { get; set; }

            [Required(ErrorMessage = "Location is required.")]
            public string Location { get; set; }

            [Required(ErrorMessage = "Availability is required.")]
            public string Available { get; set; } // Example: "Monday - Friday, 9 AM - 5 PM"

            [Required(ErrorMessage = "Price is required.")]
            [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number.")]
            public double Price { get; set; }

            [Required(ErrorMessage = "Bio is required.")]
            public string Bio { get; set; }

            public List<IFormFile> Images { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/ForgetPassword");
            }

            // Find the corresponding Service Provider record
            var service = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == user.Id);
            if (service == null)
            {
                ModelState.AddModelError(string.Empty, "You must be registered as a service provider.");
                return Page();
            }
            if (ModelState.IsValid)
            {
                var doctor = new Doctor
                {
                    ServiceProviderId = service.Id,
                    exprienece = Input.Experience,
                    Specialization = Input.Specialization,
                    location = Input.Location,
                    available = Input.Available,
                    price = Input.Price,
                    Bio = Input.Bio,
                    ServiceProvider = service
                };

                await _context.Doctors.AddAsync(doctor);
                await _userManager.AddToRoleAsync(user, "Doctor");

                await _context.SaveChangesAsync();
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
                    await _context.SaveChangesAsync();

                }
                else
                {
                    return Page();
                }

            }
            await _signInManager.SignOutAsync();


            return RedirectToPage("/Account/Login");


        }
    }
}
