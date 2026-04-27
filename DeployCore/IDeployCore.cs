using System.Collections.Generic;
using RevitCopilot.Deploy.Models;

namespace RevitCopilot.Deploy
{
    /// <summary>
    /// Core deployment interface for Revit Copilot.
    /// Covers runtime installation and Revit version registration.
    /// Plugin management and network configuration are added in later phases.
    /// </summary>
    public interface IDeployCore
    {
        /// <summary>
        /// Detect all installed Revit versions on the system.
        /// </summary>
        /// <returns>List of detected Revit installations, empty if none found.</returns>
        IReadOnlyList<RevitInstance> DetectRevitVersions();

        /// <summary>
        /// Install Runtime files from a local directory (or extracted archive) to the
        /// standard install location.
        /// </summary>
        /// <param name="sourcePath">
        /// Path to the directory containing Runtime files (Main.dll, SharedLibrary.dll, etc.).
        /// </param>
        /// <param name="targetPath">
        /// Override the default install path. If null, uses %ProgramData%\RevitCopilot\Runtime\.
        /// </param>
        /// <param name="options">Optional deploy options (overwrite, progress).</param>
        /// <returns>Deploy result with success status and details.</returns>
        DeployResult InstallRuntime(string sourcePath, string targetPath = null, DeployOptions options = null);

        /// <summary>
        /// Generate a .addin file for a specific Revit version, pointing to the installed Runtime.
        /// </summary>
        /// <param name="revitVersionYear">Revit version year (e.g. 2024).</param>
        /// <param name="runtimePath">
        /// Override the Runtime path used in the .addin file. If null, uses the default install path.
        /// </param>
        /// <returns>Deploy result with success status and details.</returns>
        DeployResult RegisterRevitVersion(int revitVersionYear, string runtimePath = null);

        /// <summary>
        /// Remove the .addin file for a specific Revit version.
        /// </summary>
        /// <param name="revitVersionYear">Revit version year (e.g. 2024).</param>
        /// <returns>Deploy result with success status and details.</returns>
        DeployResult UnregisterRevitVersion(int revitVersionYear);
    }
}
