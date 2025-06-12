using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class BookCar
    {

        public int Id { get; set; }
        [ForeignKey("Tourist")]
        public int TouristId { get; set; }
        [ForeignKey("Transportation")]
        public int carId { get; set; }
        public DateTime Startdate { get; set; }
        public DateTime? Enddate { get; set; }
        public string? Approve { get; set; } = "Pending";
        public string? Response { get; set; }
        public Tourist Tourist { get; set; }
        public Transportation Transportation { get; set; }
    }
}
