using Main.Core.Abstractions;
using System;
using System.Collections.Generic;

namespace Main.Core.Models.Addin
{

    /// <summary>
    /// 插件基类，包含公共属性和验证逻辑
    /// </summary>
    public abstract class BaseAddin : IAddin
    {
        public Guid Id { get; }
        public string Name { get; set; }
        public List<RevitVersions.RevitVersion> RevitVersions { get; set; } = new List<RevitVersions.RevitVersion>();
        public string Description { get; set; }
        public bool IsValidated { get; set; } = false;
        public bool IsActive { get; set; } = false;
        public bool IsCompatible { get; set; } = false;
        public List<IAddinNode> ItemList { get; set; } = new List<IAddinNode>();

        protected BaseAddin()
        {
            Id = Guid.NewGuid();
            IsValidated = AddinValidator.ValidateAddin(this);
        }
    }

    /// <summary>
    /// Revit 标准插件，在 Revit 启动时加载，不支持热加载
    /// </summary>
    public class StandardAddin : BaseAddin
    {
        public StandardAddin() : base() { }
    }

    /// <summary>
    /// 热加载插件，仅在首次加载时使用，使用 Add-In Manager 执行
    /// 自动放置到标准插件目录，在下次启动 Revit 后以作为标准插件使用
    /// </summary>
    public class HotLoadingAddin : BaseAddin
    {
        public HotLoadingAddin() : base() { }
    }

    public class AddinValidator
    {
        public static bool ValidateAddin(IAddin addin)
        {
            // 检查名称是否为空
            if (string.IsNullOrWhiteSpace(addin.Name))
            {
                return false;
            }
            // 检查当前运行的 Revit 版本是否受支持

            return true;
        }
    }
}
