using MedicalTourismApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace MedicalTourismApp.Areas.Identity.Pages.Account
{
    public class RequestStatusModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public class RequestStatus
        {
            public string Status { get; set; } // Can be "Pending", "Rejected", or "Approved"
        }
        public RequestStatusModel( ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult OnGet()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == userId); 
            var doctor = _context.Doctors.FirstOrDefault(sp => sp.ServiceProviderId == service.Id);
            var hotel = _context.hotels.FirstOrDefault(sp => sp.ServiceProviderId == service.Id);
            var Guide = _context.Guides.FirstOrDefault(sp => sp.ServiceProviderId == service.Id);
            var apartment = _context.apartments.FirstOrDefault(sp => sp.ServiceProviderId == service.Id);
            var lancer = _context.Freelancers.FirstOrDefault(sp => sp.ServiceProviderId == service.Id);

            if (service == null)
            {
                ModelState.AddModelError(string.Empty, "You must be registered as a service provider.");
                return RedirectToPage("/Account/Register");

            }
            else { 
                 if (service.Status == "Pending")
                {
                    TempData["status"] = "Pending";
                }
                if (service.Status == "Approved")
                {
                    if (service.ServiceType=="Doctor")
                    {
                        TempData["Id"] = doctor.Id;
                        TempData["status"] = "Approved";
                        TempData["type"] = "Doctor";

                    }
                    else if (service.ServiceType == "Hotel")
                    {
                        TempData["Id"] = hotel.Id;
                        TempData["status"] = "Approved";
                        TempData["type"] = "Hotel";

                    }
                    else if (service.ServiceType == "Guide")
                    {
                        TempData["Id"] = Guide.Id;
                        TempData["status"] = "Approved";
                        TempData["type"] = "Guide";

                    }
                    else if (service.ServiceType == "Apartment")
                    {
                        TempData["Id"] = apartment.Id;
                        TempData["status"] = "Approved";
                        TempData["type"] = "Apartment";

                    }
                    else if (service.ServiceType == "Freelance")
                    {
                        if (lancer.Typeofserivce== "physical therapy")
                        {
                            TempData["Id"] = lancer.Id;
                            TempData["status"] = "Approved";
                            TempData["type"] = "Freelance";
                            TempData["servicetype"]=lancer.Typeofserivce;
                        }
                        else if (lancer.Typeofserivce == "Alternative medicine")
                        {
                            TempData["Id"] = lancer.Id;
                            TempData["status"] = "Approved";
                            TempData["type"] = "Freelance";
                            TempData["servicetype"] = lancer.Typeofserivce;
                        }
                        TempData["Id"] = lancer.Id;
                        TempData["status"] = "Approved";
                        TempData["type"] = "Freelance";

                    }


                }
                if (service.Status == "Rejected")
                {
                    TempData["status"] = "Rejected";
                }
                var result = TempData["status"];
                return Page();
            }
        }
    }
}
