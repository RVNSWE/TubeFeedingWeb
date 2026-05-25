namespace TubeFeedingWeb
{
    public class WeightCalculations
    {
        public double WeightModifier { get; set; } = 1;
        public double WeeklyPercentWeightLost { get; set; }
        public double NextTargetWeight { get; set; }
        public double IdealWeight { get; set; }

        private double weightLost;
        private double percentWeightLost;
        private double onePercentWeightPerWeek;
        private WeightClinicData data;

        public WeightCalculations()
        {
            data = new();
        }

        public void Calculate()
        {
            weightLost = data.LastBodyWeight - data.CurrentBodyWeight;
            percentWeightLost = weightLost / data.LastBodyWeight * 100;
            WeeklyPercentWeightLost = percentWeightLost / data.WeeksSinceLastWeighed;
            onePercentWeightPerWeek = 0.01 * data.CurrentBodyWeight * data.WeeksUntilNextWeighed;
            NextTargetWeight = data.CurrentBodyWeight - onePercentWeightPerWeek;
            IdealWeight = data.CurrentBodyWeight / WeightModifier;

            WeeklyPercentWeightLost = Math.Round(WeeklyPercentWeightLost, 2, MidpointRounding.AwayFromZero);
            NextTargetWeight = Math.Round(NextTargetWeight, 2, MidpointRounding.AwayFromZero);
            IdealWeight = Math.Round(IdealWeight, 2, MidpointRounding.AwayFromZero);
        }

        public void UpdateData(WeightClinicData data)
        {
            this.data = data;

            WeightModifier = data.BCS switch
            {
                6 => 1.1,
                7 => 1.2,
                8 => 1.3,
                9 => 1.4,
                _ => 1
            };
        }
    }
}