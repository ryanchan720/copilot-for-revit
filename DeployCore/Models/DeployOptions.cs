using System;

namespace RevitCopilot.Deploy.Models
{
    /// <summary>
    /// Options that modify deploy behavior.
    /// </summary>
    public class DeployOptions
    {
        /// <summary>
        /// If true, overwrite existing files in the target directory. Default: true.
        /// </summary>
        public bool Overwrite { get; set; } = true;

        /// <summary>
        /// Optional progress callback. Receives a human-readable status line.
        /// </summary>
        public IProgress<string> Progress { get; set; }
    }
}
