namespace Flash
{
    /// <summary>
    /// Commandline options
    /// </summary>
    public class CmdOptions
    {
        /// <summary>
        /// Override contacl closure signal, i.e. start without waiting for contact closure
        /// </summary>
        public bool OverrideCC { get; set; }

        /// <summary>
        /// Path to the method file
        /// </summary>
        public string MethodPath { get; set; }

        /// <summary>
        /// Enter the test mode
        /// </summary>
        public bool TestMode { get; set; }
        
        /// <summary>
        /// Template to rename the log files
        /// </summary>
        public string Rename { get; set; }
    }
}
