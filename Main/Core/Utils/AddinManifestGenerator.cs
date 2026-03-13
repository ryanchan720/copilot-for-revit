using Main.Core.Models;
using System;
using System.IO;

namespace Main.Core.Utils
{
    internal class AddinManifestGenerator
    {
        /// <summary>
        /// Generate a Revit .addin file from the provided input and return the written file path.
        /// </summary>
        /// <param name="input">Input entries and options.</param>
        /// <returns>Absolute path to the generated .addin file.</returns>
        public static string Generate(ManifestInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (string.IsNullOrWhiteSpace(input.TargetFolder)) throw new ArgumentNullException(nameof(input.TargetFolder));

            var baseName = string.IsNullOrWhiteSpace(input.BaseFileName)
            ? DefaultSetting.FileName
            : input.BaseFileName;

            // Ensure target folder exists
            Directory.CreateDirectory(input.TargetFolder);

            // Choose a non-conflicting file path: <baseName>.addin, <baseName>1.addin, ...
            var filePath = GetProperFilePath(input.TargetFolder, baseName, DefaultSetting.FormatExAddin);

            var manifest = new ManifestFile(input.Local)
            {
                VendorDescription = input.VendorDescription ?? string.Empty,
            };

            // Populate entries (only those with Save == true if provided)
            if (input.Commands != null)
            {
                foreach (var cmd in input.Commands)
                {
                    if (cmd != null && (cmd.Save))
                        manifest.Commands.Add(cmd);
                }
            }
            if (input.Applications != null)
            {
                foreach (var app in input.Applications)
                {
                    if (app != null && (app.Save))
                        manifest.Applications.Add(app);
                }
            }

            manifest.SaveAs(filePath);
            return filePath;
        }

        private static string GetProperFilePath(string folder, string fileNameWithoutExt, string ext)
        {
            string filePath;
            var num = -1;
            do
            {
                num++;
                var name = num <= 0 ? fileNameWithoutExt + ext : fileNameWithoutExt + num + ext;
                filePath = Path.Combine(folder, name);
            }
            while (File.Exists(filePath));
            return filePath;
        }

    }
}
