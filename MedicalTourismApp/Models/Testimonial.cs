using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedicalTourismApp.Models
{
    public class Testimonial
    {
        public int Id { get; set; }
        [ForeignKey("User")]
        public string UserId{ get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Message { get; set;} 
        public bool Status { get; set;} = false;
        public DateTime Date { get; set;} = DateTime.Now; 
        public User User {get; set;}

    }
}
