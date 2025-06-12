using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class Guide
    {
    [Key]
    public int Id { get; set; } // Primary key
    [ForeignKey("ServiceProvider")]
    public int ServiceProviderId { get; set; }
    public string ContactNumber { get; set; }
    public string location { get; set; }
    public List<Image> Images { get; set; } = new List<Image>(); // Navigation property
    public string available { get; set; }
    public string language { get; set; }
    public string city { get; set; } 
    public string supportedArea { get; set; }
    public string exprienece { get; set; }
    public double startPrice { get; set; }
    public Serviceprovider ServiceProvider { get; set; }

}

}
