using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedicalTourismApp.Models
{
    public class Tourist
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; } // Foreign key linking to User
        public string PassportNumber { get; set; }
        public string Nationality { get; set; }
        public string PreferredLanguage { get; set; }

        // Navigation property
        public User User { get; set; }
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<app_doctor> app_Doctors { get; set; } = new List<app_doctor>();
        public ICollection<calapartment> calapartments { get; set; } = new List<calapartment>();
        public ICollection<GuideBooking> guideBookings { get; set; } = new List<GuideBooking>();
        public ICollection<BookFreelance> bookFreelances { get; set; } = new List<BookFreelance>();
        public ICollection<BookCar> bookCars { get; set; } = new List<BookCar>();
      
    }
}
