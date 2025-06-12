using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MedicalTourismApp.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;


        public RegisterModel(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, SignInManager<User> signInManager, ApplicationDbContext context)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public List<string> AvailableServices { get; set; } = new();


        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Full Name is required.")]
            public string FullName { get; set; }
            public string PhoneNumber { get; set; }

            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Invalid email address.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required.")]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "User Type is required.")]
            public string UserType { get; set; } // "Tourist" or "ServiceProvider"
            public string? ServiceType { get; set; }
        }

        public async Task<IActionResult> OnGet(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            AvailableServices = _roleManager.Roles.Select(r => r.Name).ToList();

            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                AvailableServices = _roleManager.Roles.Select(r => r.Name).ToList();
                ModelState.AddModelError(string.Empty, "Email is already registered.");
                return Page();
            }
            else if (ModelState.IsValid)
            {
                var user = new User
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FullName = Input.FullName,
                    PhoneNumber = Input.PhoneNumber,
                    UserType = Input.UserType // Save the selected user type
                };

                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    if (Input.UserType == "Tourist")
                    {

                        await _userManager.AddToRoleAsync(user, "Patient");
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        return RedirectToPage("/Account/Forms/Tourist");
                    }
                    else if (Input.UserType == "ServiceProvider")
                    {
                        var serviceprovider = new Serviceprovider
                        {
                            UserId = user.Id,
                            Status = "Pending",
                            RegistrationTime = DateTime.Now,
                            ServiceType = Input.ServiceType,
                            IsFreeMonthActive = true,
                            MonthEndDate = DateTime.Now.AddMonths(1),



                        };
                        _context.Serviceproviders.Add(serviceprovider);
                        await _context.SaveChangesAsync();
                        await _userManager.AddToRoleAsync(user, "ServiceProvider");
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        if (Input.ServiceType == "Doctor")
                        {
                            return RedirectToPage("/Account/Forms/Doctor");
                        }
                        else if (Input.ServiceType == "Stay")
                        {
                            return RedirectToPage("/Account/Stay");
                        }
                        else if (Input.ServiceType == "Hospital")
                        {
                            return RedirectToPage("/Account/Forms/Hospital");
                        }
                        else if (Input.ServiceType == "Transportation")
                        {
                            return RedirectToPage("/Account/Forms/Transportation");
                        }
                        else if (Input.ServiceType == "Clinic")
                        {
                            return RedirectToPage("/Account/Forms/Clinic");
                        }
                        else if (Input.ServiceType == "Guide")
                        {
                            return RedirectToPage("/Account/Forms/Guide");
                        }
                        else if (Input.ServiceType == "Freelance")
                        {
                            return RedirectToPage("/Account/Forms/FreeLance");
                        }
                        else if (Input.ServiceType == "Hotel")
                        {
                            return RedirectToPage("/Account/Forms/Hotel");
                        }
                        else if (Input.ServiceType == "Apartment")
                        {
                            return RedirectToPage("/Account/Forms/Apartment");
                        }
                    }


                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            AvailableServices = _roleManager.Roles.Select(r => r.Name).ToList();
            return Page();
        }
    }
}