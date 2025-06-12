using Microsoft.AspNetCore.Mvc;
using MedicalTourismApp.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace MedicalTourismApp.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<User> _userManager;

        public UsersController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }
        public IActionResult Users()
        {
            return View();
        }

        // Renders the Users Page and passes the list of users to the view
        public async Task<IActionResult> user()
        {
            // Fetching the list of users
            var users = _userManager.Users.ToList().Select(user => new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FullName,
                user.UserType,
                IsActive = user.IsActive ? "Active" : "Inactive"
            }).ToList();

            // Pass the list to the view
            return View(users);
        }

        // API to get Users List
        [HttpGet]
        [Route("api/users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = _userManager.Users.Where(user => user.UserType == "Tourist")
            .ToList().Select(user => new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FullName,
                user.UserType,
                IsActive = user.IsActive ? "Active" : "Inactive"
            }).ToList();

            return Ok(users);
        }
        // API to Update User Status
        [HttpPost]
        [Route("api/users/updateStatus")]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Id) || string.IsNullOrEmpty(request.Status))
                return BadRequest("Invalid request.");

            var user = await _userManager.FindByIdAsync(request.Id);
            if (user == null)
                return NotFound("User not found.");

            // Update the user's IsActive status based on the status string
            user.IsActive = request.Status == "Active";

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                return Ok("User status updated successfully.");

            return BadRequest("Failed to update user status.");
        }

        // Request model
        public class UpdateUserStatusRequest
        {
            public string Id { get; set; }
            public string Status { get; set; } // "Active" or "Inactive"
        }
    }
    namespace MedicalTourismApp.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        public class UserController : ControllerBase
        {
            private readonly UserManager<User> _userManager;

            public UserController(UserManager<User> userManager)
            {
                _userManager = userManager;
            }

            // API to get the total number of users
            [HttpGet("count")]
            public async Task<IActionResult> GetTotalUsersCount()
            {
                var totalUsers = _userManager.Users.Count(); // Count all users

                return Ok(new { TotalUsers = totalUsers });
            }




        }
    }
}
