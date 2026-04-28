namespace TubeFeedingWeb
{
    /*
     * Calculate food and water volumes for a specific day.
     */
    public class Volumes
    {
        public double ContainersPerDay { get; set; }
        public int MealsPerDay { get; set; }
        public double TotalVolumePerMeal { get; set; }
        public double FoodPerMeal { get; set; }
        public double WaterPerMeal { get; set; }
        public int Day { get; set; }
        public IReadOnlyCollection<string> FormattedFeedingTimes { get; set; }

        private double totalVolumePerDay;
        private double interval;
        private double mealHalfTime;
        private double midPoint;
        private double startTime;
        private double endTime;
        private readonly PatientDietData data;

        public Volumes(PatientDietData patientDietData, int day)
        {
            data = patientDietData;
            Day = day;
            FormattedFeedingTimes = [];
        }

        public void Calculate()
        {
            double maxVolumePerMeal = GetMaxVolumePerMeal();
            double dayMultiplier = data.FractionRER * Day; // Fraction of RER to feed on this day
            double rER = GetRER(dayMultiplier); // Resting energy requirement (kcal)
            double foodPerDay = rER / data.KcalPerG; // Total food per day (g)
            ContainersPerDay = foodPerDay / data.DietNetWeight; // Estimated containers of food used up per day
            double dietWaterVolume = foodPerDay * (data.DietWaterPercentage / 100); // Volume of water contained in food (ml)
            double waterPerDay = CalculateWaterPerDay() - dietWaterVolume; // Additional water needed per day (ml)
            totalVolumePerDay = foodPerDay + waterPerDay; // Estimated total volume administered per day (ml)
            MealsPerDay = (int)Math.Round(totalVolumePerDay / maxVolumePerMeal, 0, MidpointRounding.AwayFromZero); // Number of meals per day
            TotalVolumePerMeal = totalVolumePerDay / MealsPerDay; // Estimated total volume administered per meal (ml)

            while (TotalVolumePerMeal > maxVolumePerMeal) // While the volume administered per meal exceeds the max allowable volume
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

            FoodPerMeal = RoundDigits(FoodPerMeal);
            WaterPerMeal = RoundDigits(WaterPerMeal);

            ContainersPerDay = Math.Round(ContainersPerDay, 1, MidpointRounding.AwayFromZero);

            IReadOnlyCollection<double> feedingTimes = CalculateFeedingPlan();
            FormattedFeedingTimes = CreateFormattedListOfTimes(feedingTimes);
        }

        private static double RoundDigits(double valueToRound)
        {
            double roundedValue = valueToRound switch // Round volumes to more appropriate SF
            {
                < 10 => Math.Round(valueToRound, 2, MidpointRounding.AwayFromZero),
                < 20 => Math.Round(valueToRound, 1, MidpointRounding.AwayFromZero),
                _ => Math.Round(valueToRound, 0, MidpointRounding.AwayFromZero),
            };

            return roundedValue;
        }

        /*
         * If on re-feeding plan, start at 10ml/kg/meal, then 15ml/kg/meal once halfway to full RER,
         * then 20ml/kg/meal once on full RER.
         */
        private double GetMaxVolumePerMeal()
        {
            double maxVolumePerMeal;

            if (Day < data.Days * 0.5)
            {
                maxVolumePerMeal = data.BodyWeight * 10.0;
            }
            else if (Day < data.Days)
            {
                maxVolumePerMeal = data.BodyWeight * 15.0;
            }
            else
            {
                maxVolumePerMeal = data.BodyWeight * 20.0;
            }

            return maxVolumePerMeal;
        }

        /*
         * Get RER for current day of re-feeding plan.
         */
        private double GetRER(double dayMultiplier)
        {
            double rER;

            if (Day < data.Days)
            {
                rER = 70 * Math.Pow(data.BodyWeight, 0.75) * dayMultiplier;
            }
            else
            {
                rER = 70 * Math.Pow(data.BodyWeight, 0.75);
            }    

            return rER;
        }

        /*
         * Calculate the estimated daily fluid requirement based on species and return the result.
         */
        private double CalculateWaterPerDay()
        {
            double waterPerDay;

            if (data.Species == "Cat")
            {
                waterPerDay = 80 * Math.Pow(data.BodyWeight, 0.75);
            }
            else
            {
                waterPerDay = 132 * Math.Pow(data.BodyWeight, 0.75);
            }

            return waterPerDay;
        }

        /*
         * Calculate the time interval between feeds.
         */
        public void CalculateInterval()
        {
            int hours = 15; // Number of hours to spread the feeds over

            double preciseInterval = (double)hours / (double)MealsPerDay;
            interval = Math.Round(preciseInterval / 5, 1, MidpointRounding.AwayFromZero) * 5; // Round to the nearest 5 = to the nearest half hour

            if (interval < 1) // If interval less than one hour
            {
                interval = 1; // Set feeding interval to one hour (constraint: interval can never be less than an hour)
            }
        }

        /*
         * Calculate the first and last feeding times of the day.
         */
        private void CalculateTimeLine()
        {
            double feedingHours = (double)MealsPerDay * (double)interval; // The actual number of hours the feeds are spread over
            double preciseMealHalfTime = feedingHours / 2; // Half the total number of hours to adminster feeds over per day
            mealHalfTime = Math.Round(preciseMealHalfTime / 5, 1, MidpointRounding.AwayFromZero) * 5; // Round to the nearest 5
            startTime = midPoint - mealHalfTime; // Calculate the feeding schedule start time from its mid point
            endTime = startTime + feedingHours; // Calculate the end feeding time
        }

        /*
         * Calculate the feeding schedule.
         */
        public List<double> CalculateFeedingPlan()
        {
            midPoint = 15; // Corresponding to 16:00 (4pm)
            CalculateInterval();
            CalculateTimeLine();

            while (startTime > 8 && endTime < 22)
            {
                interval += 0.5;
                CalculateTimeLine();
            }

            if (endTime > 23) // While current end time is later than 23:30
            {
                startTime -= 23 - endTime;
                endTime = 23;
            }

            double time = startTime; // Start from the calculated start time

            List<double> feedingTimes = []; // Initialise the output list

            if (interval > 1) // If the interval is longer than 1 hour
            {
                for (int i = 0; i < MealsPerDay; i++) // For each meal to be scheduled
                {
                    feedingTimes.Add(time); // Add this time to the list
                    time += interval; // Increment the time by the calculated interval
                }
            }
            else if (MealsPerDay > 23) // Otherwise, if the patient needs more than 23 meals per day
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
                for (int i = 0; i < MealsPerDay; i++) // For each meal to be scheduled
                {
                    feedingTimes.Add(time); // Add this time to the list
                    time++; // Increment the time by one hour
                }
            }

            return feedingTimes;
        }

        /*
         * Make the calculated times human readable.
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
