using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class Image
    {
        public int Id { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }
        [ForeignKey("Serviceprovider")]
        public int ServiceProviderId { get; set; }
        public Serviceprovider ServiceProvider { get; set; } 
    }
}
