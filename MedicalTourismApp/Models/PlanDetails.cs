using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedicalTourismApp.Models
{
    public class PlanDetails
    {
        public int Id { get; set; }
        public int Months { get; set; }
        public decimal Price { get; set; }
        public bool IsPopular { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        [NotMapped]
        public decimal? SavingsPercentage
        {
            get
            {
                if (Months == 1) return 0;

                // Get the base monthly price (assuming 1-month plan is the base)
                var baseMonthlyPrice = 5m; // This should be retrieved from the 1-month plan in production
                decimal expectedTotal = baseMonthlyPrice * Months;
                decimal savings = (expectedTotal - Price) / expectedTotal * 100;

                return Math.Round(savings, 0); 
            }
        }

        [NotMapped]
        public string SavingsText => SavingsPercentage.HasValue ? $"{SavingsPercentage}% savings" : null;

    }
}
