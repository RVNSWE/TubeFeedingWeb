namespace TubeFeedingWeb.Client
{
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

        private double totalVolumePerDay; // (ml)
        private double totalVolumePerMeal; // (ml)
        private int mealsPerDay;

        public void CalculateFeedingPlan()
        {
            double rER = 70 * Math.Pow(BodyWeight, 0.75); // Resting energy requirement (kcal)
            double foodPerDay = rER / KcalPerG; // Total food per day (g)
            double containersPerDay = foodPerDay / DietNetWeight; // Estimated containers of food used up per day
            double dietWaterVolume = foodPerDay * (DietWaterPercentage / 100); // Volume of water contained in food (ml)
            double maxVolumePerMeal = BodyWeight * 20; // Max volume to be administered per meal (ml)
            double flushVolume = CalculateFlushVolume(); // Volume of water to flush the tube with before and after each meal (ml)
            double waterPerDay = CalculateWaterPerDay(dietWaterVolume); // Additional water needed per day (ml)
            totalVolumePerDay = foodPerDay + waterPerDay; // Estimated total volume administered per day (ml)
            mealsPerDay = (int)Math.Round(totalVolumePerDay / maxVolumePerMeal, 0, MidpointRounding.AwayFromZero); // Number of meals per day
            totalVolumePerMeal = totalVolumePerDay / mealsPerDay; // Estimated total volume administered per meal (ml)

            while (totalVolumePerMeal > maxVolumePerMeal) // While the volume administered per meal exceeds the max allowable volume
            {
                mealsPerDay ++; // Add another meal per day
                totalVolumePerMeal = totalVolumePerDay / mealsPerDay; // Recalculate the volume per meal

                if(mealsPerDay > 23)
                {
                    break; // Do not exceed one meal per hour
                }
            }

            double foodPerMeal = foodPerDay / mealsPerDay; // Food to administer per meal (g)
            double waterPerMeal = (waterPerDay / mealsPerDay) - (flushVolume * 2); // Extra water needed per meal (ml)

            if(waterPerMeal < 0)
            {
                waterPerMeal = 0; // Water per meal cannot be negative
            }

            switch (BodyWeight) // Round volumes per meal to more appropriate SF
            {
                case < 10:
                    foodPerMeal = Math.Round(foodPerMeal, 2, MidpointRounding.AwayFromZero);
                    waterPerMeal = Math.Round(waterPerMeal, 2, MidpointRounding.AwayFromZero);
                    break;
                case < 20:
                    foodPerMeal = Math.Round(foodPerMeal, 1, MidpointRounding.AwayFromZero);
                    waterPerMeal = Math.Round(waterPerMeal, 1, MidpointRounding.AwayFromZero);
                    break;
                default:
                    foodPerMeal = Math.Round(foodPerMeal, 0, MidpointRounding.AwayFromZero);
                    waterPerMeal = Math.Round(waterPerMeal, 0, MidpointRounding.AwayFromZero);
                    break;
            }
            containersPerDay = Math.Round(containersPerDay, 1, MidpointRounding.AwayFromZero);
        }

        /*
         * Calculate the volume of water to flush the feeding tube with before and after each feed.
         * If bodyweight is greater than value, check next value. Return first value lower than bodyweight.
         */
        private double CalculateFlushVolume()
        {
            double flushVolume = BodyWeight switch
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

            return flushVolume;
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
    }
}