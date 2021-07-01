using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Thermo.Interfaces.FusionAccess_V1.Control.Scans;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;
using Thermo.TNG.Client.API.Control.Scans;

namespace Flash
{
    /// <summary>
    /// All available scan parameters from the API
    /// </summary>
    /// <remarks>
    /// Names for the parameters are matching API representation and should not be changed
    /// </remarks>
    public struct ScanParameters
    {
        public string Analyzer;
        public double[] FirstMass;
        public double[] LastMass;
        public int? OrbitrapResolution;
        public double[] IsolationWidth;
        public string IsolationMode;
        public string[] ActivationType;
        public int? AGCTarget;
        public double? MaxIT;
        public double[] PrecursorMass;
        public int[] CollisionEnergy;
        public string ScanType;
        public double? SourceCIDEnergy;
        public int? Microscans;
        public string DataType;
        public int[] ChargeStates;
        public string ScanRate;
        public string Polarity;
        public double[] SrcRFLens;
        public string IonisationMode;
        public double[] ActivationQ;
        public double[] ReactionTime;
        public double[] ReagentMaxIT;
        public int[] ReagentAGCTarget;
    }

    /// <summary>
    /// Helper-class to create scan requests for the instrument
    /// </summary>
    public class ScanFactory
    {
        private IFusionScans controler;

        /// <summary>
        /// Create an instance using provided <see cref="IFusionScans"/> for scan initialization by API
        /// </summary>
        /// <param name="scanControler">Scan controller</param>
        public ScanFactory(IFusionScans scanControler)
        {
            controler = scanControler;
        }

        /// <summary>
        /// Create a single custom scan request <seealso cref="ICustomScan"/>
        /// </summary>
        /// <param name="parameters">Scan parameters, such as analyzer, resolution, etc, <seealso cref="ScanParameters"/></param>
        /// <param name="id">Identifier for later refernce, it will be preserved in the scan returned by the instrument</param>
        /// <param name="delay">Processing delay - the time for the instrument to wait for any further custom scans requests after
        /// executing this request</param>
        /// <returns></returns>
        public ICustomScan CreateCustomScan(ScanParameters parameters, int id = 0, double delay = 0)
        {
            ICustomScan newScan = controler.CreateCustomScan();
            FillParameters(newScan, parameters);
            newScan.RunningNumber = id;
            newScan.SingleProcessingDelay = delay;

            return newScan;
        }

        /// <summary>
        /// Create a single custom scan request of tribrid format
        /// </summary>
        /// <remarks>
        /// This is extended version of <see cref="CreateCustomScan(ScanParameters, int, double)"/>
        /// </remarks>
        /// <param name="parameters">Scan parameters, such as analyzer, resolution, etc, <seealso cref="ScanParameters"/></param>
        /// <param name="id">Identifier for later refernce, it will be preserved in the scan returned by the instrument</param>
        /// <param name="delay">Processing delay - the time for the instrument to wait for any further custom scans requests after
        /// executing this request</param>
        /// <param name="IsAGC">Boolean indicator if this scan is an AGC scan, i.e. used for estimating current ion flux</param>
        /// <param name="AGCgroup">Identifier of the AGC group, AGC scan (i.e. the one with <paramref name="IsAGC"/> = true) will 
        /// be used for AGC of all the scans in the same group</param>
        /// <returns></returns>
        public IFusionCustomScan CreateFusionCustomScan(ScanParameters parameters, int id = 0, double delay = 0, bool IsAGC = false, int AGCgroup = 1)
        {
            IFusionCustomScan newScan = new FusionCustomScan();
            FillParameters(newScan, parameters);
            newScan.RunningNumber = id;
            newScan.SingleProcessingDelay = delay;
            newScan.IsPAGCScan = IsAGC;
            newScan.PAGCGroupIndex = AGCgroup;
            return newScan;
        }

        /// <summary>
        /// Create a repeating scan request <seealso cref="IRepeatingScan"/>
        /// </summary>
        /// <param name="parameters">Scan parameters, such as analyzer, resolution, etc, <seealso cref="ScanParameters"/></param>
        /// <param name="id">Identifier for later refernce, it will be preserved in all repeated scans returned by the instrument</param>
        /// <returns></returns>
        public IRepeatingScan CreateRepeatingScan(ScanParameters parameters, int id = 0)
        {
            IRepeatingScan newScan = controler.CreateRepeatingScan();
            FillParameters(newScan, parameters);
            newScan.RunningNumber = id;

            return newScan;
        }

        /// <summary>
        /// Updates parameters of a scan request template according to the provided <see cref="ScanParameters"/>
        /// </summary>
        /// <param name="scan">Scan template, as received from API</param>
        /// <param name="parameters">Scan parameters</param>
        private void FillParameters(IScanDefinition scan, ScanParameters parameters)
        {
            foreach (FieldInfo field in typeof(ScanParameters).GetFields())
            {
                if (field.GetValue(parameters) != null)
                    if (field.FieldType.IsArray)
                         scan.Values.Add(field.Name,
                             //This casts `object` to `object[]` and joins it into string
                             String.Join(";", (field.GetValue(parameters) as IEnumerable).Cast<object>().ToArray()));
                    else
                        scan.Values.Add(field.Name, field.GetValue(parameters).ToString());
            }
        }

        /// <summary>
        /// Text representation of a scan request object
        /// </summary>
        /// <param name="scan">Scan request</param>
        /// <returns></returns>
        public string ScanToString(ICustomScan scan)
        {
            string result = "";
            result += String.Join("\n", scan.Values.Select(e => String.Format("{0} = {1}", e.Key, e.Value)).ToArray());
            result += String.Format("\nID = {0}\nDelay = {1}", scan.RunningNumber, scan.SingleProcessingDelay);
            return result;
        }
    }
}
