using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Tourist")]
        public int TouristId { get; set; } 
        [ForeignKey("ServiceProvider")]
        public int ServiceProviderId { get; set; }
        public string AppointmentType { get; set; } 
        public DateTime? AppointmentDate { get; set; }
        public DateTime? CreatedAt { get; set; }=DateTime.Now;
        public TimeSpan? AppointmentTime { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }          
        public Tourist Tourist { get; set; }
        public Serviceprovider ServiceProvider { get; set; }
    }
}
