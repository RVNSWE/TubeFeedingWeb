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
        public double AdditionalWaterRequirement { get; set; }
        public double WaterPerDay { get; set; }
        public double TotalVolumePerDay { get; set; }
        public int MealsPerDay { get; set; }
        public double TotalVolumePerMeal { get; set; }
        public double FoodPerMeal { get; set; }
        public double WaterPerMeal { get; set; }
        public double WaterFoodDilutionRate { get; set; }
        public bool FoodDiluted { get; set; }
        public int Day { get; set; }
        public IReadOnlyCollection<string> FormattedFeedingTimes { get; set; }

        private double interval;
        private double mealHalfTime;
        private double midPoint;
        private double startTime;
        private readonly PatientDietData data;

        public Volumes(PatientDietData patientDietData, int day)
        {
            data = patientDietData;
            Day = day;
            FormattedFeedingTimes = [];
            FoodDiluted = data.FoodDilutedOrSeparate == "Diluted";
        }

        public void Calculate()
        {
            MaxVolumePerMeal = data.BodyWeight * (double)MAX_ML_PER_KG; // the maximum ml/kg per meal
            RER = (70 * Math.Pow(data.BodyWeight, 0.75)) / data.Days * Day; // (kcal) work out the kcals to feed today
            FoodPerDay = RER / data.KcalPerG; // (g) the total volume of food needed for today
            ContainersPerDay = FoodPerDay / data.DietNetWeight; // how many containers of food will be used up today
            DietWaterVolume = FoodPerDay * (data.DietWaterPercentage / 100); // (ml) the volume of water the food contains
            AdditionalWaterRequirement = CalculateBasicFluidRequirement() - DietWaterVolume; // (ml) the patient's remaining daily water requirement
            TotalVolumePerDay = FoodPerDay + AdditionalWaterRequirement; // (ml) the total volume to administer
            CalculateMealsPerDay(); // work out how many meals to split feeding into

            FoodPerMeal = FoodPerDay / MealsPerDay; // (g) how much food to administer per meal
            WaterPerMeal = WaterPerDay / MealsPerDay; // (ml) how much water to administer per meal

            if (FoodDiluted)
            {
                WaterFoodDilutionRate = RoundDecimalHighAccuracy(WaterPerDay / FoodPerDay); // the water:food ratio needed to meet total fluid requirements
                FoodPerMeal += WaterPerMeal; // (ml) the volume of diluted food to draw up per meal
            }

            FoodPerMeal = RoundDecimal(FoodPerMeal);
            WaterPerMeal = RoundDecimal(WaterPerMeal);
            ContainersPerDay = Math.Round(ContainersPerDay, 1, MidpointRounding.AwayFromZero);

            IReadOnlyCollection<double> feedingTimes = CalculateFeedingPlan(); // create the feeding schedule and return as an unformatted list of times
            FormattedFeedingTimes = CreateFormattedListOfTimes(feedingTimes); // format the times to be human readable
        }

        /*
         * Calculate the number of meals the full volume will need to be split into. 
         */
        private void CalculateMealsPerDay()
        {
            MealsPerDay = (int)Math.Round(TotalVolumePerDay / MaxVolumePerMeal, 0, MidpointRounding.AwayFromZero); // initial estimate
            AdjustTotalVolume(); // take into account volume of water used to flush the tube

            while (TotalVolumePerMeal > MaxVolumePerMeal)
            {
                MealsPerDay++; // add another meal per day
                AdjustTotalVolume(); // re-adjust for flush

                if (MealsPerDay > 23)
                {
                    break; // do not exceed one meal per hour
                }
            }
        }

        /*
         * Estimate how much water will be used as flush and separate this from the patient's daily fluid requirement, then re-calculate meals.
         */
        private void AdjustTotalVolume()
        {
            double totalFlushPerDay = 2 * data.FlushVolume * MealsPerDay;

            if (totalFlushPerDay > AdditionalWaterRequirement)
            {
                WaterPerDay = 0; // don't add any more water if flush already exceeds requirement (constraint: water per day can never be less than 0)
                TotalVolumePerDay = FoodPerDay + totalFlushPerDay; // (ml) food and flush are all that will be administered
            }
            else
            {
                WaterPerDay = AdditionalWaterRequirement - totalFlushPerDay; // otherwise subtract the volume of flush per day from the additional water requirement
                TotalVolumePerDay = FoodPerDay + AdditionalWaterRequirement; // (ml) total daily volume is food, flush and additional water
            }

            TotalVolumePerMeal = TotalVolumePerDay / MealsPerDay; // (ml) recalculate the volume per meal
        }

        private double RoundDecimal(double valueToRound)
        {
            double roundedValue = data.BodyWeight switch // round volumes to more appropriate decimal
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
         * Calculate the estimated daily fluid requirement based on species and return the result.
         */
        private double CalculateBasicFluidRequirement()
        {
            double basicFluidRequirement;

            if (data.Species == "Cat")
            {
                basicFluidRequirement = 80 * Math.Pow(data.BodyWeight, 0.75);
            }
            else
            {
                basicFluidRequirement = 132 * Math.Pow(data.BodyWeight, 0.75);
            }

            return basicFluidRequirement;
        }

        /*
         * Get the number of hours feeds have actually been spread over.
         */
        private double GetScheduleLength()
        {
            double scheduleLength = ((double)MealsPerDay - 1.0) * (double)interval; // The actual number of hours the feeds are spread over

            return scheduleLength;
        }

        /*
         * Calculate the time interval between feeds.
         */
        private void CalculateInterval()
        {
            double hours = 16; // Number of hours to try to spread the feeds over

            double preciseInterval = hours / (double)MealsPerDay;
            interval = Math.Round(preciseInterval / 5, 1, MidpointRounding.AwayFromZero) * 5; // Round to the nearest 5 = to the nearest half hour

            if (interval < 1) // If interval less than one hour
            {
                interval = 1; // Set feeding interval to one hour (constraint: can never be less than an hour)
            }
        }

        /*
         * Calculate the first and last feeding times of the day from a fixed midway point, to ensure start and finish times vary within reasonable
         * waking hours where possible.
         */
        private void CalculateTimeLine()
        {
            mealHalfTime = GetScheduleLength() / 2.0; // Half the total number of hours to adminster feeds over per day
            startTime = Math.Round((midPoint - mealHalfTime) / 5, 1, MidpointRounding.AwayFromZero) * 5; // Calculate the feeding schedule start time from its mid point
        }

        /*
         * Calculate the feeding schedule.
         */
        private List<double> CalculateFeedingPlan()
        {
            midPoint = 15; // Corresponding to 16:00 (4pm)
            CalculateInterval();
            CalculateTimeLine();

            /*while (GetScheduleLength() < 14)
            {
                interval += 0.5;
                CalculateTimeLine();
            }*/

            while (startTime + GetScheduleLength() > 23.5) // While current end time is later than 23:30
            {
                if (startTime > 0)
                {
                    startTime -= 0.5; // If possible, start feeds 30 minutes earlier (constraint: can't start before midnight)
                }
                else
                {
                    if (interval > 1) interval -= 0.5; // If possible, reduce the feeding interval (constraint: can never be less than an hour)
                    CalculateTimeLine();
                }

                if (interval < 1.5)
                {
                    interval = 1;
                    break;
                }
            }

            double time = startTime; // Start from the calculated start time

            List<double> feedingTimes = []; // Initialise the output list
            if (MealsPerDay > 23) // Otherwise, if the patient needs more than 23 meals per day
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
                    time += interval; // Increment the time by the calculated interval
                }
            }

            return feedingTimes;
        }

        /*
         * Make the calculated times human readable.
         */
        private static List<string> CreateFormattedListOfTimes(IReadOnlyCollection<double> list)
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
