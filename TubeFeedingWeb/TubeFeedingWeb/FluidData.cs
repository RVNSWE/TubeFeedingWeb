using System.ComponentModel.DataAnnotations;

namespace TubeFeedingWeb
{
    public class FluidData
    {
        [Required(ErrorMessage = "The patient's body weight is required."),
            Range(0.01, 200, ErrorMessage = "Please enter a valid body weight in kg, eg. 5.4.")]
        public double BodyWeight { get; set; }
        [Required(ErrorMessage = "Please enter the number of hours these measurements were taken over."),
            Range(0.01, 72, ErrorMessage = "Must be greater than 0 and less than 72.")]
        public double MeasurementDuration { get; set; } = 6;
        [Required(ErrorMessage = "Please enter a fluid therapy rate as ml/kg/hr (leave as 0 if not on fluids).")]
        public double IVFT { get; set; } = 0;
        [Required(ErrorMessage = "Please enter the estimated volume of water consumed (leave as 0 if none)."),
            Range(0.1, 52, ErrorMessage = "Please enter a number from 0.1 to 52.")]
        public double OralWater { get; set; } = 0;
        [Required(ErrorMessage = "Please enter the estimated water intake from other sources eg. flush, CRIs, liquid treats etc. (leave as 0 if none)."),
            Range(0.1, 52, ErrorMessage = "Please enter a number from 0.1 to 52.")]
        public double OtherWater { get; set; } = 0;
        [Required(ErrorMessage = "Please enter the percentage moisture content of the diet being fed (or an estimate if not listed or on multiple types of food)."),
            Range(0.1, 99.9, ErrorMessage = "Please enter a number from 0.1 to 99.9.")]
        public double FoodMoistureContent { get; set; } = 0;
    }
}