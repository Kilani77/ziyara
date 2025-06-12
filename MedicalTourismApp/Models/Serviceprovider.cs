using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace MedicalTourismApp.Models
{
    public class Serviceprovider
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public User User { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime RegistrationTime { get; set; }
        public string? ServiceType { get; set; }
        public int NumberView { get; set; } = 0;
        public bool IsFreeMonthActive { get; set; }=true;
        public bool IsSubscribe{ get; set; }=false;
        public DateTime? MonthEndDate { get; set; }
        public List<Image> Images { get; set; } = new List<Image>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>(); 
    }
}
