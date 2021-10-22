using System;
using Thermo.Interfaces.SpectrumFormat_V1;

namespace Flash.DataObjects
{
    /// <summary>
    /// Point in m/z - intensity space
    /// </summary>
    public class MassIntensityPair : IMassIntensity, IComparable<MassIntensityPair>
    {
        public double Mz { get; set; }
        public double Intensity { get; set; }

        public MassIntensityPair(double mz, double i)
        {
            Mz = mz;
            Intensity = i;
        }

        /// <summary>
        /// Mass-intensity pairs are compared using only m/z value
        /// </summary>
        public int CompareTo(MassIntensityPair other)
        {
            return Mz.CompareTo(other.Mz);
        }

        public override string ToString()
        {
            return string.Format("{0}\t{1}", Mz, Intensity);
        }
    }
}
