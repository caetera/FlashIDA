using System;
using System.Collections.Generic;
using System.Linq;
using Thermo.Interfaces.FusionAccess_V1.Control.Scans;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;
using log4net;

namespace Flash.IDA
{
    /// <summary>
    /// FLASHIda-enabled scan processor
    /// </summary>
    public class IDAScanProcessor : IScanProcessor
    {
        //loggers
        private ILog log;
        private ILog IDAlog;

        //active components
        private FLASHIdaWrapper flashIdaWrapper;
        private MethodParameters methodParams;
        private ScanFactory scanFactory;
        private ScanScheduler scanScheduler;

        /// <summary>
        /// Create an instance of the scan processor using <paramref name="parameters"/>, connected to existing <see cref="ScanFactory"/> <paramref name="factory"/>
        /// and <see cref="ScanScheduler"/> <paramref name="scheduler"/>
        /// </summary>
        /// <param name="parameters">Parameters for scan processor</param>
        /// <param name="factory">An instance of <see cref="scanFactory"/></param>
        /// <param name="scheduler">An instance of <see cref="scanScheduler"/></param>
        public IDAScanProcessor(MethodParameters parameters, ScanFactory factory, ScanScheduler scheduler)
        {
            //initialize loggers
            log = LogManager.GetLogger("General");
            IDAlog = LogManager.GetLogger("IDA");

            methodParams = parameters;
            scanScheduler = scheduler;
            scanFactory = factory;

            flashIdaWrapper = new FLASHIdaWrapper(methodParams.IDA);
        }

        /// <summary>
        /// Add new custom scan to a queue of scheduled scans,
        /// if the scan is not defined add the default ones
        /// </summary>
        /// <param name="scan">Definition of new custom scan <see cref="IFusionCustomScan"/></param>
        public void OutputMS(IFusionCustomScan scan)
        {
            if (scan != null)
            {
                scanScheduler.AddScan(scan, 2);
            }
            else
            {
                scanScheduler.AddDefault(); //add MS1 and AGC scans to the end of queue
            }
            
        }

        /// <summary>
        /// Process provided MSScan with FLASHIda
        /// </summary>
        /// <param name="msScan">An instance of scan object, as returned by the instrument API <see cref="IMsScan"/></param>
        /// <returns></returns>
        public IEnumerable<IFusionCustomScan> ProcessMS(IMsScan msScan)
        {
            List<IFusionCustomScan> scans = new List<IFusionCustomScan>();

            //for FTMS MS1 scans search for precursors
            if (msScan.Header["MSOrder"] == "1" && msScan.Header["MassAnalyzer"] == "FTMS")
            {
                //get ScanID for logging purposes
                msScan.Trailer.TryGetValue("Access ID", out var scanId);

                try
                {
                    List<PrecursorTarget> targets = flashIdaWrapper.GetIsolationWindows(msScan);

                    //logging of targets
                    IDAlog.Info(String.Format("MS1 Scan# {0} RT {1:f04} (Access ID {2}) - {3} targets",
                        msScan.Header["Scan"], msScan.Header["StartTime"], scanId, targets.Count));
                    if (targets.Count > 0) IDAlog.Debug(String.Join<PrecursorTarget>("\n", targets.ToArray()));

                    //schedule TopN fragmentation scans with highest qScore
                    foreach (PrecursorTarget precursor in targets.OrderByDescending(t => t.Score).Take(methodParams.TopN))
                    {
                        double center = precursor.Window.Center;
                        double isolation = precursor.Window.Width;
                        int z = precursor.Charge;

                        IFusionCustomScan repScan = scanFactory.CreateFusionCustomScan(
                            new ScanParameters
                            {
                                Analyzer = methodParams.MS2.Analyzer,
                                IsolationMode = methodParams.MS2.IsolationMode,
                                FirstMass = new double[] { methodParams.MS2.FirstMass },
                                LastMass = new double[] { Math.Min(z * center + 10, 2000) },
                                OrbitrapResolution = methodParams.MS2.OrbitrapResolution,
                                AGCTarget = methodParams.MS2.AGCTarget,
                                PrecursorMass = new double[] { center },
                                IsolationWidth = new double[] { isolation },
                                ActivationType = new string[] { methodParams.MS2.Activation },
                                CollisionEnergy = methodParams.MS2.CollisionEnergy != 0 ? new int[] { methodParams.MS2.CollisionEnergy } : null,
                                ScanType = "MSn",
                                Microscans = methodParams.MS2.Microscans,
                                ChargeStates = new int[] { Math.Min(z, 25) },
                                MaxIT = methodParams.MS2.MaxIT,
                                ReactionTime = methodParams.MS2.ReactionTime != 0 ? new double[] { methodParams.MS2.ReactionTime } : null,
                                ReagentMaxIT = methodParams.MS2.ReagentMaxIT != 0 ? new double[] { methodParams.MS2.ReagentMaxIT } : null,
                                ReagentAGCTarget = methodParams.MS2.ReagentAGCTarget != 0 ? new int[] { methodParams.MS2.ReagentAGCTarget } : null,
                                SrcRFLens = new double[] { methodParams.MS1.RFLens },
                                SourceCIDEnergy = methodParams.MS1.SourceCID,
                                DataType = methodParams.MS2.DataType
                            }, delay: 3);

                        scans.Add(repScan);

                        log.Debug(String.Format("ADD m/z {0:f04}/{1:f02} ({2}+) qScore: {3:f04} to Queue as #{4}",
                            center, isolation, z, precursor.Score, scanScheduler.customScans.Count + scans.Count));

                    }
                }
                catch (Exception ex)
                {
                    IDAlog.Error(String.Format("ProcessMS, if clause failed. {0}\n{1}", ex.Message, ex.StackTrace));
                }

                scans.Add(null); //will be replaced by default scan
            }

            return scans;
        }
    }
}
