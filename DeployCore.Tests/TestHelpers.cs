using System.IO;
using RevitCopilot.Deploy;

namespace DeployCore.Tests;

/// <summary>
/// Helper to create fake Revit install directories matching the exact path patterns
/// used by DeployCoreService (which uses backslash-separated segments in Path.Combine).
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Creates a fake Revit.exe at the path DeployCoreService.DetectRevitVersions() would look.
    /// </summary>
    public static string CreateFakeRevitExe(string programFilesPath, int year)
    {
        // Must match the exact Path.Combine call in DeployCoreService
        var revitExe = Path.Combine(programFilesPath, $"Autodesk\\Revit {year}", DeployConstants.RevitExeName);
        Directory.CreateDirectory(Path.GetDirectoryName(revitExe));
        File.WriteAllText(revitExe, "");
        return revitExe;
    }

    /// <summary>
    /// Creates a fake .addin file at the path DeployCoreService would look.
    /// </summary>
    public static string CreateFakeAddinFile(string programDataPath, int year)
    {
        var addinFile = Path.Combine(programDataPath, DeployConstants.MachineAddinsRoot, year.ToString(), DeployConstants.AddinFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(addinFile));
        File.WriteAllText(addinFile, "fake addin content");
        return addinFile;
    }
}
