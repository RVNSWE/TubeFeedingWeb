using System.ComponentModel.DataAnnotations;

namespace TubeFeedingWeb
{
    public class WeightClinicData
    {
        [Required(ErrorMessage = "A body condition score out of 9 must be entered."),
            Range(6, 9, ErrorMessage = "Must be 6 or greater.")]
        public int BCS { get; set; } = 9;
        [Required(ErrorMessage = "The patient's body weight is required."),
            Range(0.01, 200, ErrorMessage = "Please enter a valid body weight in kg, eg. 5.4.")]
        public double CurrentBodyWeight { get; set; }
        [Required(ErrorMessage = "The patient's body weight is required."),
            Range(0.01, 200, ErrorMessage = "Please enter a valid body weight in kg, eg. 5.4.")]
        public double LastBodyWeight { get; set; } = 10;
        [Required(ErrorMessage = "Number of weeks since patient last weighed is required."),
            Range(0.1, 52, ErrorMessage = "Please enter a number from 0.1 to 52.")]
        public double WeeksSinceLastWeighed { get; set; } = 4;
        [Required(ErrorMessage = "Number of weeks until next weight check is required."),
            Range(0.1, 52, ErrorMessage = "Please enter a number from 0.1 to 52.")]
        public double WeeksUntilNextWeighed { get; set; } = 4;
    }
}