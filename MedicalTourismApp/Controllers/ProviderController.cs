using MedicalTourismApp.Data;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace MedicalTourismApp.Controllers
{

    public class ProviderController : Controller
    {
       
        private readonly ApplicationDbContext _context;
      
        public ProviderController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public class GuideAvailabilityViewModel
        {
            public int GuideId { get; set; }

            [Required]
            public List<DateTime> AvailableDays { get; set; } = new List<DateTime>();

            public List<DateTime> AvailableDates { get; set; } = new List<DateTime>();
        }
        public IActionResult Available(int id)
        {
            // Get existing availability records for this doctor
            var availabilities = _context.Avaliabledoctors
                .Where(a => a.DoctorId == id)
                .ToList();

            List<AvailabilityViewModel> viewModel = new List<AvailabilityViewModel>();

            foreach (var availability in availabilities)
            {
                viewModel.Add(new AvailabilityViewModel
                {
                    Id = availability.Id,
                    Day = availability.DayOfWeek,
                    Start = availability.StartTime,
                    End = availability.EndTime
                });
            }

            ViewBag.DoctorId = id;
            return View(viewModel);
        } 
        [HttpPost]
        public IActionResult Available(Dictionary<string, string> formData)
        {
            int doctorId = 0;
            if (formData.ContainsKey("doctorId") && int.TryParse(formData["doctorId"], out doctorId))
            {
            }
            else
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var service = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == userId);
                if (service != null)
                {
                    var doctor = _context.Doctors.FirstOrDefault(sp => sp.ServiceProviderId == service.Id);
                    if (doctor != null)
                    {
                        doctorId = doctor.Id;
                    }
                }
            }

            if (doctorId == 0)
            {
                TempData["Error"] = "Could not determine doctor ID. Please try again.";
                return RedirectToAction("Available", new { id = doctorId });
            }

                bool savedAny = false;

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                string dayName = day.ToString();

                // Check if this day was marked as available
                if (formData.ContainsKey($"available{dayName}") && formData[$"available{dayName}"] == "on")
                {
                    // Process primary time slot
                    if (formData.ContainsKey($"startTime_{dayName}") && formData.ContainsKey($"endTime_{dayName}") &&
                        !string.IsNullOrEmpty(formData[$"startTime_{dayName}"]) && !string.IsNullOrEmpty(formData[$"endTime_{dayName}"]))
                    {
                        // Parse times
                        TimeSpan startTime = TimeSpan.Parse(formData[$"startTime_{dayName}"]);
                        TimeSpan endTime = TimeSpan.Parse(formData[$"endTime_{dayName}"]);

                        // Skip if end time is before or equal to start time
                        if (endTime <= startTime)
                        {
                            continue;
                        }

                        // Check if this time slot already exists
                        var existingAvailability = _context.Avaliabledoctors
                            .FirstOrDefault(a => a.DoctorId == doctorId &&
                                              a.DayOfWeek == day &&
                                              a.StartTime == startTime &&
                                              a.EndTime == endTime);

                        // If not exists, add it
                        if (existingAvailability == null)
                        {
                            var doctorData = new Avaliabledoctor
                            {
                                DoctorId = doctorId,
                                DayOfWeek = day,
                                StartTime = startTime,
                                EndTime = endTime
                            };
                            _context.Avaliabledoctors.Add(doctorData);
                            savedAny = true;
                        }
                    }

                    // Process additional time slots (up to 3)
                    for (int i = 0; i < 3; i++)
                    {
                        if (formData.ContainsKey($"startTime_{dayName}_{i}") && formData.ContainsKey($"endTime_{dayName}_{i}") &&
                            !string.IsNullOrEmpty(formData[$"startTime_{dayName}_{i}"]) && !string.IsNullOrEmpty(formData[$"endTime_{dayName}_{i}"]))
                        {
                            // Parse times
                            TimeSpan startTime = TimeSpan.Parse(formData[$"startTime_{dayName}_{i}"]);
                            TimeSpan endTime = TimeSpan.Parse(formData[$"endTime_{dayName}_{i}"]);

                            // Skip if end time is before or equal to start time
                            if (endTime <= startTime)
                            {
                                continue;
                            }

                            // Check if this time slot already exists
                            var existingAvailability = _context.Avaliabledoctors
                                .FirstOrDefault(a => a.DoctorId == doctorId &&
                                                  a.DayOfWeek == day &&
                                                  a.StartTime == startTime &&
                                                  a.EndTime == endTime);

                            // If not exists, add it
                            if (existingAvailability == null)
                            {
                                var doctorData = new Avaliabledoctor
                                {
                                    DoctorId = doctorId,
                                    DayOfWeek = day,
                                    StartTime = startTime,
                                    EndTime = endTime
                                };
                                _context.Avaliabledoctors.Add(doctorData);
                                savedAny = true;
                            }
                        }
                    }
                }
            }

            // Save changes and set message
            if (savedAny)
            {
                _context.SaveChanges();
                TempData["Success"] = "Availability saved successfully!";
            }
            else
            {
                TempData["Error"] = "No new availability was added. Please check your inputs.";
            }

            return RedirectToAction("Available", new { id = doctorId });
        }

      
        public IActionResult AvailableFree(int id)
        {
            var availabilities = _context.AvaliableFree
                .Where(a => a.FreeId == id)
                .ToList();

            List<AvailabilityViewModelfree> viewModel = new List<AvailabilityViewModelfree>();

            foreach (var availability in availabilities)
            {
                viewModel.Add(new AvailabilityViewModelfree
                {
                    Id = availability.Id,
                    Day = availability.DayOfWeek,
                    Start = availability.StartTime,
                    End = availability.EndTime
                });
            }

            ViewBag.DoctorId = id;
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult AvailableFree(Dictionary<string, string> formData)
        {


            int doctorId = 0;
            
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var service = _context.Serviceproviders.FirstOrDefault(sp => sp.UserId == userId);
                if (service != null)
                {
                    var doctor = _context.Freelancers.FirstOrDefault(sp => sp.ServiceProviderId == service.Id);
                    if (doctor != null)
                    {
                        doctorId = doctor.Id;
                    }
                }
            

            // If we still don't have a doctor ID, return error
            if (doctorId == 0)
            {
                TempData["Error"] = "Could not determine doctor ID. Please try again.";
                return RedirectToAction("AvailableFree", new { id = doctorId });
            }



            bool savedAny = false;

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                string dayName = day.ToString();

                // Check if this day was marked as available
                if (formData.ContainsKey($"available{dayName}") && formData[$"available{dayName}"] == "on")
                {
                    // Process primary time slot
                    if (formData.ContainsKey($"startTime_{dayName}") && formData.ContainsKey($"endTime_{dayName}") &&
                        !string.IsNullOrEmpty(formData[$"startTime_{dayName}"]) && !string.IsNullOrEmpty(formData[$"endTime_{dayName}"]))
                    {
                        // Parse times
                        TimeSpan startTime = TimeSpan.Parse(formData[$"startTime_{dayName}"]);
                        TimeSpan endTime = TimeSpan.Parse(formData[$"endTime_{dayName}"]);

                        // Skip if end time is before or equal to start time
                        if (endTime <= startTime)
                        {
                            continue;
                        }

                        // Check if this time slot already exists
                        var existingAvailability = _context.AvaliableFree
                            .FirstOrDefault(a => a.FreeId == doctorId &&
                                              a.DayOfWeek == day &&
                                              a.StartTime == startTime &&
                                              a.EndTime == endTime);

                        // If not exists, add it
                        if (existingAvailability == null)
                        {
                            var doctorData = new avaliableFree
                            {
                                FreeId = doctorId,
                                DayOfWeek = day,
                                StartTime = startTime,
                                EndTime = endTime
                            };
                            _context.AvaliableFree.Add(doctorData);
                            savedAny = true;
                        }
                    }

                    // Process additional time slots (up to 3)
                    for (int i = 0; i < 3; i++)
                    {
                        if (formData.ContainsKey($"startTime_{dayName}_{i}") && formData.ContainsKey($"endTime_{dayName}_{i}") &&
                            !string.IsNullOrEmpty(formData[$"startTime_{dayName}_{i}"]) && !string.IsNullOrEmpty(formData[$"endTime_{dayName}_{i}"]))
                        {
                            // Parse times
                            TimeSpan startTime = TimeSpan.Parse(formData[$"startTime_{dayName}_{i}"]);
                            TimeSpan endTime = TimeSpan.Parse(formData[$"endTime_{dayName}_{i}"]);

                            // Skip if end time is before or equal to start time
                            if (endTime <= startTime)
                            {
                                continue;
                            }

                            // Check if this time slot already exists
                            var existingAvailability = _context.AvaliableFree
                                .FirstOrDefault(a => a.FreeId == doctorId &&
                                                  a.DayOfWeek == day &&
                                                  a.StartTime == startTime &&
                                                  a.EndTime == endTime);

                            // If not exists, add it
                            if (existingAvailability == null)
                            {
                                var doctorData = new avaliableFree
                                {
                                    FreeId = doctorId,
                                    DayOfWeek = day,
                                    StartTime = startTime,
                                    EndTime = endTime
                                };
                                _context.AvaliableFree.Add(doctorData);
                                savedAny = true;
                            }
                        }
                    }
                }
            }

            // Save changes and set message
            if (savedAny)
            {
                _context.SaveChanges();
                TempData["Success"] = "Availability saved successfully!";
            }
            else
            {
                TempData["Error"] = "No new availability was added. Please select The Day before entering the Time.";
            }

            return RedirectToAction("AvailableFree", new { id = doctorId });
        }
    }
    // ViewModel to display availability data
    public class AvailabilityViewModel
    {
        public int Id { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
    public class AvailabilityViewModelfree
    {
        public int Id { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
}
