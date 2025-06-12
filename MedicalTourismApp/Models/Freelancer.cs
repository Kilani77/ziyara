using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class Freelancer
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("ServiceProvider")]
        public int ServiceProviderId { get; set;}
        public string available { get; set; } 
        public string language { get; set; }
        public string supportedArea { get; set; }
        public string Typeofserivce { get; set; }
        public string exprienece { get; set; }
        public double price { get; set; }
        public List<Image> Images { get; set; } = new List<Image>();
        public ICollection<BookFreelance> bookFreelances { get; set; } = new List<BookFreelance>();
        public Serviceprovider ServiceProvider { get; set; }
    }
}
