using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
 {
    public class Transportation
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("ServiceProvider")]
        public int ServiceProviderId { get; set; }
        public string FullName { get; set; } 
        public int Numberofseats { get; set; }
        public string modelocar { get; set; } 
        public string LicensePlate { get; set; } 
        public string License { get; set; } 
        public string ContactNumber { get; set; }
        public string Location { get; set; } 
        public List<Image> Images { get; set; } = new List<Image>();
        public double PricePerDay { get; set; } 
        public bool DriverAvalablity { get; set; }
        public string Bio { get; set; } 
        public Serviceprovider ServiceProvider { get; set; }
    }
}
