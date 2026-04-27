using System;

namespace RevitCopilot.CLI
{
    /// <summary>
    /// Revit Copilot CLI - Minimal v1 entry point.
    ///
    /// Commands:
    ///   install   Install Runtime and register Revit versions
    ///   doctor    Run installation diagnostics
    ///   uninstall Remove .addin registration
    /// </summary>
    internal static class Program
    {
        internal static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                PrintMainHelp();
                return 0;
            }

            // Handle --version
            if (args[0] == "--version" || args[0] == "-v")
            {
                var version = typeof(Program).Assembly.GetName().Version;
                Console.WriteLine($"revit-copilot v{version?.ToString(3) ?? "0.1.0"}");
                return 0;
            }

            string command = args[0].ToLowerInvariant();
            string[] commandArgs = new string[args.Length - 1];
            Array.Copy(args, 1, commandArgs, 0, commandArgs.Length);

            switch (command)
            {
                case "install":
                    return Commands.InstallCommand.Run(commandArgs);
                case "doctor":
                    return Commands.DoctorCommand.Run(commandArgs);
                case "uninstall":
                    return Commands.UninstallCommand.Run(commandArgs);
                default:
                    Output.Fail($"Unknown command: {command}");
                    Console.WriteLine();
                    PrintMainHelp();
                    return 1;
            }
        }

        private static void PrintMainHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Revit Copilot CLI - Install and manage Revit Copilot Runtime");
            Console.WriteLine();
            Console.WriteLine("Usage: revit-copilot <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  install     Install Runtime files and register with Revit");
            Console.WriteLine("  doctor      Run installation diagnostics");
            Console.WriteLine("  uninstall   Remove .addin registration from Revit");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help      Show help for the specified command");
            Console.WriteLine("  -v, --version   Show CLI version");
            Console.WriteLine();
            Console.WriteLine("Run 'revit-copilot <command> --help' for command-specific help.");
            Console.WriteLine();
            Console.WriteLine("What this CLI supports (v1):");
            Console.WriteLine("  - Install Runtime from a local directory");
            Console.WriteLine("  - Auto-detect and register Revit versions (2019-2024)");
            Console.WriteLine("  - Remove .addin registration per version or all");
            Console.WriteLine("  - Basic diagnostics (doctor)");
            Console.WriteLine();
            Console.WriteLine("What this CLI does NOT support yet:");
            Console.WriteLine("  - Plugin management (install/uninstall/list plugins)");
            Console.WriteLine("  - Network configuration (URL ACL, firewall)");
            Console.WriteLine("  - Zip package support (requires pre-extracted directory)");
            Console.WriteLine("  - Runtime file removal (uninstall only removes .addin files)");
            Console.WriteLine("  - Upgrade / migration from older versions");
            Console.WriteLine();
        }
    }
}
