using Flash.DataObjects;

namespace Flash.IDA
{
    /// <summary>
    /// Precursor for fragmentation
    /// </summary>
    /// /// <remarks>
    /// Majority of the parameters used only for debugging
    /// </remarks>
    public class PrecursorTarget
    {
        /// <summary>
        /// Isolation window
        /// </summary>
        public Range Window { get; set; }
        
        /// <summary>
        /// Quality score
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Monoisotopic mass
        /// </summary>
        public double MonoMass { get; set; }

        /// <summary>
        /// Charge
        /// </summary>
        public int Charge { get; set; }

        public int MinCharge { get; set; }

        public int MaxCharge { get; set; }
        
        public double PrecursorIntensity { get; set; }

        public double PrecursorPeakGroupIntensity { get; set; }

        public double ChargeCos { get; set; }

        public double ChargeSnr { get; set; }

        public double IsoCos { get; set; }

        public double Snr { get; set; }

        public double ChargeScore { get; set; }

        public double PpmError { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public PrecursorTarget()
        {
            //empty constructor, useful to fill after creation
        }


        /// <summary>
        /// Create Precursor from parameters returned by FLASHIda engine
        /// </summary>
        /// <param name="lowMz">Lower bound of isolation window</param>
        /// <param name="highMz">Upper bound of isolation window</param>
        /// <param name="z">Charge</param>
        /// <param name="min_z">Minimal charge</param>
        /// <param name="max_z">Maximal charge</param>
        /// <param name="mass">Monoisotopic mass</param>
        /// <param name="score">Quality score</param>
        /// <param name="precursorIntensity">Intensity of precursor</param>
        /// <param name="precursorPeakGroupIntensity">Intensity of precursor peak group</param>
        /// <param name="chargeCos">Charge cosine coefficient</param>
        /// <param name="chargeSnr">Charge signal-to-noise ratio</param>
        /// <param name="isoCos">Isotopic cosine coefficient</param>
        /// <param name="snr">Signal-to-noise ratio</param>
        /// <param name="chargeScore">Charge score</param>
        /// <param name="ppmError">Mass error in ppm</param>
        public PrecursorTarget(double lowMz, double highMz, int z, int min_z, int max_z, double mass, double score, double precursorIntensity, 
            double precursorPeakGroupIntensity, double chargeCos, double chargeSnr, double isoCos, double snr, double chargeScore, double ppmError)
        {
            Window = new Range(lowMz, highMz);
            Score = score;
            MonoMass = mass;
            Charge = z;
            MinCharge = min_z;
            MaxCharge = max_z;
            PrecursorIntensity = precursorIntensity;
            PrecursorPeakGroupIntensity = precursorPeakGroupIntensity;
            ChargeCos = chargeCos;
            ChargeSnr = chargeSnr;
            IsoCos = isoCos;
            Snr = snr;
            ChargeScore = chargeScore;
            PpmError = ppmError;
        }

        /// <summary>
        /// Convert precursor to string representation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Mass={0:f05}\tZ={1}\tScore={2:f05}\tWindow=[{3:f04}-{4:f04}]\tPrecursorIntensity={5}\tPrecursorMassIntensity={6}\tFeatures=[{7:f06},{8:f06},{9:f06},{10:f06},{11:f06},{12:f06}]\tChargeRange=[{13}-{14}]", 
                MonoMass, Charge, Score, Window.Start, Window.End, PrecursorIntensity, PrecursorPeakGroupIntensity, ChargeCos, ChargeSnr, IsoCos, Snr, ChargeScore, PpmError, MinCharge, MaxCharge);
        }
    }
}
