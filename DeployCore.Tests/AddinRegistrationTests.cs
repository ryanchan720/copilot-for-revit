using System.IO;
using System.Xml.Linq;
using RevitCopilot.Deploy;
using RevitCopilot.Deploy.Models;

namespace DeployCore.Tests;

public class AddinRegistrationTests
{
    private static string CreateTempDir()
        => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"deploy_test_{Guid.NewGuid():N}")).FullName;

    private static void CleanupDir(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch { }
    }

    // ── RegisterRevitVersion: success ──────────────────────────────

    [Fact]
    public void RegisterRevitVersion_ValidSetup_CreatesAddinFile()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");
            var runtime = Path.Combine(tempRoot, "Runtime");

            TestHelpers.CreateFakeRevitExe(pf, 2024);
            Directory.CreateDirectory(runtime);
            File.WriteAllText(Path.Combine(runtime, "Main.dll"), "");

            var svc = new DeployCoreService(
                defaultRuntimePath: runtime,
                programFilesPath: pf,
                programDataPath: pd);

            var result = svc.RegisterRevitVersion(2024, runtime);

            Assert.True(result.Success, result.Message);
            var addinFile = Path.Combine(pd, DeployConstants.MachineAddinsRoot, "2024", DeployConstants.AddinFileName);
            Assert.True(File.Exists(addinFile), $"Addin file not found at {addinFile}");
            Assert.Contains(addinFile, result.Details);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void RegisterRevitVersion_AddinContent_IsValidXml()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");
            var runtime = Path.Combine(tempRoot, "Runtime");

            TestHelpers.CreateFakeRevitExe(pf, 2023);
            Directory.CreateDirectory(runtime);
            File.WriteAllText(Path.Combine(runtime, "Main.dll"), "");

            var svc = new DeployCoreService(
                defaultRuntimePath: runtime,
                programFilesPath: pf,
                programDataPath: pd);
            svc.RegisterRevitVersion(2023, runtime);

            var addinFile = Path.Combine(pd, DeployConstants.MachineAddinsRoot, "2023", DeployConstants.AddinFileName);
            var xml = XDocument.Load(addinFile);

            var addIn = xml.Root?.Element("AddIn");
            Assert.NotNull(addIn);
            Assert.Equal("Application", addIn.Attribute("Type")?.Value);
            Assert.Equal("RevitAddinPlatform", addIn.Element("Name")?.Value);
            Assert.Equal(DeployConstants.AddinClientId, addIn.Element("ClientId")?.Value);
            Assert.Equal(DeployConstants.AddinFullClassName, addIn.Element("FullClassName")?.Value);

            // Assembly should point to the Main.dll in runtime
            var assembly = addIn.Element("Assembly")?.Value;
            Assert.Contains("Main.dll", assembly);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void RegisterRevitVersion_OutOfRange_ReturnsError()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");
            var runtime = Path.Combine(tempRoot, "Runtime");

            Directory.CreateDirectory(runtime);
            File.WriteAllText(Path.Combine(runtime, "Main.dll"), "");

            var svc = new DeployCoreService(
                defaultRuntimePath: runtime,
                programFilesPath: pf,
                programDataPath: pd,
                minRevitVersion: 2019,
                maxRevitVersion: 2024);

            var result = svc.RegisterRevitVersion(2018, runtime);
            Assert.False(result.Success);
            Assert.Equal(DeployErrorCode.RevitVersionNotFound, result.ErrorCode);

            var result2 = svc.RegisterRevitVersion(2025, runtime);
            Assert.False(result2.Success);
            Assert.Equal(DeployErrorCode.RevitVersionNotFound, result2.ErrorCode);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void RegisterRevitVersion_RevitNotInstalled_ReturnsError()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");
            var runtime = Path.Combine(tempRoot, "Runtime");

            Directory.CreateDirectory(pf); // No Revit subdirectory
            Directory.CreateDirectory(runtime);
            File.WriteAllText(Path.Combine(runtime, "Main.dll"), "");

            var svc = new DeployCoreService(
                defaultRuntimePath: runtime,
                programFilesPath: pf,
                programDataPath: pd);

            var result = svc.RegisterRevitVersion(2024, runtime);
            Assert.False(result.Success);
            Assert.Equal(DeployErrorCode.RevitVersionNotFound, result.ErrorCode);
            Assert.Contains("not installed", result.Message);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void RegisterRevitVersion_RuntimeNotInstalled_ReturnsError()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");
            var runtime = Path.Combine(tempRoot, "Runtime");

            TestHelpers.CreateFakeRevitExe(pf, 2024);
            // No Main.dll in runtime

            var svc = new DeployCoreService(
                defaultRuntimePath: runtime,
                programFilesPath: pf,
                programDataPath: pd);

            var result = svc.RegisterRevitVersion(2024, runtime);
            Assert.False(result.Success);
            Assert.Equal(DeployErrorCode.InvalidSource, result.ErrorCode);
            Assert.Contains("Runtime not found", result.Message);
        }
        finally { CleanupDir(tempRoot); }
    }

    // ── UnregisterRevitVersion ─────────────────────────────────────

    [Fact]
    public void UnregisterRevitVersion_ExistingFile_DeletesAddinFile()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pd = Path.Combine(tempRoot, "PD");

            // Pre-create .addin file
            var addinFile = Path.Combine(pd, DeployConstants.MachineAddinsRoot, "2024", DeployConstants.AddinFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(addinFile));
            File.WriteAllText(addinFile, "old content");

            var svc = new DeployCoreService(programDataPath: pd);
            var result = svc.UnregisterRevitVersion(2024);

            Assert.True(result.Success, result.Message);
            Assert.False(File.Exists(addinFile));
            Assert.Contains("Removed", result.Message);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void UnregisterRevitVersion_NoExistingFile_ReturnsOk()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pd = Path.Combine(tempRoot, "PD");

            var svc = new DeployCoreService(programDataPath: pd);
            var result = svc.UnregisterRevitVersion(2024);

            Assert.True(result.Success);
            Assert.Contains("Nothing to do", result.Message);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void UnregisterRevitVersion_OutOfRange_ReturnsError()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pd = Path.Combine(tempRoot, "PD");

            var svc = new DeployCoreService(
                programDataPath: pd,
                minRevitVersion: 2019,
                maxRevitVersion: 2024);

            var result = svc.UnregisterRevitVersion(2018);
            Assert.False(result.Success);
            Assert.Equal(DeployErrorCode.RevitVersionNotFound, result.ErrorCode);
        }
        finally { CleanupDir(tempRoot); }
    }

    // ── Round-trip: Register then Unregister ───────────────────────

    [Fact]
    public void RegisterThenUnregister_FileIsCreatedAndRemoved()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");
            var runtime = Path.Combine(tempRoot, "Runtime");

            TestHelpers.CreateFakeRevitExe(pf, 2021);
            Directory.CreateDirectory(runtime);
            File.WriteAllText(Path.Combine(runtime, "Main.dll"), "");

            var svc = new DeployCoreService(
                defaultRuntimePath: runtime,
                programFilesPath: pf,
                programDataPath: pd);

            var addinFile = Path.Combine(pd, DeployConstants.MachineAddinsRoot, "2021", DeployConstants.AddinFileName);

            // Register
            var regResult = svc.RegisterRevitVersion(2021, runtime);
            Assert.True(regResult.Success, regResult.Message);
            Assert.True(File.Exists(addinFile));

            // Unregister
            var unregResult = svc.UnregisterRevitVersion(2021);
            Assert.True(unregResult.Success, unregResult.Message);
            Assert.False(File.Exists(addinFile));
        }
        finally { CleanupDir(tempRoot); }
    }
}
