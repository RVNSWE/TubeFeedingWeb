using System.ComponentModel.DataAnnotations;

namespace TubeFeedingWeb
{
    public class PatientDietData
    {
        [Required(ErrorMessage = "Please select a species.")]
        public string Species { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter a body weight in kilograms."),
            Range(0.01, 200, ErrorMessage = "Please enter a valid body weight in kg, eg. 5.4.")]
        public double BodyWeight { get; set; } // (kg)
        [Range(1, 10, ErrorMessage = "Please enter a whole number from 1 to 10.")]
        public int Days { get; set; } = 3;
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Only alphanumeric characters are permitted (a-z and 0-9)."),
            MaxLength(100, ErrorMessage = "Length cannot exceed 100 characters.")]
        public string? PatientName { get; set; } = string.Empty;
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Only alphanumeric characters are permitted (a-z and 0-9)."),
            MaxLength(100, ErrorMessage = "Length cannot exceed 100 characters.")]
        public string? DietName { get; set; } = string.Empty;
        [Required(ErrorMessage = "Please enter the kcal per gram (or per millilitre) of the diet to be fed."),
            Range(0.1, 10, ErrorMessage = "Must be kcal/g or kcal/ml.")]
        public double KcalPerG { get; set; }
        [Required(ErrorMessage = "Please enter the volume or weight of food per container in either grams or millilitres."),
            Range(1,1000, ErrorMessage = "Please enter the total weight (g) or volume (ml) of food in each container. If using a powdered food, please enter the weight or volume after reconstitution.")]
        public double DietNetWeight { get; set; } // (g)
        [Required(ErrorMessage = "Please enter the percentage moisture content of the diet."),
            Range(1,99, ErrorMessage = "Only numbers from 1 - 99 are valid. If using a powdered food, please enter the percentage moisture content after reconstitution.")]
        public double DietWaterPercentage { get; set; }
        public double FractionRER { get; set; }
        public double FlushVolume { get; set; }
    }
}