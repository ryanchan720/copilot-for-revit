using System.Collections.Generic;

namespace RevitCopilot.Deploy.Models
{
    /// <summary>
    /// Standard result object for all deploy operations.
    /// Reusable by CLI, MSI custom actions, and PowerShell.
    /// </summary>
    public class DeployResult
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }
        public DeployErrorCode ErrorCode { get; private set; }
        public IReadOnlyList<string> Details { get; private set; }

        private DeployResult() { }

        public static DeployResult Ok(string message = null, params string[] details)
        {
            return new DeployResult
            {
                Success = true,
                Message = message,
                ErrorCode = DeployErrorCode.None,
                Details = details != null ? (IReadOnlyList<string>)new List<string>(details) : new List<string>()
            };
        }

        public static DeployResult Fail(DeployErrorCode code, string message, params string[] details)
        {
            return new DeployResult
            {
                Success = false,
                Message = message,
                ErrorCode = code,
                Details = details != null ? (IReadOnlyList<string>)new List<string>(details) : new List<string>()
            };
        }
    }
}
