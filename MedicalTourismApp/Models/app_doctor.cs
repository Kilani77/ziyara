using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class app_doctor
    {
        public int Id { get; set; }
        [ForeignKey("Tourist")]
        public int TouristId { get; set;}
        [ForeignKey("Doctor")]
        public int DoctorId { get; set;}
        public DateOnly date { get; set; }
        public TimeSpan StartTime {get; set;} 
        public string? Approve {get; set;} ="pending";
        public string? Response {get; set;} 
        public Tourist Tourist { get; set; }
        public Doctor Doctor { get; set; } 
    }
}
