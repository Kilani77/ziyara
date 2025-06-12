using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class Doctor
    {
        [Key]
        public int Id { get; set; } // Primary key
        [ForeignKey("ServiceProvider")]
        public int ServiceProviderId { get; set; }
        public string exprienece { get; set; }
        public string Specialization { get; set; }
        public string location { get; set; }
        public List<Image> Images { get; set; } = new List<Image>(); 
        public string available { get; set; }
        public double price { get; set; }
        public string Bio { get; set; }
        public Serviceprovider ServiceProvider { get; set; }
        public ICollection<app_doctor> app_Doctors { get; set; } = new List<app_doctor>();

    }
}
