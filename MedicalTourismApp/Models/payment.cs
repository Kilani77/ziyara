namespace MedicalTourismApp.Models
{
    public class payment
    {
        public int Id { get; set; }
        public string CardNumber { get; set; }
        public string Name { get; set; }
        public int Cvc { get; set; }
        public double Amount { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
