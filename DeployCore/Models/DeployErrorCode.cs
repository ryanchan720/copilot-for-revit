namespace RevitCopilot.Deploy.Models
{
    /// <summary>
    /// Error codes for deploy operations.
    /// </summary>
    public enum DeployErrorCode
    {
        None = 0,

        /// <summary>Source directory does not exist.</summary>
        SourceNotFound,

        /// <summary>Insufficient permissions to perform the operation.</summary>
        AccessDenied,

        /// <summary>Source directory does not contain expected runtime files.</summary>
        InvalidSource,

        /// <summary>Target path is not usable (invalid characters, too long, etc.).</summary>
        InvalidTargetPath,

        /// <summary>Failed to copy one or more files.</summary>
        FileCopyFailed,

        /// <summary>Failed to write .addin file.</summary>
        AddinWriteFailed,

        /// <summary>No supported Revit versions detected on the system.</summary>
        NoRevitVersionsDetected,

        /// <summary>Requested Revit version is not installed.</summary>
        RevitVersionNotFound,

        /// <summary>Unexpected internal error.</summary>
        InternalError
    }
}
