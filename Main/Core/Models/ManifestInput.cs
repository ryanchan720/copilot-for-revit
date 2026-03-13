using System.Collections.Generic;

namespace Main.Core.Models
{
    internal class ManifestInput
    {
        /// <summary>
        /// Target folder where the .addin file will be created.
        /// Must be an existing or creatable folder.
        /// </summary>
        public string TargetFolder { get; set; }

        /// <summary>
        /// Base file name without extension. Defaults to DefaultSetting.FileName if null or empty.
        /// </summary>
        public string BaseFileName { get; set; }

        /// <summary>
        /// If true, the Assembly element will use only file name (local mode),
        /// otherwise absolute path.
        /// </summary>
        public bool Local { get; set; }

        /// <summary>
        /// Optional VendorDescription to embed in the manifest.
        /// </summary>
        public string VendorDescription { get; set; }

        /// <summary>
        /// ExecuteCommand entries to include.
        /// </summary>
        public IList<AddinItem> Commands { get; set; } = new List<AddinItem>();

        /// <summary>
        /// Application entries to include.
        /// </summary>
        public IList<AddinItem> Applications { get; set; } = new List<AddinItem>();
    }
}
