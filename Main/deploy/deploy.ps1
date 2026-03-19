param(
    [ValidateSet("Debug", "Release", IgnoreCase = $true)]
    [string]$Build = "Release",

    [switch]$SetNetwork
)

$ErrorActionPreference = "Stop"

$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "This deployment mode writes to ProgramData and requires Administrator privileges. Please run PowerShell as Administrator."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$buildConfigNormalized = [System.Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase($Build.ToLowerInvariant())
$buildOutput = Join-Path $repoRoot ("bin\" + $buildConfigNormalized)
$installDir = Join-Path $env:ProgramData "RevitCopilot\RevitAddinPlatform"
$machineAddinsRoot = Join-Path $env:ProgramData "Autodesk\Revit\Addins"
$supportedVersions = 2019..2024

$mainDll = Join-Path $buildOutput "Main.dll"
if (-not (Test-Path $mainDll)) {
    Write-Error "Build output not found: $mainDll`nPlease build $buildConfigNormalized|x64 first."
}

Write-Host ""
Write-Host "[1/2] Copying artifacts to install directory..."
Write-Host ("  Source: " + $buildOutput)
Write-Host ("  Target: " + $installDir)

if (-not (Test-Path $installDir)) {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

Get-ChildItem $buildOutput -File | ForEach-Object {
    Copy-Item $_.FullName -Destination $installDir -Force
    Write-Host ("  Copied: " + $_.Name)
}

Get-ChildItem $buildOutput -Directory | ForEach-Object {
    $target = Join-Path $installDir $_.Name
    if (-not (Test-Path $target)) {
        New-Item -ItemType Directory -Path $target -Force | Out-Null
    }
    Copy-Item (Join-Path $_.FullName "*") -Destination $target -Recurse -Force
    Write-Host ("  Copied dir: " + $_.Name)
}

Write-Host "  Done"

Write-Host ""
Write-Host "[2/2] Writing .addin files for installed Revit versions..."

$addinContentLines = @(
    '<?xml version="1.0" encoding="utf-8"?>',
    '<RevitAddIns>',
    '  <AddIn Type="Application">',
    '    <Name>RevitAddinPlatform</Name>',
    ('    <Assembly>' + (Join-Path $installDir "Main.dll") + '</Assembly>'),
    '    <ClientId>085B1F09-5D80-4432-8581-608416D639C5</ClientId>',
    '    <FullClassName>Main.PlatformApplication</FullClassName>',
    '    <VendorId>ADSK</VendorId>',
    '    <VendorDescription>Autodesk, www.autodesk.com</VendorDescription>',
    '  </AddIn>',
    '</RevitAddIns>'
)
$addinContent = [string]::Join([Environment]::NewLine, $addinContentLines)

$deployed = @()
$skipped = @()

foreach ($ver in $supportedVersions) {
    $revitExe = Join-Path $env:ProgramFiles ("Autodesk\Revit " + $ver + "\Revit.exe")
    if (-not (Test-Path $revitExe)) {
        $skipped += $ver
        continue
    }

    $addinsDir = Join-Path $machineAddinsRoot $ver
    $addinFile = Join-Path $addinsDir "RevitAddinPlatform.addin"

    if (-not (Test-Path $addinsDir)) {
        New-Item -ItemType Directory -Path $addinsDir -Force | Out-Null
    }

    [System.IO.File]::WriteAllText($addinFile, $addinContent, [System.Text.Encoding]::UTF8)

    $userAddinFile = Join-Path $env:APPDATA ("Autodesk\Revit\Addins\" + $ver + "\RevitAddinPlatform.addin")
    if (Test-Path $userAddinFile) {
        Remove-Item $userAddinFile -Force
        Write-Host ("  Removed user-level addin: " + $userAddinFile)
    }

    $deployed += $ver
    Write-Host ("  Revit " + $ver + " OK -> " + $addinFile)
}

if ($skipped.Count -gt 0) {
    Write-Host ""
    Write-Host ("  Skipped (not installed): " + ($skipped -join ", "))
}

Write-Host ""
Write-Host "=========================================="
if ($deployed.Count -gt 0) {
    Write-Host ("Deployment complete. Installed for Revit versions: " + ($deployed -join ", "))
} else {
    Write-Warning "No supported Revit versions (2019-2024) detected. Please check Addins folders."
}
Write-Host ("Install dir: " + $installDir)
Write-Host "=========================================="

# ─────────────────────────────────────────────────────────────────────────────
#  -SetNetwork: 配置 MCP 远程访问（URL ACL + 防火墙）
# ─────────────────────────────────────────────────────────────────────────────

if ($SetNetwork) {
    Write-Host ""
    Write-Host "Configuring network access for MCP service..."

    $port = 18181
    $url = "http://+:$port/"

    # 配置 URL ACL
    Write-Host "  [1/2] Configuring URL ACL..."
    try {
        $existingAcl = netsh http show urlacl url=$url 2>$null
        if ($existingAcl -match $url) {
            Write-Host "  URL ACL already exists, skipping."
        } else {
            netsh http add urlacl url=$url user=$env:USERNAME | Out-Null
            Write-Host "  URL ACL added for: $url"
        }
    } catch {
        Write-Error "  Failed to configure URL ACL. Make sure you're running as Administrator."
        Write-Host $_.Exception.Message
    }

    # 配置防火墙
    Write-Host "  [2/2] Configuring firewall..."
    try {
        $ruleName = "Revit MCP $port"
        $existingRule = netsh advfirewall firewall show rule name="$ruleName" 2>$null
        if ($existingRule -match $ruleName) {
            Write-Host "  Firewall rule already exists, skipping."
        } else {
            netsh advfirewall firewall add rule name="$ruleName" dir=in action=allow protocol=TCP localport=$port | Out-Null
            Write-Host "  Firewall rule added: $ruleName"
        }
    } catch {
        Write-Error "  Failed to configure firewall."
        Write-Host $_.Exception.Message
    }

    Write-Host ""
    Write-Host "Network configuration complete."
    Write-Host "MCP service is now accessible from the network on port $port."
    Write-Host ""
    Write-Host "  WARNING: If this machine has a public IP, consider restricting"
    Write-Host "  access via firewall rules to specific IP ranges."
    Write-Host ""
}

Write-Host ""
