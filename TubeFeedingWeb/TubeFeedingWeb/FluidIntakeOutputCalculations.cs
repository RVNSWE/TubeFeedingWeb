namespace TubeFeedingWeb
{
    public class FluidIntakeOutputCalculations
    {
        public double TotalIntake { get; set; }
        public double TotalOutput { get; set; }
        public double DifferenceMl { get; set; }
        public double DifferencePercent { get; set; }
        public double Comment { get; set; }

        private double waterFromFood;
        private double diarrhoeaOutput;
        private double maxDiarrhoeaOutput;
        private double vomitOutput;
        private FluidData data;

        public FluidIntakeOutputCalculations()
        {
            data = new();
        }

        public void Calculate()
        {
            waterFromFood = data.FoodMoistureContent / 100 * data.AmountEaten / data.MonitoringDuration;
            diarrhoeaOutput = 4 * data.BodyWeight * data.Diarrhoea / data.MonitoringDuration;
            if (maxDiarrhoeaOutput < diarrhoeaOutput) diarrhoeaOutput = maxDiarrhoeaOutput;
            vomitOutput = 4 * data.BodyWeight * data.Vomiting / data.MonitoringDuration;
            TotalIntake = (data.OralWater + data.OtherWater) / data.MonitoringDuration + waterFromFood + data.IVFT;
            TotalOutput = (data.Urine + data.OtherOutput) / data.MonitoringDuration + diarrhoeaOutput + vomitOutput;
            DifferenceMl = TotalIntake - TotalOutput;
            DifferencePercent = DifferenceMl / TotalIntake * 100;
        }

        public void UpdateData(FluidData data)
        {
            this.data = data;
            maxDiarrhoeaOutput = 200 * data.BodyWeight / 24;
        }
    }
}