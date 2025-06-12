
#nullable disable

using System.ComponentModel.DataAnnotations;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MedicalTourismApp.Data;

namespace MedicalTourismApp.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public LoginModel(SignInManager<User> signInManager,UserManager<User> userManager, ILogger<LoginModel> logger, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string returnUrl = null) 
        {
            if (User.Identity.IsAuthenticated) {
                 return  RedirectToAction("Index","Home");
            }
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");
            HttpContext.Session.SetString("returnUrl", returnUrl);
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            
            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
                if (user!=null)
                {
                    var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in.");
                        if (user.UserType=="Admin")
                        {
                            return RedirectToAction("Dashboard", "Admin");
                        }
                        else if (user.UserType == "ServiceProvider")
                        {
                            var id = user.Id;
                            var service = _context.Serviceproviders.FirstOrDefault(p => p.UserId == id);
                            var doctor = _context.Doctors.FirstOrDefault(p => p.ServiceProviderId == service.Id );
                            var free=_context.Freelancers.FirstOrDefault(p=>p.ServiceProviderId == service.Id);
                            if (service.ServiceType=="Doctor" && service.Status == "Approved")
                            {
                                var check = _context.Avaliabledoctors.Any(p => p.DoctorId == doctor.Id);
                                if (check == false )
                                {
                                    return RedirectToAction("Available", "provider" ,new {id=doctor.Id});
                                }
                                
                               
                                else 
                                {
                                    return RedirectToAction("Dashboard", "Doctor");
                                }
                                
                            }

                           else if (service.ServiceType== "Freelance" && service.Status == "Approved")
                            {
                                if (free.Typeofserivce== "nursing")
                                {
                                    return RedirectToAction("Dashboarder", "Nurse");

                                }
                                else if (free.Typeofserivce == "physical therapy")
                                {
                                    var phs = _context.Freelancers.FirstOrDefault(p => p.ServiceProviderId == service.Id);

                                    var check = _context.AvaliableFree.Any(p => p.FreeId == phs.Id);
                                    if (check == false)
                                    {
                                        return RedirectToAction("AvailableFree", "provider");
                                    }
                                    else
                                    {
                                        return RedirectToAction("Dashboard", "Phsy", new { id = phs.Id });
                                    }
                                }
                                else if (free.Typeofserivce == "Alternative medicine")
                                {
                                    var phs = _context.Freelancers.FirstOrDefault(p => p.ServiceProviderId == service.Id);

                                    var check = _context.AvaliableFree.Any(p => p.FreeId == phs.Id);
                                    if (check == false)
                                    {
                                        return RedirectToAction("AvailableFree", "provider", new { id = phs.Id });
                                    }
                                    else
                                    {
                                        return RedirectToAction("Dashboard", "Alt");
                                    }
                                }
                            }
                            else if (service.ServiceType == "Guide" && service.Status =="Approved")
                            {
                                return RedirectToAction("Dashboarder", "Guide");
                            }
                            else if (service.ServiceType == "Apartment" && service.Status == "Approved")
                            {
                                return RedirectToAction("Dashboarder", "Apartment"); 
                            }
                            else if (service.ServiceType == "Transportation" && service.Status == "Approved")
                            {
                                return RedirectToAction("Dashboarder", "Transportation");
                            }
                            else
                            {
                                return RedirectToPage("/Account/RequestStatus");
                            }
                        }
                        else if (user.UserType == "Tourist")
                        {
                            return LocalRedirect(returnUrl);
                        }
                       
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                        return Page();
                    }
                    
                }
                
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
