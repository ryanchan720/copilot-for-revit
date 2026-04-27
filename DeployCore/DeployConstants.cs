namespace RevitCopilot.Deploy
{
    /// <summary>
    /// Shared constants used across DeployCore.
    /// </summary>
    public static class DeployConstants
    {
        /// <summary>Default install location under %ProgramData%.</summary>
        public const string DefaultInstallRoot = @"RevitCopilot\Runtime";

        /// <summary>Machine-wide Revit Addins root.</summary>
        public const string MachineAddinsRoot = @"Autodesk\Revit\Addins";

        /// <summary>Name of the .addin manifest file.</summary>
        public const string AddinFileName = "RevitAddinPlatform.addin";

        /// <summary>Name of the main Runtime assembly.</summary>
        public const string MainAssemblyName = "Main.dll";

        /// <summary>Revit Copilot addin ClientId.</summary>
        public const string AddinClientId = "085B1F09-5D80-4432-8581-608416D639C5";

        /// <summary>Full class name of the Revit external application.</summary>
        public const string AddinFullClassName = "Main.PlatformApplication";

        /// <summary>Revit executable name.</summary>
        public const string RevitExeName = "Revit.exe";

        /// <summary>Supported Revit version range (inclusive).</summary>
        public const int MinRevitVersion = 2019;
        public const int MaxRevitVersion = 2024;

        /// <summary>
        /// Template for the .addin file content.
        /// {0} = full path to Main.dll, {1} = ClientId, {2} = FullClassName.
        /// </summary>
        public const string AddinFileTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<RevitAddIns>
  <AddIn Type=""Application"">
    <Name>RevitAddinPlatform</Name>
    <Assembly>{0}</Assembly>
    <ClientId>{1}</ClientId>
    <FullClassName>{2}</FullClassName>
    <VendorId>ADSK</VendorId>
    <VendorDescription>Autodesk, www.autodesk.com</VendorDescription>
  </AddIn>
</RevitAddIns>";
    }
}
