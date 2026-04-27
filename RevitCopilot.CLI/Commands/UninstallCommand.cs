using System;
using System.Collections.Generic;
using System.IO;
using RevitCopilot.Deploy;
using RevitCopilot.Deploy.Models;

namespace RevitCopilot.CLI.Commands
{
    /// <summary>
    /// uninstall - Remove .addin registration for specific Revit version(s).
    ///
    /// Usage:
    ///   revit-copilot uninstall --revit-version 2024
    ///   revit-copilot uninstall --all
    ///
    /// Current scope: only removes .addin files (per-version).
    /// Runtime files are NOT removed (use manual deletion for now).
    /// Network config cleanup is NOT implemented yet.
    /// </summary>
    internal static class UninstallCommand
    {
        internal static int Run(string[] args)
        {
            int specificVersion = -1;
            bool uninstallAll = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--revit-version":
                        if (i + 1 >= args.Length) { Output.Fail("--revit-version requires a value."); return 1; }
                        if (int.TryParse(args[++i], out int year))
                            specificVersion = year;
                        else
                        {
                            Output.Fail($"Invalid version: {args[i]}. Expected a year (e.g. 2024).");
                            return 1;
                        }
                        break;
                    case "--all":
                        uninstallAll = true; break;
                    case "--help" or "-h":
                        PrintHelp(); return 0;
                    default:
                        Output.Fail($"Unknown option: {args[i]}");
                        return 1;
                }
            }

            if (specificVersion == -1 && !uninstallAll)
            {
                Output.Fail("Specify --revit-version <year> or --all.");
                Output.Info("Run 'revit-copilot uninstall --help' for usage.");
                return 1;
            }

            if (specificVersion != -1 && uninstallAll)
            {
                Output.Fail("Cannot use both --revit-version and --all.");
                return 1;
            }

            var deploy = new DeployCoreService();
            int exitCode = 0;

            Output.Header("Uninstalling Revit Copilot");
            Output.Blank();

            if (uninstallAll)
            {
                // Unregister all detected Revit versions
                var versions = deploy.DetectRevitVersions();
                if (versions.Count == 0)
                {
                    Output.Warn("No Revit versions detected. Nothing to uninstall.");
                    return 0;
                }

                int removed = 0;
                int skipped = 0;
                foreach (var revit in versions)
                {
                    var result = deploy.UnregisterRevitVersion(revit.VersionYear);
                    if (result.Success)
                    {
                        removed++;
                        if (result.Message.Contains("Nothing to do"))
                            skipped++;
                        else
                            Output.Ok(result.Message);
                    }
                    else
                    {
                        Output.Fail(result.Message);
                        exitCode = 1;
                    }
                }

                Output.Blank();
                Output.Info($"Processed {versions.Count} version(s): {removed} removed, {skipped} already clean.");
            }
            else
            {
                // Unregister specific version
                var result = deploy.UnregisterRevitVersion(specificVersion);
                exitCode = Output.WriteResult(result);
            }

            // Print boundary notice
            Output.Blank();
            Output.Info("Note: This command only removes .addin registration files.");
            Output.Info("The following are NOT removed in this version:");
            Output.Detail("  - Runtime files (%ProgramData%\\RevitCopilot\\Runtime\\)");
            Output.Detail("  - Network configuration (URL ACL, firewall rules)");
            Output.Detail("  - Plugin packages");
            Output.Info("To remove Runtime files manually: rmdir /s %ProgramData%\\RevitCopilot");

            return exitCode;
        }

        internal static void PrintHelp()
        {
            Console.WriteLine("Usage: revit-copilot uninstall [options]");
            Console.WriteLine();
            Console.WriteLine("Remove .addin registration for Revit Copilot.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --revit-version <year>  Remove .addin for a specific Revit version (e.g. 2024)");
            Console.WriteLine("  --all                  Remove .addin for all detected Revit versions");
            Console.WriteLine("  -h, --help             Show this help");
            Console.WriteLine();
            Console.WriteLine("Current scope:");
            Console.WriteLine("  This command ONLY removes .addin registration files.");
            Console.WriteLine("  Runtime files, network config, and plugins are NOT removed.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  revit-copilot uninstall --revit-version 2024");
            Console.WriteLine("  revit-copilot uninstall --all");
        }
    }
}
