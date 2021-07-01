using System.IO;
using System.Xml.Serialization;
using Flash.IDA;

namespace Flash
{
    /// <summary>
    /// MS1 acquisition parameters
    /// </summary>
    /// <remarks>
    /// Naming of properties is alligned with Thermo instrument API and should not be changed
    /// </remarks>
    public struct MS1Parameters
    {
        public string Analyzer;
        public double FirstMass;
        public double LastMass;
        public int OrbitrapResolution;
        public int AGCTarget;
        public double MaxIT;
        public int Microscans;
        public string DataType;
        public double RFLens;
        public double SourceCID;
    }

    /// <summary>
    /// MS2 acquisition parameters
    /// </summary>
    /// <remarks>
    /// Naming of properties is alligned with Thermo instrument API and should not be changed
    /// </remarks>
    public struct MS2Parameters
    {
        public string Analyzer;
        public string IsolationMode;
        public double FirstMass;
        public int OrbitrapResolution;
        public int AGCTarget;
        public double MaxIT;
        public int Microscans;
        public string DataType;
        public string Activation;
        public double ReactionTime;
        public double ReagentMaxIT;
        public int ReagentAGCTarget;
        public int CollisionEnergy;
    }

    /// <summary>
    /// Complete set of aquisition parameters, includs MS1, MS2, FlashIDA, and some general ones
    /// </summary>
    public class MethodParameters
    {
        public int TopN;
        public double Duration;
        public MS1Parameters MS1;
        public MS2Parameters MS2;
        public IDAParameters IDA;

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodParameters()
        {
            MS1 = new MS1Parameters();
            MS2 = new MS2Parameters();
            IDA = new IDAParameters();
        }

        /// <summary>
        /// Serialize <see cref="MethodParameters"/> to an XML file on disk
        /// </summary>
        /// <param name="path">Path to write the result</param>
        public void Save(string path)
        {
            using (StreamWriter output = new StreamWriter(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MethodParameters));
                serializer.Serialize(output, this);
            }
        }

        /// <summary>
        /// Deserialize <see cref="MethodParameters"/> from an XML file on disk
        /// </summary>
        /// <param name="path">Path to read from</param>
        /// <returns></returns>
        public static MethodParameters Load(string path)
        {
            using (StreamReader input = new StreamReader(path))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MethodParameters));
                return (MethodParameters)serializer.Deserialize(input);
            }
        }
    }
}
