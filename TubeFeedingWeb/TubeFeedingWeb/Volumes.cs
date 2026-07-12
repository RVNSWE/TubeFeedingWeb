namespace TubeFeedingWeb
{
    /*
     * Calculate food and water volumes for a specific day.
     */
    public class Volumes
    {
        private const int MAX_ML_PER_KG = 10; // Hard coded for simplicity
        public double MaxVolumePerMeal { get; set; }
        public double RER { get; set; }
        public double FoodPerDay { get; set; }
        public double ContainersPerDay { get; set; }
        public double DietWaterVolume { get; set; }
        public double WaterPerDay { get; set; }
        public double TotalVolumePerDay { get; set; }
        public int MealsPerDay { get; set; }
        public double TotalVolumePerMeal { get; set; }
        public double FoodPerMeal { get; set; }
        public double WaterPerMeal { get; set; }
        public double WaterMlPerFoodG { get; set; }
        public bool SeparateWater { get; set; }
        public int Day { get; set; }
        public IReadOnlyCollection<string> FormattedFeedingTimes { get; set; }

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
            SeparateWater = data.SeparateWater == "Separate";
        }

        public void Calculate()
        {
            MaxVolumePerMeal = data.BodyWeight * (double)MAX_ML_PER_KG; // Get the maximum ml/kg per meal
            RER = GetRER(); // (kcal) Calculate the RER and return the kcal needed for today
            FoodPerDay = RER / data.KcalPerG; // (g) Get the total volume of food needed for today
            ContainersPerDay = FoodPerDay / data.DietNetWeight; // Calculate how many food containers / how much of a container will be used today
            DietWaterVolume = FoodPerDay * (data.DietWaterPercentage / 100); // (ml) Calculate the volume of water in the food
            WaterPerDay = CalculateWaterPerDay() - DietWaterVolume; // (ml) Calculate how much water is needed in addition to this
            TotalVolumePerDay = FoodPerDay + WaterPerDay; // (ml) Calculate the total combined volume of food and water to administer
            MealsPerDay = (int)Math.Round(TotalVolumePerDay / MaxVolumePerMeal, 0, MidpointRounding.AwayFromZero); // Calculate how many meals to split feeds over
            TotalVolumePerMeal = TotalVolumePerDay / MealsPerDay; // (ml) Calculate the total combined volume of food and water to administer per meal

            while (TotalVolumePerMeal > MaxVolumePerMeal) // While the volume administered per meal exceeds the maximum allowable volume
            {
                MealsPerDay++; // Add another meal per day
                TotalVolumePerMeal = TotalVolumePerDay / MealsPerDay; // Recalculate the volume per meal

                if (MealsPerDay > 23)
                {
                    break; // Do not exceed one meal per hour
                }
            }

            double totalFlushVolume = 2 * data.FlushVolume * MealsPerDay; // (ml) Calculate the total volume of water administered as flush with each meal
            WaterPerDay -= totalFlushVolume; // (ml) Calculate the volume of water needed today in addition to what is in the food and administered as flush

            if (WaterPerDay < 0)
            {
                WaterPerDay = 0; // Water per day cannot be negative
            }

            FoodPerMeal = FoodPerDay / MealsPerDay; // (g) Calculate how much food to administer per meal

            if(SeparateWater) // If administering food and water separately
            {
                WaterPerMeal = WaterPerDay / MealsPerDay; // (ml) Calculate how much water to administer per meal
            }
            else // If mixing water into food before drawing up meals
            {
                WaterMlPerFoodG = WaterPerDay / FoodPerDay; // (ml) Calculate the volume of water to mix in per g of food to meet total fluid requirements
                double waterAddedToFood = WaterMlPerFoodG * FoodPerMeal; // (ml) Calculate the volume of added water per meal
                FoodPerMeal += waterAddedToFood; // (ml) Calculate the amount of diluted food to draw up per meal
            }

            FoodPerMeal = RoundDecimalLowAccuracy(FoodPerMeal);
            WaterPerMeal = RoundDecimalLowAccuracy(WaterPerMeal);
            WaterMlPerFoodG = RoundDecimalHighAccuracy(WaterMlPerFoodG);

            ContainersPerDay = Math.Round(ContainersPerDay, 1, MidpointRounding.AwayFromZero);

            IReadOnlyCollection<double> feedingTimes = CalculateFeedingPlan();
            FormattedFeedingTimes = CreateFormattedListOfTimes(feedingTimes);
        }

        private double RoundDecimalLowAccuracy(double valueToRound)
        {
            double roundedValue = data.BodyWeight switch // Round volumes to more appropriate decimal
            {
                < 2 => Math.Round(valueToRound, 2, MidpointRounding.AwayFromZero),
                < 20 => Math.Round(valueToRound, 1, MidpointRounding.AwayFromZero),
                _ => Math.Round(valueToRound, 0, MidpointRounding.AwayFromZero),
            };

            return roundedValue;
        }

        private double RoundDecimalHighAccuracy(double valueToRound)
        {
            double roundedValue = data.BodyWeight switch // Round volumes to more appropriate decimal
            {
                < 2 => Math.Round(valueToRound, 2, MidpointRounding.AwayFromZero),
                _ => Math.Round(valueToRound, 1, MidpointRounding.AwayFromZero),
            };

            return roundedValue;
        }

        /*
         * Get RER for current day of re-feeding plan.
         */
        private double GetRER()
        {
            double rER;

            rER = (70 * Math.Pow(data.BodyWeight, 0.75)) / data.Days * Day;

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

        /*
         * Calculate maximum ml/kg per meal for current day of re-feeding plan if not the final day.
         * Half vol at first, then 3/4.
         */
        /*private void GetMaxMlPerKg()
        {
            if (Day < data.Days * 0.5)
            {
                MaxMlPerKg = data.MinMealSize;
            }
            else if (Day < data.Days)
            {
                double difference = data.MaxMealSize - data.MinMealSize;
                MaxMlPerKg = data.MinMealSize + (difference * 0.5);
            }
        }*/
    }
}
