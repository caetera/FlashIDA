using System;
using System.Collections.Concurrent;
using log4net;
using Thermo.Interfaces.FusionAccess_V1.Control.Scans;
using System.Linq;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Scans;

namespace Flash
{
    /// <summary>
    /// Helper class to handle scheduling custom scans request to the instrument
    /// </summary>
    public class ScanScheduler
    {
        /// <summary>
        /// Custom scan request queue
        /// </summary>
        public ConcurrentQueue<IFusionCustomScan> customScans { get; set; }
        
        //debug only
        private int MS1Count;
        private int MS2Count;
        private int AGCCount;

        private IFusionCustomScan defaultScan;
        private IFusionCustomScan agcScan;

        private ILog log;

        /// <summary>
        /// Create the instance using provided definitions of default scan <paramref name="scan"/> and default AGC scan <paramref name="AGCScan"/>
        /// Default scans will be submitted every time the queue is empty
        /// </summary>
        /// <param name="scan">API definition of a default "regular" scan</param>
        /// <param name="AGCScan">API definition of a default "regular" AGC scan</param>
        public ScanScheduler(IFusionCustomScan scan, IFusionCustomScan AGCScan)
        {
            defaultScan = scan;
            agcScan = AGCScan;
            customScans = new ConcurrentQueue<IFusionCustomScan>();
            log = LogManager.GetLogger("General");

            MS1Count = 0;
            MS2Count = 0;
            AGCCount = 0;
        }

        /// <summary>
        /// Adds single scan request to a queue
        /// </summary>
        /// <param name="scan">Scan to add</param>
        /// <param name="level">MS level of the scan (this parameter is used for internal "book-keeping")</param>
        public void AddScan(IFusionCustomScan scan, int level)
        {
            customScans.Enqueue(scan);
            switch (level)
            {
                case 0: AGCCount++; break;
                case 1: MS1Count++; break;
                case 2: MS2Count++; break;
                default:
                    log.Warn(String.Format("Level == {0}", level));
                    break;
            }
        }

        /// <summary>
        /// Add a default scan(s) to a queue
        /// </summary>
        public void AddDefault()
        {
            if (AGCCount < 2)
            {
                customScans.Enqueue(agcScan);
                log.Debug(String.Format("ADD default AGC scan as #{0}", customScans.Count));
                AGCCount++;
            }

            if (MS1Count < 2)
            {
                customScans.Enqueue(defaultScan);
                log.Debug(String.Format("ADD default MS1 scan as #{0}", customScans.Count));
                MS1Count++;
            }

            log.Debug(String.Format("QUEUE is [{0}]",
                String.Join(" - ", customScans.ToArray().Select(scan => PrintScanInfo(scan)).ToArray())));
        }

        /// <summary>
        /// Receive next scan from the queue
        /// </summary>
        /// <returns></returns>
        public IFusionCustomScan getNextScan()
        {
            log.Info(String.Format("Queue length: {0}", customScans.Count));

            if (customScans.IsEmpty)
            {
                log.Debug("Empty queue - gonna send AGC scan");
                customScans.Enqueue(defaultScan);
                MS1Count++;
                log.Debug(String.Format("ADD default MS1 scan as #{0}", customScans.Count));
                return agcScan;
            }
            else
            {
                customScans.TryDequeue(out var nextScan);
                if (nextScan != null)
                {
                    if (nextScan.Values["ScanType"] == "Full")
                    {
                        if (nextScan.Values["Analyzer"] == "IonTrap") AGCCount--;
                        else MS1Count--;

                        log.Debug(String.Format("POP Full {0} scan [{1} - {2}] // AGC: {3}, MS1: {4}, MS2: {5}", 
                            nextScan.Values["Analyzer"], nextScan.Values["FirstMass"], nextScan.Values["LastMass"],
                            AGCCount, MS1Count, MS2Count));
                    }
                    else if (nextScan.Values["ScanType"] == "MSn")
                    {
                        MS2Count--;
                        log.Debug(String.Format("POP MSn scan MZ = {0} Z = {1} // AGC: {2}, MS1: {3}, MS2: {4}",
                            nextScan.Values["PrecursorMass"], nextScan.Values["ChargeStates"],
                            AGCCount, MS1Count, MS2Count));
                    }
                    
                    return nextScan;
                }
                else
                {
                    log.Debug("Cannot receive next scan - gonna send AGC scan");
                    customScans.Enqueue(defaultScan);
                    MS1Count++;
                    log.Debug(String.Format("ADD default MS1 scan as #{0}", customScans.Count));
                    return agcScan;
                }
            }
        }

        /// <summary>
        /// Returns some basic scan parameters in a text form
        /// </summary>
        /// <remarks>
        /// Used for dubug logging
        /// </remarks>
        private string PrintScanInfo(IScanDefinition scan)
        {
            if (scan.Values["ScanType"] == "Full")
            {
                return String.Format("#{0} Full {1} [{2}:{3}]", scan.RunningNumber, scan.Values["Analyzer"], scan.Values["FirstMass"], scan.Values["LastMass"]);
            }
            else if (scan.Values["ScanType"] == "MSn")
            {
                return String.Format("#{0} MSn {1} [{2}, {3}+]", scan.RunningNumber, scan.Values["Analyzer"], scan.Values["PrecursorMass"], scan.Values["ChargeStates"]);
            }
            return "Unknown";
        }
    }
}
