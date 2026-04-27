using System;

namespace RevitCopilot.Deploy.Models
{
    /// <summary>
    /// Represents a detected Revit installation on the system.
    /// </summary>
    public class RevitInstance
    {
        /// <summary>Revit version year (e.g. 2019, 2020, ..., 2024).</summary>
        public int VersionYear { get; set; }

        /// <summary>
        /// Full path to the Revit installation directory
        /// (e.g. "C:\Program Files\Autodesk\Revit 2024").
        /// </summary>
        public string InstallPath { get; set; }

        /// <summary>
        /// Machine-wide Addins directory for this Revit version
        /// (e.g. "C:\ProgramData\Autodesk\Revit\Addins\2024").
        /// </summary>
        public string AddinDirectory { get; set; }

        /// <summary>
        /// Whether a RevitAddinPlatform.addin file already exists for this version.
        /// </summary>
        public bool IsRegistered { get; set; }

        /// <summary>Full path to Revit.exe.</summary>
        public string RevitExePath { get; set; }

        public override string ToString() => $"Revit {VersionYear}  [{InstallPath}]";
    }
}
