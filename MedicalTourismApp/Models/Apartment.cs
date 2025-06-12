using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class Apartment
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Serviceprovider")]
        public int ServiceProviderId { get; set; }
        public string Name { get; set; }
        public string ContactNumber { get; set; } 
        public int NumberOfRooms { get; set; }
        public bool HasKitchen { get; set; }
        public bool HasBalcony { get; set; }
        public string Location { get; set; }
        public string Available { get; set; }
        public int CapacityOfPlace { get; set; }
        public string Description { get; set; } 
        public double Price { get; set; }
        public List<Image> Images { get; set; } = new List<Image>();
        public Serviceprovider serviceprovider { get; set; }
        public ICollection<calapartment> calapartments { get; set; } = new List<calapartment>();

    }
}
