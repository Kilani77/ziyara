using Microsoft.AspNetCore.Identity;

namespace MedicalTourismApp.Models
{
    public class User:IdentityUser
    {
        public string FullName { get; set; }
        public string UserType { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFirstLogin { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public ICollection<Testimonial> testimonials { get; set; } = new List<Testimonial>();

    }
}
