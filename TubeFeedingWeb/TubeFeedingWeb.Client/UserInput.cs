namespace TubeFeedingWeb.Client
{
    public class UserInput
    {
        public double BodyWeight { get; set; }
        public bool Species { get; set; } // true = cat, false = dog
        public int Days { get; set; }
        public string? PatientName { get; set; } = string.Empty;
        public string? DietName { get; set; } = string.Empty;
        public double KcalPerMl { get; set; }
        public double DietNetWeight { get; set; }
        public double DietWaterPercentage { get; set; }
        private double totalVolumePerDay;
        private int mealsPerDay;

        public void CalculateFeedingPlan()
        {
            double rER = 70 * Math.Pow(BodyWeight, 0.75); // Resting energy requirement
            double foodPerDay = rER / KcalPerMl; // Total food per day (g)
            double dietWaterVolume = foodPerDay * (DietWaterPercentage / 100); // Volume of water contained in food (ml)
            double maxVolumePerMeal = BodyWeight * 20; // Max volume to be administered per meal
            double flushPerMeal = CalculateFlushPerMeal(); // Volume of water to flush the tube with before and after each meal
            double waterPerDay = CalculateWaterPerDay(dietWaterVolume); // Additional water needed per day
            totalVolumePerDay = foodPerDay + waterPerDay; // Total volume administered per day
            mealsPerDay = (int)Math.Round(totalVolumePerDay / maxVolumePerMeal, 0, MidpointRounding.AwayFromZero); // Number of meals per day
            
        }

        /*
         * Calculate the volume of water to flush the feeding tube with before and after each feed.
         * If bodyweight is greater than value, check next value. Return first value lower than bodyweight.
         */
        private double CalculateFlushPerMeal()
        {
            double flushPerMeal = BodyWeight switch
            {
                < 1.5 => 2,
                < 2 => 3,
                < 3 => 4,
                < 4 => 5,
                < 4.5 => 6,
                < 5 => 8,
                < 8 => 10,
                < 20 => 12,
                _ => 20,
            };

            return flushPerMeal;
        }

        /*
         * Calculate the estimated daily fluid requirement based on species (true = cat, false = dog), and return the result.
         */
        private double CalculateWaterPerDay(double dietWaterVolume)
        {
            double waterPerDay;

            if (Species == true)
            {
                waterPerDay = 80 * Math.Pow(BodyWeight, 0.75);
            }
            else
            {
                waterPerDay = 132 * Math.Pow(BodyWeight, 0.75);
            }

            if (dietWaterVolume > 0)
            {
                waterPerDay -= dietWaterVolume;
            }

            return waterPerDay;
        }

        private void CalculateMeals(double foodPerDay, double flushPerMeal, double waterPerDay)
        {
            double waterToAddPerDay = waterPerDay - (flushPerMeal * mealsPerDay);

            if (waterToAddPerDay < 0)
            {
                waterToAddPerDay = 0;
            }

            double foodPerMeal = foodPerDay / mealsPerDay;
            double waterToAddPerMeal = waterToAddPerDay / mealsPerDay;

        }
    }
}