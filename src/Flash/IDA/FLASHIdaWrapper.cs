using System;
using System.Collections.Generic;
using System.Linq;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;
using System.Runtime.InteropServices;
using Thermo.Interfaces.SpectrumFormat_V1;
using Flash.DataObjects;
using System.IO;
using log4net;
using log4net.Core;
using System.Xml.Linq;
using Thermo.TNG.Client.API.MsScanContainer;

namespace Flash.IDA
{
    /// <summary>
    /// Wrapper for FLASHIda C++ engine
    /// </summary>
    public class FLASHIdaWrapper : IDisposable
    {
        //loggers
        private static ILog log = LogManager.GetLogger("General");
        private static ILog IDAlog = LogManager.GetLogger("IDA");

        //binding for FlashIda engine
        const string dllName = "OpenMS.dll";
        [DllImport(dllName)]
        static private extern IntPtr CreateFLASHIda(string arg);

        [DllImport(dllName)]
        static private extern void DisposeFLASHIda(IntPtr pTestClassObject);

        [DllImport(dllName)]
        static private extern int GetPeakGroupSize(IntPtr pTestClassObjectdouble, double[] mzs, double[] ints, int length, double rt, int msLevel, string name);

        [DllImport(dllName)]
        static private extern int GetAllPeakGroupSize(IntPtr pTestClassObjectdouble);

        [DllImport(dllName)]
        static private extern void GetAllMonoisotopicMasses(IntPtr pTestClassObjectdouble, double[] monoMasses, int length);


        [DllImport(dllName)]
        static private extern void GetIsolationWindows(IntPtr pTestClassObjectdouble, double[] wstart, double[] wend, 
            double[] qScores, int[] charges, int[] min_charges, int[] max_charges, double[] monoMasses, double[] chargeCos, double[] chargeSnrs,
                           double[] isoCos,
                           double[] snrs, double[] chargeScores,
                           double[] ppmErrors, double[] precursorIntensities, double[] peakgroupIntensities
            );

        [DllImport(dllName)]
        static private extern void TestCode(IntPtr pTestClassObjectdouble, int[] arg, int length);

        private IntPtr m_pNativeObject;

        /// <summary>
        /// Construct wrapping object
        /// </summary>
        /// <param name="param">FLASHIda parameters</param>
        /// <param name="log">Path for additional logging from the C++ side (optional)</param>
        public FLASHIdaWrapper(IDAParameters param)
        {
            string arg = param.ToFLASHDeconvInput();
            Console.WriteLine(arg);
            m_pNativeObject = CreateFLASHIda(arg);
        }

        /// <summary>
        /// Destroy wrapping object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Default destructor
        /// </summary>
        ~FLASHIdaWrapper()
        {
            Dispose(false);
        }

        /// <summary>
        /// Destroy wrapping object
        /// </summary>
        /// <param name="bDisposing">Do not call the finalizer</param>
        protected virtual void Dispose(bool bDisposing)
        {
            if (m_pNativeObject != IntPtr.Zero)
            {
                // Call the DLL Export to dispose this class
                DisposeFLASHIda(m_pNativeObject);
                m_pNativeObject = IntPtr.Zero;
            }

            if (bDisposing)
            {
                // No need to call the finalizer since we've now cleaned
                // up the unmanaged memory
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Obtain the the list of targets for fragmentation from the current spectrum
        /// The spectral data has to be converted to array format (see below)
        /// </summary>
        /// <remarks>
        /// Internal function <see cref="GetIsolationWindows(IMsScan)"/> provides higher level interface
        /// </remarks>
        /// <param name="mzs">Array of m/z values</param>
        /// <param name="ints">Array of intensity values</param>
        /// <param name="rt">Retention time</param>
        /// <param name="msLevel">MS level as integer, i.e. MS1 - 1, MS2 - 2, etc</param>
        /// <param name="name">Identifier of the spectrum</param>
        /// <returns></returns>
        protected List<PrecursorTarget> GetIsolationWindows(double[] mzs, double[] ints, double rt, int msLevel, string name)
        {
            int size = 0;
            try
            {
                size = GetPeakGroupSize(m_pNativeObject, mzs, ints, mzs.Length, rt, msLevel, name);                
            }
            catch (Exception idaException)
            {
                log.Error(String.Format("IDAWrapper.GetPeakGroupSize reported: {0}\n{1}", idaException.Message, idaException.StackTrace));
            }

            double[] wstart = new double[size];
            double[] wend = new double[size];
            double[] tqScores = new double[size];
            int[] tCharges = new int[size];
            int[] tMinCharges = new int[size];
            int[] tMaxCharges = new int[size];
            double[] tmonoMasses = new double[size];
            double[] tchargeCos = new double[size];
            double[] tchargeSnrs = new double[size];
            double[] tisoCos = new double[size];
            double[] tsnrs = new double[size];
            double[] tchargeScores = new double[size];
            double[] tppmErrors = new double[size];
            double[] tprecursorIntensities = new double[size];
            double[] tpeakgroupIntensities = new double[size];

            try
            {
                GetIsolationWindows(m_pNativeObject, wstart, wend, tqScores, tCharges, tMinCharges, tMaxCharges, tmonoMasses, tchargeCos,
                    tchargeSnrs, tisoCos, tsnrs, tchargeScores, tppmErrors,
                    tprecursorIntensities, tpeakgroupIntensities);
            }
            catch (Exception idaException)
            {
                log.Error(String.Format("IDAWrapper.GetIsolationWindows reported: {0}\n{1}", idaException.Message, idaException.StackTrace));
            }

            List<PrecursorTarget> result = new List<PrecursorTarget>(size); //convert raw output into a list of PrecursorTarget objects

            for (int i = 0; i < size; i++)
            {
                result.Add(new PrecursorTarget(wstart[i], wend[i], tCharges[i], tMinCharges[i], tMaxCharges[i], tmonoMasses[i], tqScores[i], tprecursorIntensities[i],
                    tpeakgroupIntensities[i], tchargeCos[i], tchargeSnrs[i], tisoCos[i], tsnrs[i], tchargeScores[i], tppmErrors[i]));  
            }

            return result;
        }

        public List<double> GetAllMonoisotopicMasses()
        {
            try
            {
                int size = GetAllPeakGroupSize(m_pNativeObject);
                double[] masses= new double[size];
                GetAllMonoisotopicMasses(m_pNativeObject, masses, size);
                return masses.ToList();
            }
            catch (Exception idaException)
            {
                log.Error(String.Format("IDAWrapper.GetAllMonoisotopicMasses reported: {0}\n{1}", idaException.Message, idaException.StackTrace));
            }
            return null;
        }


        /// <summary>
        /// Obtain the the list of targets for fragmentation from the current spectrum.
        /// </summary>
        /// <param name="msScan">Mass spectrum object</param>
        /// <returns></returns>
        public List<PrecursorTarget> GetIsolationWindows(IMsScan msScan)
        {
            int msLevel = int.Parse(msScan.Header["MSOrder"]);
            double rt = double.Parse(msScan.Header["StartTime"]);
            string name = msScan.Header["Scan"];

            double[] mzs;
            double[] ints;

            //always send centroided scans
            mzs = msScan.Centroids.Select(c => c.Mz).ToArray();
            ints = msScan.Centroids.Select(c => c.Intensity).ToArray();
            
            return GetIsolationWindows(mzs, ints, rt, msLevel, name);
        }

        /// <summary>
        /// Calculate value G(<paramref name="x"/>) of Gaussian function (G) with height = <paramref name="intensity"/>, center = <paramref name="x0"/>,
        /// and standard deviation = <paramref name="sigma"/>
        /// </summary>
        /// <param name="x">Argument of Gaussian function</param>
        /// <param name="x0">Center of Gaussian function</param>
        /// <param name="intensity">Height of Gaussian function</param>
        /// <param name="sigma">Standard deviation of Gaussian function</param>
        /// <returns></returns>
        private static double Gauss(double x, double x0, double intensity, double sigma)
        {            
            return intensity * Math.Exp(-1 * Math.Pow(x - x0, 2)/(2 * Math.Pow(sigma,2)));
        }

        /// <summary>
        /// Return index of the first element in the <paramref name="sequence"/> larger or equal than
        /// <paramref name="value"/>
        /// </summary>
        /// <remarks>
        /// returned index is equal to the size of the sequence if all sequence elements are smaller than the value
        /// </remarks>
        /// <param name="sequence">Sequence of numbers</param>
        /// <param name="value">Value to search for</param>
        /// <returns></returns>
        private static int FirstIndexAfter(IList<double> sequence, double value)
        {
            for (int index = sequence.Count - 1; index >= 0; index--)
            {
                if(sequence[index] < value) return index + 1;
            }

            return 0;
        }

        /// <summary>
        /// Converts centroid representation of the spectrum to profile one, using Gaussian shapes for the peaks
        /// The resulting profile spectrum is written to an array of <see cref="MassIntensityPair"/> objects.
        /// </summary>
        /// <param name="centroids">Array of mass centroids</param>
        /// <param name="peakPoints">Number of points to use for each Gaussian shape</param>
        /// <param name="points">Returning array</param>
        /// <returns>Boolean indicator weither the conversion was successful</returns>
        public static bool ToProfile(IEnumerable<ICentroid> centroids, int peakPoints, out MassIntensityPair[] points)
        {
            //create lists with largest possible capacity from the start (performance optimization - no need for resizing)
            List<double> fullGrid = new List<double>(centroids.Count() * peakPoints);
            List<double> fullIntensity = new List<double>(fullGrid.Capacity);
            bool success = true; //indicate if errors happened

            foreach (ICentroid centroid in centroids)
            {
                if (centroid.Resolution == null)
                {
                    log.Warn(String.Format("Centroid {0} has no resolution - ignoring", centroid.Mz));
                    success = false;
                }
                else
                {
                    double mz = centroid.Mz;
                    double intensity = centroid.Intensity;
                    double resolution = centroid.Resolution.Value;

                    double width = 2 * mz / resolution; //2 FWHM on each side ~ 5 sigma of gauss
                    double sigma = width / (4 * Math.Sqrt(2 * Math.Log(2))); // FWHM = sqrt(2 * ln(2)) * sigma_gauss

                    int start = FirstIndexAfter(fullGrid, mz - width); //start index
                    int end = start + peakPoints; //stop index

                    for (int gridIndex = start; gridIndex < end; gridIndex++) //filling data
                    {
                        if (gridIndex < fullGrid.Count) //reuse grid point
                        {
                            fullIntensity[gridIndex] += Gauss(fullGrid[gridIndex], mz, intensity, sigma);
                        }
                        else //create new point
                        {
                            //1.0 is forcing devision result to double, peakPoints - 1 is the number of intervals
                            //end - 1 is due to zero based indices
                            fullGrid.Add(mz + width * (1 - (2 * (end - gridIndex - 1) / (peakPoints - 1.0))));
                            fullIntensity.Add(Gauss(fullGrid[gridIndex], mz, intensity, sigma));
                        }
                    }
                }
            }

            points = new MassIntensityPair[fullGrid.Count];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new MassIntensityPair(fullGrid[i], fullIntensity[i]);
            }

            return success;

        }

        /// <summary>
        /// Extra execution entry point
        /// </summary>
        /// <remarks>
        /// Used internally for testing
        /// </remarks>
        /// <param name="args">Command line arguments</param>
        static public void Main(string[] args)
        {
            string line;

            StreamReader file;
            StreamWriter wfile;

            //parse command args
            if (args.Length < 4)
            {
                Console.WriteLine("Too little parameters: provide input file, output file, maxMs2CountPerMs1 and qScoreThreshold");
                Environment.Exit(1);
            }

            try
            {
                file = new StreamReader(args[0]);
            }
            catch
            {
                Console.WriteLine("Cannot open input file: {0}", args[0]);
                Environment.Exit(1);
                return;
            }

            try
            {
                wfile = new StreamWriter(args[1]);
            }
            catch
            {
                Console.WriteLine("Cannot open output file: {0}", args[1]);
                Environment.Exit(1);
                return;
            }
            //create Wrapper
            var tolerances = new double[] { 10, 10 };
            var param = new IDAParameters(tolerances, int.Parse(args[2]), double.Parse(args[3]), 180, 4, 50, 500, 50000);
           
            try
            {
                MethodParameters methodParams = MethodParameters.Load(@"C:\Users\KyowonJeong\openms-development\FlashIDA\src\Flash\etc\method.xml");
                param = methodParams.IDA;
                //Console.WriteLine(methodParams.IDA.TargetLog);
            }
            catch (Exception ex)
            {
                log.Error(String.Format("Error loading method file: {0}\n{1}", ex.Message, ex.StackTrace));
                Environment.Exit(1);
            }

            FLASHIdaWrapper w;
           
            w = new FLASHIdaWrapper(param);
           
            // Read the file and display it line by line.  
            var mzs = new List<double>();
            var ints = new List<double>();
            var rt = .0;
            var msLevel = 1;
            var totalScore = .0;
            bool start = false;
            wfile.WriteLine("rt\tmz1\tmz2\tqScore\tcharges\tmonoMasses\tccos\tcsnr\tcos\tsnr\tcScore\tppm\tprecursorIntensity\tmassIntensity");
            while ((line = file.ReadLine()) != null)
            {
                var token = line.Split('\t');

                if (line.StartsWith(@"Spec") || (start && line.StartsWith(@"Running FLASHDeconv ... ")))
                {
                    //Console.WriteLine(line);
                    start = true;
                    if (mzs.Count > 0)
                    {
                        var l = w.GetIsolationWindows(mzs.ToArray(), ints.ToArray(), rt, msLevel, line);
                        /*List<double> monoMasses = w.GetAllMonoisotopicMasses();

                        Console.WriteLine(rt);
                        if (l.Count > 0) Console.WriteLine(String.Join<PrecursorTarget>("\n", l.ToArray()));
                        if (monoMasses.Count > 0)
                        {
                            Console.WriteLine(String.Format("AllMass={0}", String.Join<double>(" ", monoMasses.ToArray()))); ;
                        }*/

                        mzs.Clear();
                        ints.Clear();

                        foreach (var item in l)
                        {
                            wfile.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}\t{10}\t{11}\t{12}\t{13}", 
                                rt, item.Window.Start, item.Window.End, item.Score, item.Charge, item.MonoMass, item.ChargeCos, item.ChargeSnr, item.IsoCos,
                                item.Snr, item.ChargeScore, item.PpmError,
                                item.PrecursorIntensity, item.PrecursorPeakGroupIntensity);
                            //   Console.WriteLine(item);
                            totalScore += item.Score;
                        }
                    }

                    rt = double.Parse(token[1])/60.0;

                    if (start && line.StartsWith(@"Running FLASHDeconv ... "))
                    {
                        break;
                    }
                }

                else if (start)
                {
                    mzs.Add(double.Parse(token[0]));
                    ints.Add(double.Parse(token[1]));
                }
            }
            Console.WriteLine(@"Total QScore (i.e., expected number of PrSM identification): {0}", totalScore);

            wfile.Close();
            file.Close();
            w.Dispose();
        }
        
    } 
}
