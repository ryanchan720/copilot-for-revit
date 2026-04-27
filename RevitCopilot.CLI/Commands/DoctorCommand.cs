using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RevitCopilot.Deploy;
using RevitCopilot.Deploy.Models;

namespace RevitCopilot.CLI.Commands
{
    /// <summary>
    /// doctor - Output diagnostic information about the current installation.
    ///
    /// Usage:
    ///   revit-copilot doctor
    ///   revit-copilot doctor --json
    /// </summary>
    internal static class DoctorCommand
    {
        internal static int Run(string[] args)
        {
            bool jsonOutput = false;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--json":
                        jsonOutput = true; break;
                    case "--help" or "-h":
                        PrintHelp(); return 0;
                    default:
                        Output.Fail($"Unknown option: {args[i]}");
                        return 1;
                }
            }

            var deploy = new DeployCoreService();
            var defaultPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                DeployConstants.DefaultInstallRoot);

            var checks = new List<DoctorCheck>();

            // Check 1: Revit versions
            var revitVersions = deploy.DetectRevitVersions();
            checks.Add(new DoctorCheck(
                "Revit versions detected",
                revitVersions.Count > 0,
                revitVersions.Count > 0
                    ? string.Join(", ", revitVersions.Select(v => v.VersionYear.ToString()))
                    : "None found"));

            // Check 2: Runtime directory
            bool runtimeDirExists = Directory.Exists(defaultPath);
            checks.Add(new DoctorCheck(
                $"Runtime directory ({defaultPath})",
                runtimeDirExists,
                runtimeDirExists ? "Exists" : "Not found"));

            // Check 3: Main.dll
            string mainDllPath = Path.Combine(defaultPath, DeployConstants.MainAssemblyName);
            bool mainDllExists = File.Exists(mainDllPath);
            checks.Add(new DoctorCheck(
                $"Main.dll ({DeployConstants.MainAssemblyName})",
                mainDllExists,
                mainDllExists ? "Found" : "Missing"));

            // Check 4: .addin registration per detected version
            foreach (var revit in revitVersions)
            {
                string addinFile = Path.Combine(revit.AddinDirectory, DeployConstants.AddinFileName);
                bool addinExists = File.Exists(addinFile);
                checks.Add(new DoctorCheck(
                    $".addin file for Revit {revit.VersionYear}",
                    addinExists,
                    addinExists ? $"Registered ({addinFile})" : "Not registered"));
            }

            // Check 5: Current user write permission to ProgramData\RevitCopilot
            bool canWrite = false;
            string writeTestDir = Path.Combine(defaultPath, ".doctor-write-test");
            try
            {
                Directory.CreateDirectory(defaultPath);
                File.WriteAllText(writeTestDir, "test");
                File.Delete(writeTestDir);
                canWrite = true;
            }
            catch
            {
                canWrite = false;
            }
            checks.Add(new DoctorCheck(
                "Write permission to Runtime directory",
                canWrite,
                canWrite ? "Writable" : "No permission (run as Administrator)"));

            // Output
            if (jsonOutput)
            {
                WriteJsonOutput(checks, revitVersions, defaultPath);
                return 0;
            }

            // Human-readable output
            Console.WriteLine();
            Output.Header("Revit Copilot - Diagnostic Report");
            Console.WriteLine(new string('-', 50));
            Output.Blank();

            int passCount = 0;
            int failCount = 0;

            foreach (var check in checks)
            {
                if (check.Passed)
                {
                    Output.Ok(check.Name);
                    passCount++;
                }
                else
                {
                    Output.Fail(check.Name);
                    failCount++;
                }
                Output.Detail(check.Detail);
            }

            Output.Blank();
            Console.WriteLine(new string('-', 50));
            Output.Info($"Results: {passCount} passed, {failCount} failed out of {checks.Count} checks.");

            if (failCount > 0)
            {
                Output.Blank();
                Output.Info("Suggestions:");
                if (!runtimeDirExists || !mainDllExists)
                    Output.Detail("Run: revit-copilot install --source <runtime-directory>");
                if (!canWrite)
                    Output.Detail("Run this tool as Administrator for install/uninstall operations.");
                if (revitVersions.Count > 0 && checks.Exists(c => c.Name.Contains(".addin") && !c.Passed))
                    Output.Detail("Run: revit-copilot install --source <runtime-directory> to register .addin files.");
            }

            return failCount > 0 ? 1 : 0;
        }

        private static void WriteJsonOutput(
            List<DoctorCheck> checks,
            IReadOnlyList<RevitInstance> revitVersions,
            string runtimePath)
        {
            // Minimal JSON output for scripting
            Console.WriteLine("{");
            Console.WriteLine($"  \"runtimePath\": \"{EscapeJson(runtimePath)}\",");
            Console.WriteLine($"  \"revitVersions\": [{string.Join(", ", revitVersions.Select(v => $"\"{v.VersionYear}\""))}],");
            Console.WriteLine("  \"checks\": [");
            for (int i = 0; i < checks.Count; i++)
            {
                var c = checks[i];
                Console.WriteLine($"    {{\"name\": \"{EscapeJson(c.Name)}\", \"passed\": {(c.Passed ? "true" : "false")}, \"detail\": \"{EscapeJson(c.Detail)}\"}}{(i < checks.Count - 1 ? "," : "")}");
            }
            Console.WriteLine("  ]");
            Console.WriteLine("}");
        }

        private static string EscapeJson(string s)
        {
            return s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
        }

        internal static void PrintHelp()
        {
            Console.WriteLine("Usage: revit-copilot doctor [options]");
            Console.WriteLine();
            Console.WriteLine("Run diagnostics on the current Revit Copilot installation.");
            Console.WriteLine();
            Console.WriteLine("Checks:");
            Console.WriteLine("  - Detected Revit versions");
            Console.WriteLine("  - Runtime directory existence");
            Console.WriteLine("  - Main.dll presence");
            Console.WriteLine("  - .addin registration per Revit version");
            Console.WriteLine("  - Write permission to Runtime directory");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --json          Output in JSON format (for scripting)");
            Console.WriteLine("  -h, --help      Show this help");
        }

        private record DoctorCheck(string Name, bool Passed, string Detail);
    }
}
