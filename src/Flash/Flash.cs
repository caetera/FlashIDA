using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using Thermo.TNG.Factory;
using Thermo.Interfaces.FusionAccess_V1;
using Thermo.Interfaces.InstrumentAccess_V1.Control.Acquisition;
using Thermo.Interfaces.InstrumentAccess_V1;
using Thermo.Interfaces.FusionAccess_V1.MsScanContainer;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;
using System.IO;
using Flash.IDA;
using System.Timers;
using Thermo.Interfaces.FusionAccess_V1.Control.Scans;
using Thermo.Interfaces.FusionAccess_V1.Control;
using log4net;
using log4net.Config;
using Mono.Options;

namespace Flash
{
    class Flash
    {
        //acquisition controller
        static IAcquisition acquisition;

        //instrument controller
        static IFusionControl control;

        //instrument scan control
        static IFusionScans scanControl;

        //scans that are ariving from the instrument
        static IFusionMsScanContainer  msscans;

        //switch indicating that we received custom scan control
        static bool inCustom = false;

        //switch indicating that we need to stop
        static bool stopRequest = false;

        //class to handle scan scheduling
        static ScanScheduler scanScheduler;

        //helper class to create scan objects
        static ScanFactory scanFactory;

        //flashIDA 
        static IDAScanProcessor flashIDAProcessor;

        //DataPipe
        static DataPipe dataPipe;

        //loggers
        static ILog log;

        //Method parameters
        static MethodParameters methodParams;

        //Duration timer
        static Timer duration;

        //Spectrum running number
        static int currentNumber;

        //assembly data
        static private string selfFileName = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
        static private string selfName = Assembly.GetExecutingAssembly().GetName().Name;
        static private string selfVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        static private string selfLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        //Command line option structue
        static CmdOptions cliArgs;
        
        /// <summary>
        /// Parsing command line prameters into <see cref="CmdOptions"/>
        /// </summary>
        /// <param name="cliArgs">Command line as received from OS</param>
        /// <returns></returns>
        static CmdOptions ParseCLI(string[] cliArgs)
        {
            bool showHelp = false;
            bool showVersion = false;
            CmdOptions args = new CmdOptions();

            OptionSet options = new OptionSet
            {
                { String.Format("Usage\n{0} [option arguments]\n" +
                                "All arguments are optional\nOptions:", selfFileName) },
                { "h|help", "Usage information", _ =>  showHelp = true },
                { "v|version", "Show version information", _ => showVersion = true },
                { "o|nocc", "Ignore contact closure. Default: false",  _ => args.OverrideCC = true },
                { "t|test", "Run in test mode without connection to the instrument. Default: false", _ => args.TestMode = true },
                { "m|method=", "Location of method file. Default: method.xml in the program folder", v => args.MethodPath = v },
                { "r|rawname=", "The name or path to raw file, that will be used to name the log files. If not specified timestamp will be used", v => args.Rename = v }
            };

            List<string> positionArgs = new List<string>();

            try
            {
                positionArgs = options.Parse(cliArgs);
            }
            catch (OptionException e)
            {
                Console.Error.WriteLine(String.Format("Error parsing command line:\n{0}", e.Message));
                options.WriteOptionDescriptions(Console.Out);
                Environment.Exit(1);
            }

            if (showHelp)
            {
                options.WriteOptionDescriptions(Console.Out);
                Environment.Exit(0);
            }

            if (showVersion)
            {
                Console.WriteLine("{0} Version {1}", selfName, selfVersion);
                Environment.Exit(0);
            }

            if (args.TestMode)
            {
                FLASHIdaWrapper.Main(positionArgs.ToArray());
                Environment.Exit(0);
            }

            if (args.MethodPath == null) //no method file provided
            {
                args.MethodPath = Path.Combine(selfLocation, "method.xml");
            }

            if (!File.Exists(args.MethodPath))
            {
                if (File.Exists(Path.Combine(selfLocation + args.MethodPath))) //in case user provided relative path to method file
                {
                    args.MethodPath = Path.Combine(selfLocation, args.MethodPath);
                }
                else
                {
                    Console.Error.WriteLine(String.Format("Cannot find method file {0}", args.MethodPath));
                    Environment.Exit(1);
                }
            }
            
            return args;
        }

        static void Main(string[] args)
        {
            cliArgs = ParseCLI(args);

            XmlDocument appConfig = new XmlDocument();
            appConfig.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile); //logger configuration is stored in the {App}.config

            if (cliArgs.Rename != null)//replace appender file names in logger configuration, if the rename key was provided
            {
                string suffix = CheckLogPath(cliArgs.Rename);

                appConfig.SelectSingleNode("//log4net/appender[@name='GeneralFile']/file").Attributes.GetNamedItem("value").Value = String.Format("FlashLog_{0}.log", suffix);
                appConfig.SelectSingleNode("//log4net/appender[@name='IDAFile']/file").Attributes.GetNamedItem("value").Value = String.Format("IDALog_{0}.log", suffix);
            }

            XmlConfigurator.Configure((XmlElement) appConfig.GetElementsByTagName("log4net").Item(0));
            log = LogManager.GetLogger("General");

            try
            {
                //Create Access Container
                IFusionInstrumentAccessContainer instrumentContainer = Factory<IFusionInstrumentAccessContainer>.Create();

                //Connect to the instrument
                instrumentContainer.StartOnlineAccess();

                //hook up to the connection signal
                instrumentContainer.ServiceConnectionChanged += InstrumentConnected;
            }
            catch (Exception ex)
            {
                log.Error(String.Format("Cannot create Instrument Container. Do you have Tune installed? Do you run it on the Instrument Computer?\n{0}\n{1}",
                    ex.Message, ex.StackTrace));
                Environment.Exit(1);
            }

            //infinite loop - waiting for other signals - should have been done better
            while (!stopRequest)
            {
                
            }

            log.Info("Exiting");
        }

        /// <summary>
        /// "Instrument-is-connected"-event handler
        /// </summary>
        private static void InstrumentConnected(object sender, EventArgs e)
        {
            //sender is the instrument container
            IFusionInstrumentAccessContainer instrumentContainer = (IFusionInstrumentAccessContainer)sender;

            //Connect to the instrument accessor, IFAIK it should be always index 1
            IFusionInstrumentAccess instrumentAccess = instrumentContainer.Get(1);
            log.Info(String.Format("Instrument {0} ({1}) is connected", instrumentAccess.InstrumentName, instrumentAccess.InstrumentId));

            //fill controllers
            acquisition = instrumentAccess.Control.Acquisition;
            control = instrumentAccess.Control;

            //subscribe for Status Changes
            acquisition.StateChanged += OnStateChanged;

            //switch the acquisition on if necessary
            if (acquisition.State.SystemMode == SystemMode.Off || acquisition.State.SystemMode == SystemMode.Standby)
            {
                log.Info("Switching instrument on...");
                acquisition.SetMode(acquisition.CreateOnMode());
            }

            // handler for acqusition error events
            instrumentAccess.AcquisitionErrorsArrived += HandleAcqError;

            int numberOfMS = instrumentAccess.CountMsDetectors;
            log.Info(String.Format("Number of MS: {0}", numberOfMS));

            //it is unlikely there will be less than one MS detector, but for sanity we are checking for it
            if (numberOfMS > 0) msscans = instrumentAccess.GetMsScanContainer(0);

            //interface to schedule and create scans ('false' means cooperative access)
            try
            {
                scanControl = control.GetScans(false) as IFusionScans;
                log.Info("ScanControl success");
            }
            //NOTE: it is extremly important to catch all possible exceptions in the "instrument part", unhandled exception does not crash the software the usual way, but lead to weird behavior
            catch (Exception ex) 
            {
                log.Error(String.Format("ScanControl failed\n{0}\n{1}", ex.Message, ex.StackTrace));
            }
 
            //should fire when a custom scan is done (never fires as of current version of API), apparently fixed in API 3.5
            scanControl.CanAcceptNextCustomScan += CustomScanListner;

            //helper to have easier interface for scan creation
            scanFactory = new ScanFactory(scanControl);

            //load method
            try
            {
                methodParams = MethodParameters.Load(cliArgs.MethodPath);
                log.Info("Read method");
            }
            catch (Exception ex)
            {
                log.Error(String.Format("Error loading method file: {0}\n{1}", ex.Message, ex.StackTrace));
                Environment.Exit(1);
            }

            IFusionCustomScan agcScan = null;
            IFusionCustomScan defaultScan = null;

            try
            {
                //default AGC scan, scan parameters match the vendor implementation
                agcScan = scanFactory.CreateFusionCustomScan(
                    new ScanParameters
                    {
                        Analyzer = "IonTrap",
                        FirstMass = new double[] { methodParams.MS1.FirstMass },
                        LastMass = new double[] { methodParams.MS1.LastMass },
                        ScanRate = "Turbo",
                        AGCTarget = 30000,
                        MaxIT = 1,
                        Microscans = 1,
                        SrcRFLens = new double[] { methodParams.MS1.RFLens },
                        SourceCIDEnergy = methodParams.MS1.SourceCID,
                        DataType = "Profile",
                        ScanType = "Full"

                    }, id: 41, IsAGC: true, delay: 3); //41 is the magic scan identifier

                //default MS1 scan
                defaultScan = scanFactory.CreateFusionCustomScan(
                    new ScanParameters
                    {
                        Analyzer = methodParams.MS1.Analyzer,
                        FirstMass = new double[] { methodParams.MS1.FirstMass },
                        LastMass = new double[] { methodParams.MS1.LastMass },
                        OrbitrapResolution = methodParams.MS1.OrbitrapResolution,
                        AGCTarget = methodParams.MS1.AGCTarget,
                        MaxIT = methodParams.MS1.MaxIT,
                        Microscans = methodParams.MS1.Microscans,
                        SrcRFLens = new double[] { methodParams.MS1.RFLens },
                        SourceCIDEnergy = methodParams.MS1.SourceCID,
                        DataType = methodParams.MS1.DataType,
                        ScanType = "Full"
                    }, id: 42, delay: 3); 

                log.Info("Created default and AGC scans");
            }
            catch (Exception ex)
            {
                log.Error(String.Format("Cannot create default scans: {0}\n{1}", ex.Message, ex.StackTrace));
            }
            
            //create instance of custom scan queue and scheduler
            try
            {
                scanScheduler = new ScanScheduler(defaultScan, agcScan);
                log.Info("ScanScheduler created");
            }
            catch (Exception ex)
            {
                log.Error(String.Format("ScanScheduler failed: {0}\n{1}", ex.Message, ex.StackTrace));
            }

            //Initialize FLASHIDA Processor
            try
            {
                flashIDAProcessor = new IDAScanProcessor(methodParams, scanFactory, scanScheduler);
                log.Info("Created FLASHIDA processor");
            }
            catch (Exception ex)
            {
                log.Error(String.Format("IDAScanProcessor failed: {0}\n{1}", ex.Message, ex.StackTrace));
            }

            //Initialize data processing pipeline
            try
            {
                dataPipe = new DataPipe(flashIDAProcessor);
                log.Info("Created DataPipe");
            }
            catch (Exception ex)
            {
                log.Error(String.Format("DataPipe failed: {0}\n{1}", ex.Message, ex.StackTrace));
            }

            if (cliArgs.OverrideCC) //do not wait for contact closure event - start now
            {
                log.Info("Contact closure override");

                //subscribe for new scans from the instruments
                msscans.MsScanArrived += ProcessSpectrum;

                //start method
                duration = new Timer(methodParams.Duration * 60000); //Timer acepts milliseconds, but the duration is in minutes
                duration.Elapsed += StopExecution; //run StopExecution when the time is up
                duration.AutoReset = false;
                duration.Start();
                log.Info("Method started");

                //send the first custom scan (the magic one)
                try
                {
                    scanControl.SetFusionCustomScan(scanScheduler.getNextScan());
                    log.Info("Sent the first magic scan");
                }
                catch (Exception ex)
                {
                    log.Error(String.Format("First magic scan failed: {0}\n{1}", ex.Message, ex.StackTrace));
                }
            }
            else
            {
                //Subscribe for contact closure and wait with starting
                instrumentAccess.ContactClosureChanged += OnContactClosure;
                log.Info("Waiting for contact closure");
            }
        }

        /// <summary>
        /// Contact closure event handler
        /// </summary>
        private static void OnContactClosure(object sender, ContactClosureEventArgs e)
        {
            log.Info("Contact closure received");

            //unsubscribe from any future contact closure events
            IFusionInstrumentAccess instrumentAccess = (IFusionInstrumentAccess)sender;
            instrumentAccess.ContactClosureChanged -= OnContactClosure;
            
            //subscribe for new scans from the instruments
            msscans.MsScanArrived += ProcessSpectrum;

            //start method
            duration = new Timer(methodParams.Duration * 60000);
            duration.Elapsed += StopExecution;
            duration.AutoReset = false;
            duration.Start();
            log.Info("Method started");
            
            //send the first custom scan (the magic one)
            try
            {
                scanControl.SetFusionCustomScan(scanScheduler.getNextScan());
                log.Info("Sent the first magic scan");
            }
            catch (Exception ex)
            {
                log.Error(String.Format("First magic scan failed: {0}\n{1}", ex.Message, ex.StackTrace));
            }
        }

        /// <summary>
        /// Handler for CanAcceptNextCustomScan event
        /// </summary>
        /// <remarks>
        /// Never happens in the current version of API (3.4), might be fixed in 3.5
        /// </remarks>
        private static void CustomScanListner(object sender, EventArgs e)
        {
            log.Info("Got CanAcceptNextCustomScan");
        }

        /// <summary>
        /// Method to send custom scan request to instrument
        /// </summary>
        /// <param name="scan">Scan request</param>
        private static void SendCustomScan(IFusionCustomScan scan)
        {
            if (scan != null)
            {
                scan.RunningNumber = ++currentNumber;
                if (scan.Values["ScanType"] == "Full")
                {
                    log.Debug(String.Format("Sending Full {0} scan [{1} - {2}]; ID: {3}",
                        scan.Values["Analyzer"], scan.Values["FirstMass"], scan.Values["LastMass"], currentNumber));
                }
                //make sure not to ask for non-existing keys from scan.Values, the procedure will fail silently
                else if (scan.Values["ScanType"] == "MSn") //PrecursorMass and ChargeStates exist only for MSn scans, 
                {
                    log.Debug(String.Format("Sending MSn scan MZ = {0} Z = {1}; ID: {2}",
                        scan.Values["PrecursorMass"], scan.Values["ChargeStates"], currentNumber));
                }

                scanControl.SetFusionCustomScan(scan);
            }
            else
            {
                log.Debug("Sending NULL - Nothing to do");
            }
        }

        /// <summary>
        /// Handler of instrument status changes
        /// </summary>
        private static void OnStateChanged(object sender, StateChangedEventArgs e)
        {
            log.Info(String.Format("Instrument Status: {0}", acquisition.State.SystemMode.ToString()));
        }

        /// <summary>
        /// Processing routine for each scans received from the instrument
        /// Scan is contained in event arhs <paramref name="e"/>
        /// </summary>
        private static void ProcessSpectrum(object sender, MsScanEventArgs e)
        {
            IMsScan msScan = e.GetScan();

            //parse out API scan identifier
            msScan.Trailer.TryGetValue("Access ID", out var scanId);

            if (msScan.Header["MSOrder"] == "1")
            {
                log.Debug(String.Format("RECD {0} MS1 Scan #{1}; ID: {2}",
                    msScan.Header["MassAnalyzer"], msScan.Header["Scan"], scanId));
            }
            else if (msScan.Header["MSOrder"] == "2")
            {
                log.Debug(String.Format("RECD {0} MS2 Scan #{1}; ID: {2}; Precursor: {3:f04}",
                    msScan.Header["MassAnalyzer"], msScan.Header["Scan"], scanId, msScan.Header["PrecursorMass[0]"]));
            }

            //when magic scan received switch to custom control mode
            if (scanId == "41")
            {
                if (!inCustom) inCustom = true;
                currentNumber = 41;
            }
            
            //push current scan to the DataPipe and try sending next scheduled scan to the instrument
            if (inCustom)
            {
                dataPipe.Push(msScan);
                SendCustomScan(scanScheduler.getNextScan());
            }

            msScan.Dispose();//Release resources
        }

        /// <summary>
        /// Handler for acquisition errors
        /// </summary>
        /// <remarks>
        /// Most of these errors are purely technical, such as spray instability
        /// </remarks>
        private static void HandleAcqError(object sender, AcquisitionErrorsArrivedEventArgs e)
        {
            log.Error(String.Format("Aquisition Error: {0}", String.Join("; ", e.Errors.Select(err => err.Content)).Trim()));
        }

        /// <summary>
        /// Stop the acqusition
        /// </summary>
        private static void StopExecution(object sender, ElapsedEventArgs args)
        {
            stopRequest = true;
            log.Info("Time is over");
            duration.Close();
        }

        /// <summary>
        /// Returns still unused path for log files
        /// Preserve existing log files from being overwritten
        /// </summary>
        /// <remarks>
        /// Internal use
        /// </remarks>
        /// <param name="filepath">RawFile path</param>
        /// <returns></returns>
        private static string CheckLogPath(string filepath)
        {
            string suffix = Path.GetFileNameWithoutExtension(filepath);

            while (File.Exists(String.Format("FlashLog_{0}.log", suffix)) || File.Exists(String.Format("IDALog_{0}.log", suffix)))
                suffix = String.Format("{0}_{1}", suffix, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));

            return suffix;

        }
    }
}
