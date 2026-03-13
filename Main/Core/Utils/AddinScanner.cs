using System.Collections.Generic;
using System.Reflection;
using Main.Core.Models;
using Main.Core.Abstractions;
using System.IO;
using Main.Core.Models.Addin; // 使用 StandardAddin 和 AddinValidator

namespace Main.Core.Utils
{
    internal static class AddinScanner
    {
        /// <summary>
        /// 扫描 DLL，查找实现了 IExternalCommand 的类，返回 Addin 对象
        /// </summary>
        /// <param name="dllPath">DLL 文件路径</param>
        /// <returns>Addin 对象，包含所有命令项</returns>
        public static IAddin ScanAddin(string dllPath)
        {
            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
                throw new FileNotFoundException("DLL file not found.", dllPath);

            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                var commandType = typeof(Autodesk.Revit.UI.IExternalCommand);
                var addinItems = new List<IAddinNode>();

                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && commandType.IsAssignableFrom(type))
                    {
                        var item = new Command
                        {
                            AssemblyPath = dllPath,
                            AssemblyName = Path.GetFileName(dllPath),
                            FullClassName = type.FullName,
                            Name = type.Name,
                            Description = $"Command from {type.FullName}"
                        };
                        addinItems.Add(item);
                    }
                }

                if (addinItems.Count == 0)
                    return null;

                var addin = new StandardAddin
                {
                    Name = Path.GetFileNameWithoutExtension(dllPath),
                    ItemList = addinItems
                };

                addin.IsValidated = AddinValidator.ValidateAddin(addin);
                return addin;
            }
            catch
            {
                return null;
            }
        }
    }
}
