using System;
using System.Collections.Generic;
using System.IO;
using RevitCopilot.Deploy.Models;

namespace RevitCopilot.Deploy
{
    /// <summary>
    /// Default implementation of IDeployCore.
    /// Detects Revit versions, installs Runtime files, and manages .addin registration.
    /// </summary>
    public class DeployCoreService : IDeployCore
    {
        private readonly string _defaultRuntimePath;
        private readonly int _minVersion;
        private readonly int _maxVersion;
        private readonly string _programFilesPath;
        private readonly string _programDataPath;

        /// <summary>
        /// Create a new DeployCoreService.
        /// </summary>
        /// <param name="defaultRuntimePath">
        /// Override the default Runtime install path. If null, uses %ProgramData%\RevitCopilot\Runtime\.
        /// </param>
        /// <param name="minRevitVersion">Minimum supported Revit version year. Default: 2019.</param>
        /// <param name="maxRevitVersion">Maximum supported Revit version year. Default: 2024.</param>
        /// <param name="programFilesPath">
        /// Override Program Files path. If null, uses the real system path.
        /// Useful for testing without an installed Revit.
        /// </param>
        /// <param name="programDataPath">
        /// Override ProgramData path. If null, uses the real system path.
        /// Useful for testing .addin registration without writing to system directories.
        /// </param>
        public DeployCoreService(string defaultRuntimePath = null,
            int minRevitVersion = DeployConstants.MinRevitVersion,
            int maxRevitVersion = DeployConstants.MaxRevitVersion,
            string programFilesPath = null,
            string programDataPath = null)
        {
            _programDataPath = programDataPath ??
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            _defaultRuntimePath = defaultRuntimePath ??
                Path.Combine(_programDataPath, DeployConstants.DefaultInstallRoot);

            _minVersion = minRevitVersion;
            _maxVersion = maxRevitVersion;
            _programFilesPath = programFilesPath ??
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        /// <inheritdoc />
        public IReadOnlyList<RevitInstance> DetectRevitVersions()
        {
            var results = new List<RevitInstance>();

            for (int year = _minVersion; year <= _maxVersion; year++)
            {
                var revitExe = Path.Combine(_programFilesPath, $"Autodesk\\Revit {year}", DeployConstants.RevitExeName);
                if (!File.Exists(revitExe))
                    continue;

                var installPath = Path.GetDirectoryName(revitExe);
                var addinDir = Path.Combine(_programDataPath, DeployConstants.MachineAddinsRoot, year.ToString());
                var addinFile = Path.Combine(addinDir, DeployConstants.AddinFileName);

                results.Add(new RevitInstance
                {
                    VersionYear = year,
                    InstallPath = installPath,
                    AddinDirectory = addinDir,
                    RevitExePath = revitExe,
                    IsRegistered = File.Exists(addinFile)
                });
            }

            return results;
        }

        /// <inheritdoc />
        public DeployResult InstallRuntime(string sourcePath, string targetPath = null, DeployOptions options = null)
        {
            options = options ?? new DeployOptions();
            targetPath = targetPath ?? _defaultRuntimePath;

            // Validate source
            if (string.IsNullOrWhiteSpace(sourcePath))
                return DeployResult.Fail(DeployErrorCode.SourceNotFound, "Source path is required.");

            var resolvedSource = Path.GetFullPath(sourcePath);
            if (!Directory.Exists(resolvedSource))
                return DeployResult.Fail(DeployErrorCode.SourceNotFound,
                    $"Source directory not found: {resolvedSource}");

            // Validate that source contains at least Main.dll
            var mainDll = Path.Combine(resolvedSource, DeployConstants.MainAssemblyName);
            if (!File.Exists(mainDll))
                return DeployResult.Fail(DeployErrorCode.InvalidSource,
                    $"Source does not contain {DeployConstants.MainAssemblyName}. " +
                    $"Ensure the source is a valid Runtime package directory.",
                    resolvedSource);

            // Resolve target
            var resolvedTarget = Path.GetFullPath(targetPath);
            var details = new List<string>();

            try
            {
                Report(options.Progress, $"Creating target directory: {resolvedTarget}");
                Directory.CreateDirectory(resolvedTarget);

                // Copy all files (flat)
                foreach (var file in Directory.GetFiles(resolvedSource))
                {
                    var dest = Path.Combine(resolvedTarget, Path.GetFileName(file));
                    if (options.Overwrite || !File.Exists(dest))
                    {
                        File.Copy(file, dest, options.Overwrite);
                        details.Add($"Copied: {Path.GetFileName(file)}");
                    }
                    else
                    {
                        details.Add($"Skipped (exists): {Path.GetFileName(file)}");
                    }
                }

                // Copy subdirectories recursively
                foreach (var dir in Directory.GetDirectories(resolvedSource))
                {
                    var dirName = Path.GetFileName(dir);
                    var destDir = Path.Combine(resolvedTarget, dirName);
                    CopyDirectory(dir, destDir, options.Overwrite, details);
                }

                Report(options.Progress, $"Runtime installed to: {resolvedTarget}");
                return DeployResult.Ok(
                    $"Runtime installed successfully to {resolvedTarget}",
                    details.ToArray());
            }
            catch (UnauthorizedAccessException ex)
            {
                return DeployResult.Fail(DeployErrorCode.AccessDenied,
                    $"Access denied when writing to {resolvedTarget}. " +
                    "Ensure you have Administrator privileges.",
                    ex.Message);
            }
            catch (Exception ex)
            {
                return DeployResult.Fail(DeployErrorCode.FileCopyFailed,
                    $"Failed to install Runtime: {ex.Message}",
                    ex.StackTrace);
            }
        }

        /// <inheritdoc />
        public DeployResult RegisterRevitVersion(int revitVersionYear, string runtimePath = null)
        {
            runtimePath = runtimePath ?? _defaultRuntimePath;

            // Validate the requested version is in our supported range
            if (revitVersionYear < _minVersion || revitVersionYear > _maxVersion)
                return DeployResult.Fail(DeployErrorCode.RevitVersionNotFound,
                    $"Revit version {revitVersionYear} is outside the supported range ({_minVersion}-{_maxVersion}).");

            // Verify the Revit version is actually installed
            var revitExe = Path.Combine(_programFilesPath, $"Autodesk\\Revit {revitVersionYear}", DeployConstants.RevitExeName);
            if (!File.Exists(revitExe))
                return DeployResult.Fail(DeployErrorCode.RevitVersionNotFound,
                    $"Revit {revitVersionYear} is not installed. Cannot register.");

            // Verify Runtime is installed
            var mainDll = Path.Combine(runtimePath, DeployConstants.MainAssemblyName);
            if (!File.Exists(mainDll))
                return DeployResult.Fail(DeployErrorCode.InvalidSource,
                    $"Runtime not found at {runtimePath}. Install Runtime first.");

            // Write .addin file
            var addinDir = Path.Combine(_programDataPath, DeployConstants.MachineAddinsRoot, revitVersionYear.ToString());
            var addinFile = Path.Combine(addinDir, DeployConstants.AddinFileName);

            try
            {
                Directory.CreateDirectory(addinDir);

                var assemblyPath = Path.Combine(runtimePath, DeployConstants.MainAssemblyName);
                var content = string.Format(DeployConstants.AddinFileTemplate,
                    assemblyPath,
                    DeployConstants.AddinClientId,
                    DeployConstants.AddinFullClassName);

                File.WriteAllText(addinFile, content, System.Text.Encoding.UTF8);

                // Remove user-level .addin if it exists (clean up old installs)
                var userAddinFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Autodesk", "Revit", "Addins", revitVersionYear.ToString(),
                    DeployConstants.AddinFileName);
                if (File.Exists(userAddinFile))
                {
                    File.Delete(userAddinFile);
                }

                return DeployResult.Ok(
                    $"Registered Revit {revitVersionYear}. Addin file: {addinFile}",
                    addinFile);
            }
            catch (UnauthorizedAccessException ex)
            {
                return DeployResult.Fail(DeployErrorCode.AccessDenied,
                    $"Access denied when writing to {addinDir}. " +
                    "Ensure you have Administrator privileges.",
                    ex.Message);
            }
            catch (Exception ex)
            {
                return DeployResult.Fail(DeployErrorCode.AddinWriteFailed,
                    $"Failed to write .addin file: {ex.Message}",
                    ex.StackTrace);
            }
        }

        /// <inheritdoc />
        public DeployResult UnregisterRevitVersion(int revitVersionYear)
        {
            if (revitVersionYear < _minVersion || revitVersionYear > _maxVersion)
                return DeployResult.Fail(DeployErrorCode.RevitVersionNotFound,
                    $"Revit version {revitVersionYear} is outside the supported range ({_minVersion}-{_maxVersion}).");

            var addinDir = Path.Combine(_programDataPath, DeployConstants.MachineAddinsRoot, revitVersionYear.ToString());
            var addinFile = Path.Combine(addinDir, DeployConstants.AddinFileName);

            if (!File.Exists(addinFile))
                return DeployResult.Ok($"No .addin file found for Revit {revitVersionYear}. Nothing to do.");

            try
            {
                File.Delete(addinFile);
                return DeployResult.Ok($"Unregistered Revit {revitVersionYear}. Removed: {addinFile}");
            }
            catch (UnauthorizedAccessException ex)
            {
                return DeployResult.Fail(DeployErrorCode.AccessDenied,
                    $"Access denied when removing {addinFile}.",
                    ex.Message);
            }
            catch (Exception ex)
            {
                return DeployResult.Fail(DeployErrorCode.AddinWriteFailed,
                    $"Failed to remove .addin file: {ex.Message}",
                    ex.StackTrace);
            }
        }

        // ── Private helpers ──────────────────────────────────────────────

        private static void CopyDirectory(string sourceDir, string targetDir, bool overwrite, List<string> details)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var dest = Path.Combine(targetDir, Path.GetFileName(file));
                if (overwrite || !File.Exists(dest))
                {
                    File.Copy(file, dest, overwrite);
                }
            }
            details.Add($"Copied dir: {Path.GetFileName(sourceDir)}");

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(subDir, Path.Combine(targetDir, Path.GetFileName(subDir)), overwrite, details);
            }
        }

        private static void Report(IProgress<string> progress, string message)
        {
            progress?.Report(message);
        }
    }
}
