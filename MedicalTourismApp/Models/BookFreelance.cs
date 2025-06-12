using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class BookFreelance
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Freelancer")]
        public int FreelanceId { get; set; }
        [ForeignKey("Tourist")]
        public int TouristId { get; set; }
        public DateOnly? BookingDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public TimeSpan? StartTime { get; set; } 
        public TimeSpan? EndTime { get; set; }
        public bool? Approve { get; set; } = null;
        public string? Response { get; set; }
        public Tourist Tourist { get; set; }
        public Freelancer Freelancer { get; set; }
    }
}
