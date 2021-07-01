using System.Collections.Generic;
using Thermo.Interfaces.FusionAccess_V1.Control.Scans;
using Thermo.Interfaces.InstrumentAccess_V1.MsScanContainer;

namespace Flash
{
    /// <summary>
    /// Classes that can process MS scans and create new scan requests should implement this interface
    /// </summary>
    public interface IScanProcessor
    {
        /// <summary>
        /// Process a single MS scan and return any number of scan requests
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        IEnumerable<IFusionCustomScan> ProcessMS(IMsScan arg);
        
        /// <summary>
        /// Submit scan request
        /// </summary>
        /// <param name="obj"></param>
        void OutputMS(IFusionCustomScan obj);
    }
}
