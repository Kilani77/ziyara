using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class GuideBooking
    {
        public int Id { get; set; }
        [ForeignKey("Tourist")]
        public int TouristId { get; set; }
        [ForeignKey("Guide")]
        public int GuideId { get; set; }
        public string? AreaBook { get; set; }
        public DateOnly BookingDate { get; set; }
        public string? Approve { get; set; } = "Pending";
        public string? Response { get; set; }
        public Tourist Tourist { get; set; }
        public Guide Guide { get; set; }
    }
}
