using System.IO;
using System.Linq;
using RevitCopilot.Deploy;
using RevitCopilot.Deploy.Models;

namespace DeployCore.Tests;

public class DetectRevitVersionsTests
{
    private static string CreateTempDir()
        => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"detect_test_{Guid.NewGuid():N}")).FullName;

    private static void CleanupDir(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch { }
    }

    [Fact]
    public void DetectRevitVersions_NoRevitInstalled_ReturnsEmptyList()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");
            Directory.CreateDirectory(pf);

            var svc = new DeployCoreService(
                programFilesPath: pf,
                programDataPath: pd,
                minRevitVersion: 2019,
                maxRevitVersion: 2024);

            var result = svc.DetectRevitVersions();

            Assert.Empty(result);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void DetectRevitVersions_SingleVersion_ReturnsOneInstance()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");

            var revitExe = TestHelpers.CreateFakeRevitExe(pf, 2024);

            var svc = new DeployCoreService(
                programFilesPath: pf,
                programDataPath: pd,
                minRevitVersion: 2019,
                maxRevitVersion: 2024);

            var result = svc.DetectRevitVersions();

            Assert.Single(result);
            Assert.Equal(2024, result[0].VersionYear);
            Assert.Contains("2024", result[0].InstallPath);
            Assert.Equal(revitExe, result[0].RevitExePath);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void DetectRevitVersions_MultipleVersions_ReturnsAll()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");

            foreach (var year in new[] { 2021, 2023, 2024 })
                TestHelpers.CreateFakeRevitExe(pf, year);

            var svc = new DeployCoreService(
                programFilesPath: pf,
                programDataPath: pd,
                minRevitVersion: 2019,
                maxRevitVersion: 2024);

            var result = svc.DetectRevitVersions();

            Assert.Equal(3, result.Count);
            Assert.Equal(new[] { 2021, 2023, 2024 }, result.Select(r => r.VersionYear).OrderBy(v => v));
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void DetectRevitVersions_OutOfRangeVersion_NotReturned()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");

            foreach (var year in new[] { 2018, 2025 })
                TestHelpers.CreateFakeRevitExe(pf, year);

            var svc = new DeployCoreService(
                programFilesPath: pf,
                programDataPath: pd,
                minRevitVersion: 2019,
                maxRevitVersion: 2024);

            var result = svc.DetectRevitVersions();

            Assert.Empty(result);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void DetectRevitVersions_AddinFileExists_IsRegisteredTrue()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");

            TestHelpers.CreateFakeRevitExe(pf, 2022);
            TestHelpers.CreateFakeAddinFile(pd, 2022);

            var svc = new DeployCoreService(
                programFilesPath: pf,
                programDataPath: pd,
                minRevitVersion: 2019,
                maxRevitVersion: 2024);

            var result = svc.DetectRevitVersions();

            Assert.Single(result);
            Assert.True(result[0].IsRegistered);
            Assert.Contains("2022", result[0].AddinDirectory);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void DetectRevitVersions_NoAddinFile_IsRegisteredFalse()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");

            TestHelpers.CreateFakeRevitExe(pf, 2020);

            var svc = new DeployCoreService(
                programFilesPath: pf,
                programDataPath: pd,
                minRevitVersion: 2019,
                maxRevitVersion: 2024);

            var result = svc.DetectRevitVersions();

            Assert.Single(result);
            Assert.False(result[0].IsRegistered);
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void DetectRevitVersions_CustomVersionRange_RespectsBounds()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");

            foreach (var year in new[] { 2019, 2020, 2021, 2022 })
                TestHelpers.CreateFakeRevitExe(pf, year);

            // Scan only 2020-2021
            var svc = new DeployCoreService(
                programFilesPath: pf,
                programDataPath: pd,
                minRevitVersion: 2020,
                maxRevitVersion: 2021);

            var result = svc.DetectRevitVersions();

            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { 2020, 2021 }, result.Select(r => r.VersionYear).OrderBy(v => v));
        }
        finally { CleanupDir(tempRoot); }
    }

    [Fact]
    public void DetectRevitVersions_DirectoryExistsButNoExe_Skipped()
    {
        var tempRoot = CreateTempDir();
        try
        {
            var pf = Path.Combine(tempRoot, "PF");
            var pd = Path.Combine(tempRoot, "PD");

            // Create the Revit directory but NOT the exe
            Directory.CreateDirectory(Path.Combine(pf, $"Autodesk\\Revit 2024"));

            var svc = new DeployCoreService(
                programFilesPath: pf,
                programDataPath: pd);

            var result = svc.DetectRevitVersions();

            Assert.Empty(result);
        }
        finally { CleanupDir(tempRoot); }
    }
}
