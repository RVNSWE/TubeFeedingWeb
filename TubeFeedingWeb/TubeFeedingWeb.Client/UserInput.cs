namespace TubeFeedingWeb.Client
{
    /*
     * TO DO: Make editable
     */
    public class UserInput
    {
        public double BodyWeight { get; set; } // (kg)
        public bool Species { get; set; } // true = cat, false = dog
        public int Days { get; set; }
        public string? PatientName { get; set; } = string.Empty;
        public string? DietName { get; set; } = string.Empty;
        public double KcalPerG { get; set; }
        public double DietNetWeight { get; set; } // (g)
        public double DietWaterPercentage { get; set; }
        public double FlushVolume { get; set; }

        public UserInput()
        {
            CalculateFlushVolume();
        }

        /*
         * Calculate the volume of water to flush the feeding tube with before and after each feed.
         * If bodyweight is greater than value, check next value. Return first value lower than bodyweight.
         */
        private void CalculateFlushVolume()
        {
            FlushVolume = BodyWeight switch
            {
                < 1.5 => 1,
                < 2 => 1.5,
                < 3 => 2,
                < 4 => 2.5,
                < 4.5 => 3,
                < 5 => 4,
                < 8 => 5,
                < 20 => 6,
                _ => 10,
            };
        }
    }
}