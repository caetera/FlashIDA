using System.Threading.Tasks.Dataflow;
using Thermo.Interfaces.FusionAccess_V1.Control.Scans;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;

namespace Flash
{
    /// <summary>
    /// Generic class allowing using different scan processors in multitreaded way, as a data processing pipeline
    /// </summary>
    public class DataPipe
    {
        private BufferBlock<IMsScan> inputScans; //input buffer
        private TransformManyBlock<IMsScan, IFusionCustomScan> scanAnalyzer; //translate every input into many outputs
        private ActionBlock<IFusionCustomScan> outputScans; //execute an action for each input

        //class encapsulating all method actions
        private IScanProcessor scanProcessor;

        /// <summary>
        /// Initialize the class using selected <see cref="IScanProcessor"/>
        /// </summary>
        /// <param name="processor">An class implementing scan processing</param>
        public DataPipe(IScanProcessor processor)
        {
            scanProcessor = processor;

            inputScans = new BufferBlock<IMsScan>();
            scanAnalyzer = new TransformManyBlock<IMsScan, IFusionCustomScan>(scanProcessor.ProcessMS);
            outputScans = new ActionBlock<IFusionCustomScan>(scanProcessor.OutputMS);

            inputScans.LinkTo(scanAnalyzer, new DataflowLinkOptions { PropagateCompletion = true }); //scans from inputScans will be fed to scanAnalyzer
            scanAnalyzer.LinkTo(outputScans, new DataflowLinkOptions { PropagateCompletion = true }); //output from scanAnalyzer will be fed to outputScans
        }

        /// <summary>
        /// Feed a single mass spectrum scan (<see cref="IMsScan"/> from API) to the data processing pipeline
        /// </summary>
        /// <param name="scan">Mass spectrum scan</param>
        public void Push(IMsScan scan)
        {
            inputScans.Post(scan);
        }
    }
}
