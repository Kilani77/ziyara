using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedicalTourismApp.Models
{
    public class Hotel 
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("Serviceprovider")]
        public int ServiceProviderId { get; set; }
        public int NumberOfRooms { get; set; }
        public string Name { get; set; }
        public string ContactNumber { get; set; }
        public bool HasPool { get; set; }
        public bool HasGym { get; set; }
        public string CapacityOfPlace { get; set; }
        public string Location { get; set; }
        public string Available { get; set; }
        public bool RoomService { get; set; } 
        public bool WifiAvailability { get; set; }
        public bool ParkingAvailability { get; set; } 
        public bool BreakfastIncluded { get; set; }
        public string Description { get; set; } 
        public double Price { get; set; }
        public Serviceprovider serviceprovider { get; set; }
    }


}
