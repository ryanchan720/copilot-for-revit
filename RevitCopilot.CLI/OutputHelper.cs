using System;

namespace RevitCopilot.CLI
{
    /// <summary>
    /// Shared output helpers for consistent CLI formatting.
    /// </summary>
    internal static class Output
    {
        internal static readonly string PrefixOk = "[OK]";
        internal static readonly string PrefixFail = "[FAIL]";
        internal static readonly string PrefixWarn = "[WARN]";
        internal static readonly string PrefixInfo = "[INFO]";

        internal static void Ok(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"{PrefixOk} {message}");
            Console.ResetColor();
        }

        internal static void Fail(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"{PrefixFail} {message}");
            Console.ResetColor();
        }

        internal static void Warn(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{PrefixWarn} {message}");
            Console.ResetColor();
        }

        internal static void Info(string message)
        {
            Console.WriteLine($"{PrefixInfo} {message}");
        }

        internal static void Blank()
        {
            Console.WriteLine();
        }

        internal static void Detail(string message)
        {
            Console.WriteLine($"      {message}");
        }

        internal static void Header(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  {title}");
            Console.ResetColor();
        }

        internal static int WriteResult(RevitCopilot.Deploy.Models.DeployResult result)
        {
            if (result.Success)
            {
                Ok(result.Message);
                if (result.Details != null)
                {
                    foreach (var d in result.Details)
                        Detail(d);
                }
                return 0;
            }

            Fail(result.Message);
            if (result.Details != null)
            {
                foreach (var d in result.Details)
                    Detail(d);
            }
            return MapErrorCode(result.ErrorCode);
        }

        internal static int MapErrorCode(RevitCopilot.Deploy.Models.DeployErrorCode code)
        {
            return code switch
            {
                RevitCopilot.Deploy.Models.DeployErrorCode.None => 0,
                RevitCopilot.Deploy.Models.DeployErrorCode.AccessDenied => 3,
                RevitCopilot.Deploy.Models.DeployErrorCode.SourceNotFound => 4,
                RevitCopilot.Deploy.Models.DeployErrorCode.InvalidSource => 5,
                RevitCopilot.Deploy.Models.DeployErrorCode.InvalidTargetPath => 6,
                RevitCopilot.Deploy.Models.DeployErrorCode.FileCopyFailed => 7,
                RevitCopilot.Deploy.Models.DeployErrorCode.AddinWriteFailed => 8,
                RevitCopilot.Deploy.Models.DeployErrorCode.NoRevitVersionsDetected => 9,
                RevitCopilot.Deploy.Models.DeployErrorCode.RevitVersionNotFound => 10,
                _ => 1
            };
        }

        internal static string ErrorCodeLabel(RevitCopilot.Deploy.Models.DeployErrorCode code)
        {
            return code switch
            {
                RevitCopilot.Deploy.Models.DeployErrorCode.None => "NONE",
                RevitCopilot.Deploy.Models.DeployErrorCode.AccessDenied => "ACCESS_DENIED",
                RevitCopilot.Deploy.Models.DeployErrorCode.SourceNotFound => "SOURCE_NOT_FOUND",
                RevitCopilot.Deploy.Models.DeployErrorCode.InvalidSource => "INVALID_SOURCE",
                RevitCopilot.Deploy.Models.DeployErrorCode.InvalidTargetPath => "INVALID_TARGET_PATH",
                RevitCopilot.Deploy.Models.DeployErrorCode.FileCopyFailed => "FILE_COPY_FAILED",
                RevitCopilot.Deploy.Models.DeployErrorCode.AddinWriteFailed => "ADDIN_WRITE_FAILED",
                RevitCopilot.Deploy.Models.DeployErrorCode.NoRevitVersionsDetected => "NO_REVIT_VERSIONS",
                RevitCopilot.Deploy.Models.DeployErrorCode.RevitVersionNotFound => "REVIT_VERSION_NOT_FOUND",
                RevitCopilot.Deploy.Models.DeployErrorCode.InternalError => "INTERNAL_ERROR",
                _ => $"UNKNOWN({(int)code})"
            };
        }
    }
}
