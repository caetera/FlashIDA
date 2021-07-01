using System;
using Thermo.Interfaces.SpectrumFormat_V1;

namespace Flash.DataObjects
{
    /// <summary>
    /// Simple class implementing `ICentroid` interface from Thermo IAPI
    /// </summary>
    /// <remarks>
    /// Used only for testing
    /// </remarks>
    public class Centroid : ICentroid
    {
        public bool? IsExceptional => false;

        public bool? IsReferenced => false;

        public bool? IsMerged => false;

        public bool? IsFragmented => false;

        public int? Charge { get; set; }

        public IMassIntensity[] Profile => null;

        public double? Resolution { get; set; }

        public int? ChargeEnvelopeIndex => -1;

        public bool? IsMonoisotopic => false;

        public bool? IsClusterTop => false;

        public double Mz { get; set; }

        public double Intensity { get; set; }

        /// <summary>
        /// Create centroid from provided parameters
        /// </summary>
        /// <param name="mz">m/z</param>
        /// <param name="intensity">Intensity</param>
        /// <param name="z">Charge</param>
        /// <param name="resolution">Resolution</param>
        public Centroid(double mz, double intensity, int z, double resolution)
        {
            Mz = mz;
            Intensity = intensity;
            Charge = z;
            Resolution = resolution;
        }

        /// <summary>
        /// String representaion
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0}\t{1}", Mz, Intensity);
        }
    }
}
