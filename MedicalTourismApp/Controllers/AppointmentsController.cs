using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalTourismApp.Models;
using MedicalTourismApp.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Numerics;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace MedicalTourismApp.Controllers
{
    [Route("api/Appointments")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly UserManager<User> _usermanager;
        public AppointmentsController(ApplicationDbContext context,
        ISubscriptionService subscriptionService,
        UserManager<User> usermanage)
        {
            _subscriptionService = subscriptionService;
            _usermanager = usermanage;
            _context = context;
        }

        [HttpGet("approved/{date}")]
        public IActionResult GetApprovedAppointments(string date)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var service = _context.Serviceproviders.FirstOrDefault(p => p.UserId == userId);
                var doctor = _context.Doctors.FirstOrDefault(p => p.ServiceProviderId == service.Id);

                if (doctor == null)
                    return BadRequest("Doctor not found");

                DateOnly parsedDate = DateOnly.Parse(date); // this line might be throwing the exception

                var appointments = _context.app_Doctors
                    .Include(p => p.Tourist)
                    .ThenInclude(t => t.User)
                    .Where(a => a.DoctorId == doctor.Id && a.Approve == "Approved" && a.date == parsedDate)
                    .Select(a => new
                    {
                        a.Id,
                        a.date,
                        startTime = a.StartTime.ToString(@"hh\:mm"),
                        response = a.Response,
                        tourist = new
                        {
                            user = new
                            {
                                fullName = a.Tourist.User.FullName
                            }
                        }
                    }).ToList();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        // GET: api/Appointments/approved-month/2023-01
        [HttpGet("approved-month/{yearMonth}")]
        public async Task<ActionResult<IEnumerable<app_doctor>>> GetApprovedAppointmentsByMonth(string yearMonth)
        {


            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
                var doctor = await _context.Doctors.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);

                if (doctor == null)
                    return BadRequest("Doctor not found");

                var parts = yearMonth.Split('-');
                var year = int.Parse(parts[0]);
                var month = int.Parse(parts[1]);

                var appointments = await _context.app_Doctors
                    .Where(a => a.date.Year == year &&
                                a.date.Month == month &&
                                a.Approve == "approved" &&
                                a.DoctorId == doctor.Id) // 👈 Filter by doctor
                    .Select(a => new {
                        a.date
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("reject/{id}")]
        public async Task<IActionResult> RejectAppointment(int id, [FromBody] string reason)
        {
            var appointment = await _context.app_Doctors
                .Include(a => a.Tourist)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound("Appointment not found");

            appointment.Approve = "Rejected";
            appointment.Response = reason; // save the reason in DB
            await _context.SaveChangesAsync();

            // Send rejection email
            var email = appointment.Tourist.User.Email;
            var fullName = appointment.Tourist.User.FullName;
            var date = appointment.date.ToString("yyyy-MM-dd");
            var time = appointment.StartTime.ToString(@"hh\:mm");

            string subject = "Appointment Rejected";
            string message = $"Dear {fullName},\n\nYour appointment scheduled for {date} at {time} has been rejected.\nReason: {reason}\n\nRegards,\nMedical Trip Team";

            await SendEmailAsync(email, subject, message);

            return Ok("Appointment rejected and email sent.");
        }
        [HttpPut("reject-all/{date}")]
        public async Task<IActionResult> RejectAllAppointmentsForDate(string date, [FromBody] string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var doctor = await _context.Doctors.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);

            if (doctor == null)
                return BadRequest("Doctor not found");

            DateOnly parsedDate = DateOnly.Parse(date);
            var appointments = await _context.app_Doctors
                .Include(a => a.Tourist)
                .ThenInclude(t => t.User)
                .Where(a => a.DoctorId == doctor.Id && a.Approve == "Approved" && a.date == parsedDate)
                .ToListAsync();

            foreach (var appointment in appointments)
            {
                appointment.Approve = "Rejected";
                appointment.Response = reason;

                // Email each user
                var email = appointment.Tourist.User.Email;
                var fullName = appointment.Tourist.User.FullName;
                var time = appointment.StartTime.ToString(@"hh\:mm");
                string subject = "Appointment Rejected";
                string message = $"Dear {fullName},\n\nYour appointment scheduled for {parsedDate} at {time} has been rejected.\nReason: {reason}\n\nRegards,\nMedical Trip Team";
                await SendEmailAsync(email, subject, message);
            }

            await _context.SaveChangesAsync();

            return Ok("All appointments rejected and emails sent.");
        }
        [HttpPut("rejectn/{id}")]
        public async Task<IActionResult> RejectAppointmentn(int id, [FromBody] string reason)
        {
            var appointment = await _context.bookFreelances
                .Include(a => a.Tourist)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound("Appointment not found");

            appointment.Approve = false;
            appointment.Response = reason; // save the reason in DB
            await _context.SaveChangesAsync();

            // Send rejection email
            var email = appointment.Tourist.User.Email;
            var fullName = appointment.Tourist.User.FullName;
            var date = appointment.BookingDate;

            string subject = "Appointment Rejected";
            string message = $"Dear {fullName},\n\nYour appointment scheduled for {date} has been rejected.\nReason: {reason}\n\nRegards,\nMedical Trip Team";

            await SendEmailAsync(email, subject, message);

            return Ok("Appointment rejected and email sent.");
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("tripmedical88@gmail.com", "cqmyspjxolsrkfgt"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("tripmedical88@gmail.com"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
        [HttpGet("booked-days/{nurseId}/{year}-{month}")]
        public IActionResult GetBookedDays(int nurseId, int year, int month)
        {
            var appointments = _context.bookFreelances
                .Where(a => a.FreelanceId == nurseId &&
                            a.StartDate.HasValue &&
                            a.EndDate.HasValue &&
                            a.Approve == true)
                .ToList();

            var bookedDays = new HashSet<string>();

            foreach (var appt in appointments)
            {
                var start = appt.StartDate.Value;
                var end = appt.EndDate.Value;

                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    if (date.Year == year && date.Month == month)
                    {
                        bookedDays.Add(date.ToString("yyyy-MM-dd"));
                    }
                }
            }

            return Ok(bookedDays);
        }

        [HttpGet("for-month/{nurseId}/{year}-{month}")]
        public IActionResult GetAppointmentsForMonth(int nurseId, int year, int month)
        {
            try
            {
                // Validate date parameters
                if (year < 2000 || year > 2100 || month < 1 || month > 12)
                {
                    return BadRequest("Invalid date parameters");
                }

                // Create start and end date for the requested month
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Query appointments that overlap with this month
                var appointments = _context.bookFreelances
                    .Where(a => a.FreelanceId == nurseId &&
                             a.StartDate.HasValue &&
                             a.EndDate.HasValue &&
                             a.StartDate.Value <= endDate &&
                             a.EndDate.Value >= startDate &&
                             a.Approve == true)
                    .Select(a => new
                    {
                        id = a.Id,
                        touristId = a.TouristId,
                        touristName = a.Tourist.User.FullName, // Assuming Tourist has a Name property
                        bookingDate = a.BookingDate != null ? a.BookingDate.Value.ToString("yyyy-MM-dd") : null,
                        startDate = a.StartDate.Value.ToString("yyyy-MM-dd"),
                        endDate = a.EndDate.Value.ToString("yyyy-MM-dd"),
                        startTime = a.StartTime.HasValue ? a.StartTime.Value.ToString() : null,
                        endTime = a.EndTime.HasValue ? a.EndTime.Value.ToString() : null,
                        approve = a.Approve,
                        response = a.Response
                    })
                    .ToList();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // GET: api/GuideBookings/for-guide/{guideId}/{yearMonth}
        [HttpGet("for-guide/{guideId}/{yearMonth}")]
        public async Task<ActionResult<IEnumerable<object>>> GetBookingsForGuide(int guideId, string yearMonth)
        {
            try
            {
                if (string.IsNullOrEmpty(yearMonth) || !yearMonth.Contains("-"))
                {
                    return BadRequest("Invalid year-month format. Should be YYYY-MM");
                }

                string[] parts = yearMonth.Split('-');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int year) || !int.TryParse(parts[1], out int month))
                {
                    return BadRequest("Invalid year-month format. Should be YYYY-MM");
                }

                DateOnly firstDay = new DateOnly(year, month, 1);
                DateOnly lastDay = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
                var bookings = await _context.GuideBookings
                    .Include(b => b.Tourist)
                    .ThenInclude(t => t.User)
                    .Where(b => b.GuideId == guideId &&
                           b.BookingDate >= firstDay &&
                           b.BookingDate <= lastDay &&
                           b.Approve == "Approved")
                    .Select(b => new
                    {
                        Id = b.Id,
                        GuideId = b.GuideId,
                        TouristId = b.TouristId,
                        TouristName = b.Tourist.User.FullName,
                        BookingDate = b.BookingDate,
                        AreaBook = b.AreaBook,
                        Approve = b.Approve,
                        Nati = b.Tourist.Nationality
                    })
                    .ToListAsync();

                return Ok(bookings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/GuideBookings/update-status/{id}
        [HttpPost("update-status/{id}")]
        public async Task<ActionResult> UpdateBookingStatus(int id, [FromBody] StatusUpdateModel model)
        {
            try
            {
                var booking = await _context.GuideBookings.FindAsync(id);

                if (booking == null)
                {
                    return NotFound("Booking not found");
                }

                booking.Approve = model.Status;
                _context.Update(booking);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPut("rejectg/{id}")]
        public async Task<IActionResult> RejectAppointmentg(int id, [FromBody] string reason)
        {
            var appointment = await _context.GuideBookings
                .Include(a => a.Tourist)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound("Appointment not found");

            appointment.Approve = "Rejected";
            appointment.Response = reason; // save the reason in DB
            await _context.SaveChangesAsync();

            // Send rejection email
            var email = appointment.Tourist.User.Email;
            var fullName = appointment.Tourist.User.FullName;
            var date = appointment.BookingDate.ToString("yyyy-MM-dd");

            string subject = "Appointment Rejected";
            string message = $"Dear {fullName},\n\nYour appointment scheduled for {date} has been rejected.\nReason: {reason}\n\nRegards,\nMedical Trip Team";

            await SendEmailAsync(email, subject, message);

            return Ok("Appointment rejected and email sent.");
        }
        [HttpGet("booked-daysa/{nurseId}/{year}-{month}")]
        public IActionResult GetBookedDaysa(int nurseId, int year, int month)
        {
            var appointments = _context.calapartments
                .Where(a => a.ApartmentId == nurseId &&
                            a.Approve == "Approved")
                .ToList();

            var bookedDays = new HashSet<string>();

            foreach (var appt in appointments)
            {
                var start = appt.Startdate;
                var end = appt.Enddate;

                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    if (date.Year == year && date.Month == month)
                    {
                        bookedDays.Add(date.ToString("yyyy-MM-dd"));
                    }
                }
            }

            return Ok(bookedDays);
        }

        [HttpGet("for-montha/{nurseId}/{year}-{month}")]
        public IActionResult GetAppointmentsForMontha(int nurseId, int year, int month)
        {
            try
            {
                // Validate date parameters
                if (year < 2000 || year > 2100 || month < 1 || month > 12)
                {
                    return BadRequest("Invalid date parameters");
                }

                // Create start and end date for the requested month
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Query appointments that overlap with this month
                var appointments = _context.calapartments
                    .Where(a => a.ApartmentId == nurseId &&
                             a.Startdate <= endDate &&
                             a.Enddate >= startDate &&
                             a.Approve == "Approved")
                    .Select(a => new
                    {
                        id = a.Id,
                        touristId = a.TouristId,
                        touristName = a.Tourist.User.FullName, // Assuming Tourist has a Name property
                        startDate = a.Startdate.ToString("yyyy-MM-dd"),
                        endDate = a.Enddate.Value.ToString("yyyy-MM-dd"),
                        approve = a.Approve,
                        response = a.Response,
                        ascort = a.numberEscorts
                    })
                    .ToList();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPut("rejecta/{id}")]
        public async Task<IActionResult> RejectAppointmenta(int id, [FromBody] string reason)
        {
            var appointment = await _context.calapartments
                .Include(a => a.Tourist)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound("Appointment not found");

            appointment.Approve = "Rejected";
            appointment.Response = reason; // save the reason in DB
            await _context.SaveChangesAsync();

            // Send rejection email
            var email = appointment.Tourist.User.Email;
            var fullName = appointment.Tourist.User.FullName;
            var date = appointment.Startdate.ToString("yyyy-MM-dd");

            string subject = "Appointment Rejected";
            string message = $"Dear {fullName},\n\nYour appointment scheduled for {date} has been rejected.\nReason: {reason}\n\nRegards,\nMedical Trip Team";

            await SendEmailAsync(email, subject, message);

            return Ok("Appointment rejected and email sent.");
        }
        [HttpGet("booked-dayst/{nurseId}/{year}-{month}")]

        public IActionResult GetBookedDayst(int nurseId, int year, int month)
        {
            var appointments = _context.bookCars
                .Where(a => a.carId == nurseId &&
                            a.Approve == "Approved")
                .ToList();

            var bookedDays = new HashSet<string>();

            foreach (var appt in appointments)
            {
                var start = appt.Startdate;
                var end = appt.Enddate;

                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    if (date.Year == year && date.Month == month)
                    {
                        bookedDays.Add(date.ToString("yyyy-MM-dd"));
                    }
                }
            }

            return Ok(bookedDays);
        }

        [HttpGet("for-montht/{nurseId}/{year}-{month}")]
        public IActionResult GetAppointmentsForMontht(int nurseId, int year, int month)
        {
            try
            {
                // Validate date parameters
                if (year < 2000 || year > 2100 || month < 1 || month > 12)
                {
                    return BadRequest("Invalid date parameters");
                }

                // Create start and end date for the requested month
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Query appointments that overlap with this month
                var appointments = _context.bookCars
                    .Where(a => a.carId == nurseId &&
                             a.Startdate <= endDate &&
                             a.Enddate >= startDate &&
                             a.Approve == "Approved")
                    .Select(a => new
                    {
                        id = a.Id,
                        touristId = a.TouristId,
                        touristName = a.Tourist.User.FullName, // Assuming Tourist has a Name property
                        startDate = a.Startdate.ToString("yyyy-MM-dd"),
                        endDate = a.Enddate.Value.ToString("yyyy-MM-dd"),
                        approve = a.Approve,
                        response = a.Response,
                    })
                    .ToList();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPut("rejectt/{id}")]
        public async Task<IActionResult> RejectAppointmentt(int id, [FromBody] string reason)
        {
            var appointment = await _context.bookCars
                .Include(a => a.Tourist)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound("Appointment not found");

            appointment.Approve = "Rejected";
            appointment.Response = reason; // save the reason in DB
            await _context.SaveChangesAsync();

            // Send rejection email
            var email = appointment.Tourist.User.Email;
            var fullName = appointment.Tourist.User.FullName;
            var date = appointment.Startdate.ToString("yyyy-MM-dd");

            string subject = "Appointment Rejected";
            string message = $"Dear {fullName},\n\nYour Car Booking  scheduled for {date} has been rejected.\nReason: {reason}\n\nRegards,\nMedical Trip Team";

            await SendEmailAsync(email, subject, message);

            return Ok("Appointment rejected and email sent.");
        }

        [HttpGet("approvedp/{date}")]
        public IActionResult GetApprovedAppointmentsp(string date)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var service = _context.Serviceproviders.FirstOrDefault(p => p.UserId == userId);
                var doctor = _context.Freelancers.FirstOrDefault(p => p.ServiceProviderId == service.Id);

                if (doctor == null)
                    return BadRequest("Doctor not found");

                DateOnly parsedDate = DateOnly.Parse(date); // this line might be throwing the exception

                var appointments = _context.bookFreelances
                    .Include(p => p.Tourist)
                    .ThenInclude(t => t.User)
                    .Where(a => a.FreelanceId == doctor.Id && a.Approve == true && a.BookingDate == parsedDate)
                    .Select(a => new
                    {
                        a.Id,
                        bookingdate = a.BookingDate.Value.ToString("yyyy-MM-dd"),
                        startTime = a.StartTime,
                        response = a.Response,
                        tourist = new
                        {
                            user = new
                            {
                                fullName = a.Tourist.User.FullName
                            }
                        }
                    }).ToList();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        // GET: api/Appointments/approved-month/2023-01
        [HttpGet("approved-monthp/{yearMonth}")]
        public async Task<ActionResult<IEnumerable<BookFreelance>>> GetApprovedAppointmentsByMonthp(string yearMonth)
        {


            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
                var doctor = await _context.Freelancers.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);

                if (doctor == null)
                    return BadRequest("Doctor not found");

                var parts = yearMonth.Split('-');
                var year = int.Parse(parts[0]);
                var month = int.Parse(parts[1]);

                var appointments = await _context.bookFreelances
                    .Where(a => a.BookingDate.Value.Year == year &&
                                a.BookingDate.Value.Month == month &&
                                a.Approve == true &&
                                a.FreelanceId == doctor.Id) // 👈 Filter by doctor
                    .Select(a => new {
                        bookingdate = a.BookingDate.HasValue ? a.BookingDate.Value.ToString("yyyy-MM-dd") : null
                    })
                    .ToListAsync();

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("rejectp/{id}")]
        public async Task<IActionResult> RejectAppointmentp(int id, [FromBody] string reason)
        {
            var appointment = await _context.bookFreelances
                .Include(a => a.Tourist)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
                return NotFound("Appointment not found");

            appointment.Approve = false;
            appointment.Response = reason; // save the reason in DB
            await _context.SaveChangesAsync();

            // Send rejection email
            var email = appointment.Tourist.User.Email;
            var fullName = appointment.Tourist.User.FullName;
            var date = appointment.BookingDate;
            var time = appointment.StartTime;

            string subject = "Appointment Rejected";
            string message = $"Dear {fullName},\n\nYour appointment scheduled for {date} at {time} has been rejected.\nReason: {reason}\n\nRegards,\nMedical Trip Team";

            await SendEmailAsync(email, subject, message);

            return Ok("Appointment rejected and email sent.");
        }
        [HttpPut("reject-allp/{date}")]
        public async Task<IActionResult> RejectAllAppointmentsForDatep(string date, [FromBody] string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var service = await _context.Serviceproviders.FirstOrDefaultAsync(p => p.UserId == userId);
            var doctor = await _context.Freelancers.FirstOrDefaultAsync(p => p.ServiceProviderId == service.Id);

            if (doctor == null)
                return BadRequest("Doctor not found");

            DateOnly parsedDate = DateOnly.Parse(date);
            var appointments = await _context.bookFreelances
                .Include(a => a.Tourist)
                .ThenInclude(t => t.User)
                .Where(a => a.FreelanceId == doctor.Id && a.Approve == true && a.BookingDate == parsedDate)
                .ToListAsync();

            foreach (var appointment in appointments)
            {
                appointment.Approve = false;
                appointment.Response = reason;

                // Email each user
                var email = appointment.Tourist.User.Email;
                var fullName = appointment.Tourist.User.FullName;
                var time = appointment.StartTime;
                string subject = "Appointment Rejected";
                string message = $"Dear {fullName},\n\nYour appointment scheduled for {parsedDate} at {time} has been rejected.\nReason: {reason}\n\nRegards,\nMedical Trip Team";
                await SendEmailAsync(email, subject, message);
            }

            await _context.SaveChangesAsync();

            return Ok("All appointments rejected and emails sent.");
        }
        [HttpGet("check-subscription")]
        public async Task<IActionResult> CheckSubscription()
        {
            var user = await _usermanager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }
            var needsSubscription = await _subscriptionService.NeedsSubscriptionAsync(user.Id);
            return Ok(new
            {
                needsSubscription,
                message = needsSubscription ? "Please subscribe to continue using the service" : "Subscription active"
            });
        }

    }

    public class StatusUpdateModel
    {
        public string Status { get; set; }
    }



}
