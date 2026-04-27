using System.Collections.Generic;
using System.IO;
using System.Linq;
using RevitCopilot.Deploy;
using RevitCopilot.Deploy.Models;

namespace DeployCore.Tests;

public class InstallRuntimeTests
{
    private readonly ITestOutputHelper _output;

    public InstallRuntimeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Creates a temporary directory with a Main.dll file (and optionally other files/subdirs).
    /// Caller is responsible for disposing.
    /// </summary>
    private static TempSource CreateSourceDir(string[] extraFiles = null, string[] subDirs = null)
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"deploy_source_{Guid.NewGuid():N}"));
        File.WriteAllText(Path.Combine(dir.FullName, "Main.dll"), "fake-dll");

        if (extraFiles != null)
        {
            foreach (var f in extraFiles)
                File.WriteAllText(Path.Combine(dir.FullName, f), "content");
        }

        if (subDirs != null)
        {
            foreach (var sd in subDirs)
            {
                var sub = Directory.CreateDirectory(Path.Combine(dir.FullName, sd));
                File.WriteAllText(Path.Combine(sub.FullName, "nested.dll"), "nested");
            }
        }

        return new TempSource(dir);
    }

    // ── Success cases ──────────────────────────────────────────────

    [Fact]
    public void InstallRuntime_ValidSource_CopiesFiles()
    {
        using var src = CreateSourceDir(extraFiles: new[] { "SharedLibrary.dll", "config.json" });

        var target = Path.Combine(Path.GetTempPath(), $"deploy_target_{Guid.NewGuid():N}");
        try
        {
            var svc = new DeployCoreService(defaultRuntimePath: target);
            var result = svc.InstallRuntime(src.Dir.FullName, target);

            Assert.True(result.Success, result.Message);
            Assert.Equal(DeployErrorCode.None, result.ErrorCode);
            Assert.Contains(result.Details, d => d.Contains("Main.dll"));
            Assert.Contains(result.Details, d => d.Contains("SharedLibrary.dll"));
            Assert.Contains(result.Details, d => d.Contains("config.json"));
            Assert.True(File.Exists(Path.Combine(target, "Main.dll")));
            Assert.True(File.Exists(Path.Combine(target, "SharedLibrary.dll")));
            Assert.True(File.Exists(Path.Combine(target, "config.json")));
        }
        finally
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    [Fact]
    public void InstallRuntime_WithSubDirs_CopiesRecursively()
    {
        using var src = CreateSourceDir(subDirs: new[] { "libs" });

        var target = Path.Combine(Path.GetTempPath(), $"deploy_target_{Guid.NewGuid():N}");
        try
        {
            var svc = new DeployCoreService(defaultRuntimePath: target);
            var result = svc.InstallRuntime(src.Dir.FullName, target);

            Assert.True(result.Success, result.Message);
            Assert.Contains("Copied dir: libs", result.Details);
            Assert.True(File.Exists(Path.Combine(target, "libs", "nested.dll")));
        }
        finally
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    [Fact]
    public void InstallRuntime_OverwriteTrue_OverwritesExistingFiles()
    {
        using var src = CreateSourceDir();

        var target = Path.Combine(Path.GetTempPath(), $"deploy_target_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(target);
            // Write a stale Main.dll
            File.WriteAllText(Path.Combine(target, "Main.dll"), "old-content");

            var svc = new DeployCoreService(defaultRuntimePath: target);
            var options = new DeployOptions { Overwrite = true };
            var result = svc.InstallRuntime(src.Dir.FullName, target, options);

            Assert.True(result.Success, result.Message);
            Assert.Contains("Copied: Main.dll", result.Details);
            var content = File.ReadAllText(Path.Combine(target, "Main.dll"));
            Assert.Equal("fake-dll", content);
        }
        finally
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    [Fact]
    public void InstallRuntime_OverwriteFalse_SkipsExistingFiles()
    {
        using var src = CreateSourceDir(extraFiles: new[] { "Extra.dll" });

        var target = Path.Combine(Path.GetTempPath(), $"deploy_target_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(target);
            File.WriteAllText(Path.Combine(target, "Main.dll"), "existing-content");

            var svc = new DeployCoreService(defaultRuntimePath: target);
            var options = new DeployOptions { Overwrite = false };
            var result = svc.InstallRuntime(src.Dir.FullName, target, options);

            Assert.True(result.Success, result.Message);
            Assert.Contains("Skipped (exists): Main.dll", result.Details);
            // Extra.dll should still be copied (doesn't exist yet)
            Assert.Contains("Copied: Extra.dll", result.Details);
            // Main.dll retains old content
            Assert.Equal("existing-content", File.ReadAllText(Path.Combine(target, "Main.dll")));
        }
        finally
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    [Fact]
    public void InstallRuntime_DefaultOptions_Overwrites()
    {
        // Default DeployOptions has Overwrite = true
        var options = new DeployOptions();
        Assert.True(options.Overwrite);
    }

    // ── Error cases ────────────────────────────────────────────────

    [Fact]
    public void InstallRuntime_NullSource_ReturnsSourceNotFound()
    {
        var target = Path.Combine(Path.GetTempPath(), $"deploy_target_{Guid.NewGuid():N}");
        try
        {
            var svc = new DeployCoreService(defaultRuntimePath: target);
            var result = svc.InstallRuntime(null, target);

            Assert.False(result.Success);
            Assert.Equal(DeployErrorCode.SourceNotFound, result.ErrorCode);
            Assert.Contains("required", result.Message);
        }
        finally
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    [Fact]
    public void InstallRuntime_NonexistentSource_ReturnsSourceNotFound()
    {
        var target = Path.Combine(Path.GetTempPath(), $"deploy_target_{Guid.NewGuid():N}");
        try
        {
            var svc = new DeployCoreService(defaultRuntimePath: target);
            var result = svc.InstallRuntime("/nonexistent/path/that/does/not/exist", target);

            Assert.False(result.Success);
            Assert.Equal(DeployErrorCode.SourceNotFound, result.ErrorCode);
        }
        finally
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    [Fact]
    public void InstallRuntime_SourceMissingMainDll_ReturnsInvalidSource()
    {
        // Create a source dir without Main.dll
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"deploy_bad_source_{Guid.NewGuid():N}"));
        File.WriteAllText(Path.Combine(dir.FullName, "readme.txt"), "no dll here");

        var target = Path.Combine(Path.GetTempPath(), $"deploy_target_{Guid.NewGuid():N}");
        try
        {
            var svc = new DeployCoreService(defaultRuntimePath: target);
            var result = svc.InstallRuntime(dir.FullName, target);

            Assert.False(result.Success);
            Assert.Equal(DeployErrorCode.InvalidSource, result.ErrorCode);
            Assert.Contains("Main.dll", result.Message);
        }
        finally
        {
            try { Directory.Delete(dir.FullName, recursive: true); } catch { }
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    [Fact]
    public void InstallRuntime_EmptySource_ReturnsInvalidSource()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"deploy_empty_source_{Guid.NewGuid():N}"));

        var target = Path.Combine(Path.GetTempPath(), $"deploy_target_{Guid.NewGuid():N}");
        try
        {
            var svc = new DeployCoreService(defaultRuntimePath: target);
            var result = svc.InstallRuntime(dir.FullName, target);

            Assert.False(result.Success);
            Assert.Equal(DeployErrorCode.InvalidSource, result.ErrorCode);
        }
        finally
        {
            try { Directory.Delete(dir.FullName, recursive: true); } catch { }
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    // ── Progress reporting ─────────────────────────────────────────

    [Fact]
    public void InstallRuntime_ReportsProgress()
    {
        using var src = CreateSourceDir();
        var target = Path.Combine(Path.GetTempPath(), $"deploy_target_{Guid.NewGuid():N}");
        try
        {
            var progressMessages = new List<string>();
            var progress = new Progress<string>(msg => progressMessages.Add(msg));

            var svc = new DeployCoreService(defaultRuntimePath: target);
            var options = new DeployOptions { Progress = progress };
            var result = svc.InstallRuntime(src.Dir.FullName, target, options);

            Assert.True(result.Success, result.Message);
            Assert.NotEmpty(progressMessages);
            Assert.Contains(progressMessages, m => m.Contains("Creating target directory"));
            Assert.Contains(progressMessages, m => m.Contains("Runtime installed to"));
        }
        finally
        {
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
        }
    }

    // ── Helper ─────────────────────────────────────────────────────

    private class TempSource : IDisposable
    {
        public DirectoryInfo Dir { get; }
        public TempSource(DirectoryInfo dir) => Dir = dir;
        public void Dispose()
        {
            try { Directory.Delete(Dir.FullName, recursive: true); } catch { }
        }
    }
}
