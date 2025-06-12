using MedicalTourismApp.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Hospital
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Serviceprovider")]
    public int ServiceProviderId { get; set; }

    
    public string Name { get; set; }
    
    public string Location { get; set; }
    public string ContactNumber { get; set; }
    public string Email { get; set; }
    public string Website { get; set; }
    public List<Image> Images { get; set; } = new List<Image>(); // Navigation property
    public int YearEstablished { get; set; }
    public int NumberOfBeds { get; set; }
    public string Specialties { get; set; }
    public int MedicalStaffCount { get; set; }
    public string Equipment { get; set; }
    public double StartingPrice { get; set; }
    public bool EmergencyAvailable { get; set; }
    public bool Open24Hours { get; set; }
    public string LanguagesSpoken { get; set; }
    public string LicenseDocuments { get; set; }
    public Serviceprovider ServiceProvider { get; set; }
}
