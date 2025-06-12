using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class Avaliabledoctor
    {
            public int Id { get; set; }
            [ForeignKey("Doctor")]
            public int DoctorId { get; set; } // Foreign key to Doctor
            public DayOfWeek DayOfWeek { get; set; } // Sunday, Monday, etc.
            public TimeSpan StartTime { get; set; } // e.g., 09:00 AM
            public TimeSpan EndTime { get; set; } // e.g., 05:00 PM
        
    }
}
