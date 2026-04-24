namespace TubeFeedingWeb.Client
{
    /*
     * Calculate food and water volumes for a specific day.
     */
    public class Volumes
    {
        public double ContainersPerDay { get; set; }
        public int MealsPerDay { get; set; }
        public double MaxVolumePerMeal { get; set; }
        public double TotalVolumePerMeal { get; set; }
        public double FoodPerMeal { get; set; }
        public double WaterPerMeal { get; set; }
        public int Day { get; set; }
        public IReadOnlyCollection<string> FormattedFeedingTimes { get; set; }

        private double totalVolumePerDay; // (ml)
        private readonly PatientDietData data;

        public Volumes(PatientDietData data, int day)
        {
            this.data = data;
            Day = day;
            FormattedFeedingTimes = [];
        }

        public void Calculate()
        {
            float dayMultiplier = data.FractionRER * Day; // Fraction of RER to feed on this day
            double rER = 70 * Math.Pow(data.BodyWeight, 0.75) * dayMultiplier; // Resting energy requirement (kcal)
            double foodPerDay = rER / data.KcalPerG; // Total food per day (g)
            ContainersPerDay = foodPerDay / data.DietNetWeight; // Estimated containers of food used up per day
            double dietWaterVolume = foodPerDay * (data.DietWaterPercentage / 100); // Volume of water contained in food (ml)
            MaxVolumePerMeal = data.BodyWeight * 20; // Max volume to be administered per meal (ml)
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
            WaterPerMeal = (waterPerDay / MealsPerDay) - (data.FlushVolume * 2); // Extra water needed per meal (ml)

            if (WaterPerMeal < 0)
            {
                WaterPerMeal = 0; // Water per meal cannot be negative
            }

            switch (data.BodyWeight) // Round volumes per meal to more appropriate SF
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

            IReadOnlyCollection<double> feedingTimes = CalculateFeedingPlan(MealsPerDay);
            FormattedFeedingTimes = CreateFormattedListOfTimes(feedingTimes);
        }

        /*
         * Calculate the estimated daily fluid requirement based on species (true = cat, false = dog), and return the result.
         */
        private double CalculateWaterPerDay(double dietWaterVolume)
        {
            double waterPerDay;

            if (data.Species == true)
            {
                waterPerDay = 80 * Math.Pow(data.BodyWeight, 0.75);
            }
            else
            {
                waterPerDay = 132 * Math.Pow(data.BodyWeight, 0.75);
            }

            if (dietWaterVolume > 0)
            {
                waterPerDay -= dietWaterVolume;
            }

            return waterPerDay;
        }

        /*
         * Calculate the time interval between feeds.
         */
        public static double CalculateInterval(double mealsPerDay)
        {
            int hours = 15; // Number of hours to spread the feeds over

            double preciseInterval = hours / mealsPerDay;
            double interval = Math.Round(preciseInterval / 5, 1, MidpointRounding.AwayFromZero) * 5; // Round to the nearest 5 = to the nearest half hour

            if (interval < 1) // If interval less than one hour
            {
                interval = 1; // Set feeding interval to one hour (constraint: interval can never be less than an hour)
            }

            return interval;
        }

        /*
         * Calculate the first and last feeding times of the day from an initial assumed midpoint of 16:00 (4pm).
         */
        public static List<double> CalculateFeedingPlan(double mealsPerDay)
        {
            double interval = CalculateInterval(mealsPerDay);

            double preciseMealHalfTime = (mealsPerDay * interval) / 2; // Effectively half the total number of hours to adminster feeds over per day
            double mealHalfTime = Math.Round(preciseMealHalfTime / 5, 1, MidpointRounding.AwayFromZero) * 5; // Round to the nearest 5
            int midPoint = 16; // Corresponding to 16:00 or 4pm
            double startTime = midPoint - mealHalfTime; // Calculate the feeding schedule start time from its mid point
            double endTime = startTime + mealHalfTime; // Calculate the end time from the mid point

            while (endTime > 23.5) // While current end time is later than 23:30
            {
                midPoint -= 1; // Shift the mid point one hour earlier
                startTime = midPoint - mealHalfTime; // Recalculate the start time
                endTime = midPoint + mealHalfTime; // Recalculate the end time
            }

            double time = startTime; // Start from the calculated start time

            List<double> feedingTimes = []; // Initialise the output list

            if (interval > 1) // If the interval is longer than 1 hour
            {
                for (int i = 0; i < mealsPerDay; i++) // For each meal to be scheduled
                {
                    feedingTimes.Add(time); // Add this time to the list
                    time += interval; // Increment the time by the calculated interval
                }
            }
            else if (mealsPerDay > 23) // Otherwise, if the patient needs more than 23 meals per day
            {
                time = 0; // Set the time to midnight
                for (int i = 0; i < 24; i++) // For each meal (which will be every hour)
                {
                    feedingTimes.Add(time); // Add the current time to the list
                    time++; // Increment time by one hour
                }
            }
            else // Otherwise
            {
                for (int i = 0; i < mealsPerDay; i++) // For each meal to be scheduled
                {
                    feedingTimes.Add(time); // Add this time to the list
                    time++; // Increment the time by one hour
                }
            }

            return feedingTimes;
        }

        /*
         * Convert the calculated times into a human readable list of times
         */
        public static List<string> CreateFormattedListOfTimes(IReadOnlyCollection<double> list)
        {
            List<string> formattedList = []; // Initialise output list

            foreach (double time in list) // For each time in the calculated feeding schedule
            {
                int roundedTime = (int)Math.Round(time, 0, MidpointRounding.AwayFromZero); // Round the time to the nearest int
                string formattedTime; // Prepare a string field for the formatted (human readable) time

                string hours = time.ToString(); // Store the hour as a string
                string minutes = ":00"; // Set the minutes to 0

                if (time < roundedTime) // If the real time is less than the rounded time then it was rounded upwards, meaning minutes were 30+
                {
                    hours = Math.Round(time, 0, MidpointRounding.ToZero).ToString(); // So round the hour down
                    minutes = ":30"; // And set minutes to 30
                }

                if (time < 10) // If time is less than 10 then the hour is only one digit long
                {
                    hours = "0" + Math.Round(time, 0, MidpointRounding.ToZero).ToString(); // So add a zero before it
                }

                formattedTime = hours + minutes; // Combine the hours and minutes into one string

                formattedList.Add(formattedTime); // Add the human readable time to the output list
            }

            return formattedList;
        }
    }
}
