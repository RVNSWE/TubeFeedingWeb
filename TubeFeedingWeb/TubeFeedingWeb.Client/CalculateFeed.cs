namespace TubeFeedingWeb.Client
{
    /*
     * Calculate food and water volumes for a specific day.
     */
    public class CalculateFeed
    {
        public double ContainersPerDay { get; set; }
        public int MealsPerDay { get; set; }
        public double MaxVolumePerMeal { get; set; }
        public double TotalVolumePerMeal { get; set; }
        public double FoodPerMeal { get; set; }
        public double WaterPerMeal { get; set; }

        private double totalVolumePerDay; // (ml)
        private readonly float dayModifier;
        private readonly UserInput input;

        public CalculateFeed(UserInput userInput, int day)
        {
            input = userInput;
            dayModifier = (1 / input.Days) * day; // Get starting RER fraction and multiply it by the current day number

            CalculateFeedingPlan();
        }

        private void CalculateFeedingPlan()
        {
            double rER = (70 * Math.Pow(input.BodyWeight, 0.75)) * dayModifier; // Resting energy requirement (kcal) for the current day
            double foodPerDay = rER / input.KcalPerG; // Total food per day (g)
            ContainersPerDay = foodPerDay / input.DietNetWeight; // Estimated containers of food used up per day
            double dietWaterVolume = foodPerDay * (input.DietWaterPercentage / 100); // Volume of water contained in food (ml)
            MaxVolumePerMeal = input.BodyWeight * 20; // Max volume to be administered per meal (ml)
            double waterPerDay = CalculateWaterPerDay(dietWaterVolume); // Additional water needed per day (ml)
            totalVolumePerDay = foodPerDay + waterPerDay; // Estimated total volume administered per day (ml)
            MealsPerDay = (int)Math.Round(totalVolumePerDay / MaxVolumePerMeal, 0, MidpointRounding.AwayFromZero); // Number of meals per day
            TotalVolumePerMeal = totalVolumePerDay / MealsPerDay; // Estimated total volume administered per meal (ml)

            while (TotalVolumePerMeal > MaxVolumePerMeal) // While the volume administered per meal exceeds the max allowable volume
            {
                MealsPerDay++; // Add another meal per day
                TotalVolumePerMeal = totalVolumePerDay / MealsPerDay; // Recalculate the volume per meal

                if (MealsPerDay > 23)
                {
                    break; // Do not exceed one meal per hour
                }
            }

            FoodPerMeal = foodPerDay / MealsPerDay; // Food to administer per meal (g)
            WaterPerMeal = (waterPerDay / MealsPerDay) - (input.FlushVolume * 2); // Extra water needed per meal (ml)

            if (WaterPerMeal < 0)
            {
                WaterPerMeal = 0; // Water per meal cannot be negative
            }

            switch (input.BodyWeight) // Round volumes per meal to more appropriate SF
            {
                case < 10:
                    FoodPerMeal = Math.Round(FoodPerMeal, 2, MidpointRounding.AwayFromZero);
                    WaterPerMeal = Math.Round(WaterPerMeal, 2, MidpointRounding.AwayFromZero);
                    break;
                case < 20:
                    FoodPerMeal = Math.Round(FoodPerMeal, 1, MidpointRounding.AwayFromZero);
                    WaterPerMeal = Math.Round(WaterPerMeal, 1, MidpointRounding.AwayFromZero);
                    break;
                default:
                    FoodPerMeal = Math.Round(FoodPerMeal, 0, MidpointRounding.AwayFromZero);
                    WaterPerMeal = Math.Round(WaterPerMeal, 0, MidpointRounding.AwayFromZero);
                    break;
            }
            ContainersPerDay = Math.Round(ContainersPerDay, 1, MidpointRounding.AwayFromZero);
        }

        /*
         * Calculate the estimated daily fluid requirement based on species (true = cat, false = dog), and return the result.
         */
        private double CalculateWaterPerDay(double dietWaterVolume)
        {
            double waterPerDay;

            if (input.Species == true)
            {
                waterPerDay = 80 * Math.Pow(input.BodyWeight, 0.75);
            }
            else
            {
                waterPerDay = 132 * Math.Pow(input.BodyWeight, 0.75);
            }

            if (dietWaterVolume > 0)
            {
                waterPerDay -= dietWaterVolume;
            }

            return waterPerDay;
        }
    }
}
