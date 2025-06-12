using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace MedicalTourismApp.Areas.Identity.Pages.Account.Forms
{
    public class TouristModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<User> _signInManager;

        public string ReturnUrl { get; set; }

        public TouristModel(UserManager<User> userManager, ApplicationDbContext context, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Passport Number is required.")]
            
            public string PassportNumber { get; set; }

            [Required(ErrorMessage = "Nationality is required.")]
            public string Nationality { get; set; }

            [Required(ErrorMessage = "Preferred Language is required.")]
            public string PreferredLanguage { get; set; }

        }
        public IActionResult OnGet(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl= returnUrl;
            return Page();
        }
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl =HttpContext.Session.GetString("returnUrl");
            returnUrl ??= Url.Content("~/");

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Register");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToPage("/Account/ForgetPassword");
            }

           
            if (ModelState.IsValid)
            {
                var tourist = new Tourist
                {
                   Nationality=Input.Nationality,
                   PreferredLanguage=Input.PreferredLanguage,
                   PassportNumber=Input.PassportNumber,
                   UserId=user.Id,
                   
                  
                };

                await _context.Tourists.AddAsync(tourist);
                await _signInManager.SignInAsync(user, isPersistent: false);

                await _context.SaveChangesAsync();
                return LocalRedirect(returnUrl);
            }

            return Page();
        }
    }
}
