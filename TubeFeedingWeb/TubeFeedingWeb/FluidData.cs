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
        public double MonitoringDuration { get; set; } = 6;
        [Required(ErrorMessage = "Please enter a fluid therapy rate as ml/kg/hr (leave as 0 if not on fluids)."),
            Range(0, 100000, ErrorMessage = "Must be a number from 0 to 100000.")]
        public double IVFT { get; set; }
        [Required(ErrorMessage = "Please enter the estimated volume of water consumed in millilitres (leave as 0 if none)."),
            Range(0, 100000, ErrorMessage = "Must be a number from 0 to 100000.")]
        public double OralWater { get; set; }
        [Required(ErrorMessage = "Please enter the estimated water intake from other sources in millilitres (leave as 0 if none)."),
            Range(0, 100000, ErrorMessage = "Must be a number from 0 to 100000.")]
        public double OtherWater { get; set; }
        [Required(ErrorMessage = "Please enter the percentage moisture content of the diet being fed (or an estimate if exact percentage unknown)."),
            Range(0.1, 99.9, ErrorMessage = "Must be a number from 0.1 to 99.9.")]
        public double FoodMoistureContent { get; set; }
        [Required(ErrorMessage = "Please enter an estimate of the amount of food eaten in grams."),
            Range(0, 100000, ErrorMessage = "Must be a number from 0 to 100000.")]
        public double AmountEaten { get; set; }
        [Required(ErrorMessage = "Please enter the volume of urine produced in millilitres (or an estimate if exact amount unknown)."),
            Range(0, 100000, ErrorMessage = "Must be a number from 0 to 100000.")]
        public double Urine { get; set; }
        [Required(ErrorMessage = "Please enter the volume of fluid lost by any other means in millilitres."),
            Range(0, 100000, ErrorMessage = "Must be a number from 0 to 100000.")]
        public double OtherOutput { get; set; }
        [Required(ErrorMessage = "Please enter the number of incidences of diarrhoea."),
            Range(0, 1000, ErrorMessage = "Must be a number from 0 to 1000.")]
        public double Diarrhoea { get; set; }
        [Required(ErrorMessage = "Please enter the number of times the patient has vomited."),
            Range(0, 1000, ErrorMessage = "Must be a number from 0 to 1000.")]
        public double Vomiting { get; set; }
    }
}