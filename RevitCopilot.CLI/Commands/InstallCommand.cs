using System;
using System.Collections.Generic;
using System.Linq;
using RevitCopilot.Deploy;
using RevitCopilot.Deploy.Models;

namespace RevitCopilot.CLI.Commands
{
    /// <summary>
    /// install - Install Runtime files and optionally register Revit versions.
    ///
    /// Usage:
    ///   revit-copilot install --source &lt;path&gt; [--target &lt;path&gt;] [--revit-versions 2020,2021,2022]
    ///
    /// --source       (required) Path to Runtime package directory (contains Main.dll).
    /// --target       (optional) Override install target. Default: %ProgramData%\RevitCopilot\Runtime\
    /// --revit-versions  (optional) Comma-separated Revit years to register. Default: all detected.
    /// --no-overwrite    (optional) Skip existing files.
    /// </summary>
    internal static class InstallCommand
    {
        internal static int Run(string[] args)
        {
            string? sourcePath = null;
            string? targetPath = null;
            string? revitVersionsArg = null;
            bool overwrite = true;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--source":
                        if (i + 1 >= args.Length) { Output.Fail("--source requires a value."); return 1; }
                        sourcePath = args[++i]; break;
                    case "--target":
                        if (i + 1 >= args.Length) { Output.Fail("--target requires a value."); return 1; }
                        targetPath = args[++i]; break;
                    case "--revit-versions":
                        if (i + 1 >= args.Length) { Output.Fail("--revit-versions requires a value."); return 1; }
                        revitVersionsArg = args[++i]; break;
                    case "--no-overwrite":
                        overwrite = false; break;
                    case "--help" or "-h":
                        PrintHelp(); return 0;
                    default:
                        Output.Fail($"Unknown option: {args[i]}");
                        return 1;
                }
            }

            // Validate required args
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                Output.Fail("Missing required option: --source <path>");
                Output.Info("Run 'revit-copilot install --help' for usage.");
                return 1;
            }

            var deploy = new DeployCoreService();
            int exitCode = 0;

            // Step 1: Install Runtime
            Output.Header("Installing Runtime");
            Output.Info($"Source: {sourcePath}");
            Output.Info($"Target: {targetPath ?? "(default: %ProgramData%\\RevitCopilot\\Runtime\\)"}");
            Output.Info($"Overwrite: {overwrite}");
            Output.Blank();

            var progress = new Progress<string>(msg => Output.Detail(msg));
            var options = new DeployOptions { Overwrite = overwrite, Progress = progress };
            var result = deploy.InstallRuntime(sourcePath, targetPath, options);
            exitCode = Output.WriteResult(result);

            if (exitCode != 0)
                return exitCode;

            Output.Blank();

            // Step 2: Register Revit versions
            Output.Header("Registering Revit versions");
            Output.Blank();

            var detectedVersions = deploy.DetectRevitVersions().ToList();
            if (detectedVersions.Count == 0)
            {
                Output.Warn("No Revit versions detected on this system.");
                Output.Info("Runtime is installed, but no .addin files were generated.");
                Output.Info("You can manually register later with: revit-copilot install --source <path> --revit-versions 2024");
                return 0;
            }

            List<int> versionsToRegister;
            if (!string.IsNullOrWhiteSpace(revitVersionsArg))
            {
                // Parse user-specified versions
                versionsToRegister = new List<int>();
                foreach (var part in revitVersionsArg.Split(','))
                {
                    if (int.TryParse(part.Trim(), out int year))
                        versionsToRegister.Add(year);
                    else
                        Output.Warn($"Ignoring invalid version: {part.Trim()}");
                }
            }
            else
            {
                // Default: all detected
                versionsToRegister = detectedVersions.Select(v => v.VersionYear).ToList();
            }

            int registered = 0;
            int failed = 0;
            foreach (var year in versionsToRegister)
            {
                var regResult = deploy.RegisterRevitVersion(year, targetPath);
                if (regResult.Success)
                {
                    registered++;
                    Output.Ok(regResult.Message);
                }
                else
                {
                    failed++;
                    Output.Fail(regResult.Message);
                    exitCode = 1;
                }
            }

            Output.Blank();
            Output.Info($"Registered {registered} Revit version(s), {failed} failed.");
            return exitCode;
        }

        internal static void PrintHelp()
        {
            Console.WriteLine("Usage: revit-copilot install [options]");
            Console.WriteLine();
            Console.WriteLine("Install Runtime files and register with Revit versions.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --source <path>          (required) Runtime package directory containing Main.dll");
            Console.WriteLine("  --target <path>          Override install target path");
            Console.WriteLine("  --revit-versions <list>  Comma-separated Revit years (e.g. 2020,2021,2022)");
            Console.WriteLine("                          Default: all detected versions");
            Console.WriteLine("  --no-overwrite           Skip files that already exist");
            Console.WriteLine("  -h, --help               Show this help");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  revit-copilot install --source ./Runtime");
            Console.WriteLine("  revit-copilot install --source ./Runtime --revit-versions 2024");
            Console.WriteLine("  revit-copilot install --source ./Runtime --target C:\\Custom\\Runtime");
        }
    }
}
