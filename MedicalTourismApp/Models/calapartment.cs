using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class calapartment
    {
        public int Id { get; set; }
        [ForeignKey("Tourist")]
        public int TouristId { get; set; }
        [ForeignKey("Apartment")]
        public int ApartmentId { get; set; }
        public DateTime Startdate { get; set; }
        public DateTime? Enddate { get; set; }
        public int numberEscorts { get; set; }
        public string? Approve { get; set; } = "Pending";
        public string? Response { get; set; }
        public Tourist Tourist { get; set; }
        public Apartment apartment { get; set; }

    }
}
