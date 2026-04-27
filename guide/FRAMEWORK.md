# 实施框架

本文档定义标准发布产物、安装流程、升级与卸载流程。

---

## 标准发布产物

### 1. Runtime 包

**文件名**：`RevitCopilot-Runtime-{version}.zip`

**内容**：
```
Runtime/
├── Main.dll
├── SharedLibrary.dll
├── Newtonsoft.Json.dll
├── Microsoft.Web.WebView2.Core.dll
├── Microsoft.Web.WebView2.WinForms.dll
├── Microsoft.Web.WebView2.Wpf.dll
├── runtimes/
│   └── win-x64/
│       └── native/
│           └── WebView2Loader.dll
├── Addins/                  # 空目录，预留给插件
├── User/                    # 用户数据目录
├── Configs/
│   └── Updates/             # 升级配置
└── manifest.json            # Runtime 元数据
```

**manifest.json 示例**：
```json
{
  "name": "RevitCopilot.Runtime",
  "version": "1.0.0",
  "revitVersions": ["2019", "2020", "2021", "2022", "2023", "2024"],
  "framework": ".NETFramework 4.8",
  "dependencies": []
}
```

**用途**：
- 被 Installer 解压并安装到 `%ProgramData%\RevitCopilot\Runtime\`。
- 可独立下载，供高级用户手动安装。

---

### 2. 默认插件包

**文件名**：`RevitCopilot-DefaultAddins-{version}.zip`

**内容**：
```
Addins/
├── Copilot.ElementCRUD/
│   ├── Copilot.ElementCRUD.dll
│   ├── README.md
│   ├── manifest.json
│   └── libs/
├── Copilot.Annotations/
│   └── ...
├── Copilot.View/
│   └── ...
└── Copilot.GeneralUtils/
    └── ...
```

**用途**：
- 可选安装，提供常用命令。
- 安装到 `%ProgramData%\RevitCopilot\Runtime\Addins\`。

---

### 3. GUI 安装器

**文件名**：`RevitCopilot-Installer-{version}.msi`

**功能**：
- 检测已安装的 Revit 版本。
- 让用户选择要注册的 Revit 版本。
- 提供自定义安装路径选项（高级）。
- 提供网络配置选项（高级）。
- 可选安装默认插件包。

**安装步骤**：
1. 欢迎页
2. Revit 版本选择（自动检测，可手动选择）
3. 安装选项（网络配置、默认插件）
4. 确认安装
5. 执行安装（显示进度）
6. 完成

**卸载**：
- 通过"添加/删除程序"卸载。
- 清理所有相关文件、注册表、.addin 文件。

---

### 4. CLI 工具包

**文件名**：`RevitCopilot-CLI-{version}.zip`

**内容**：
```
CLI/
├── revit-copilot.exe        # 主命令
├── DeployCore.dll           # 部署核心库
├── *.dll                    # 其他依赖
└── README.md                # 使用说明
```

**命令示例**：

```powershell
# 安装 Runtime
revit-copilot install --runtime RevitCopilot-Runtime-1.0.0.zip

# 安装到指定 Revit 版本
revit-copilot install --revit-versions 2020,2021,2022

# 安装插件
revit-copilot plugin install Copilot.ElementCRUD.zip

# 列出已安装插件
revit-copilot plugin list

# 配置网络
revit-copilot config network --port 18181 --allow-remote

# 升级 Runtime
revit-copilot upgrade --runtime RevitCopilot-Runtime-1.1.0.zip

# 卸载
revit-copilot uninstall --clean-all
```

**用途**：
- 高级用户、CI/CD、脚本化部署。
- 开发者快速搭建开发环境。

---

## 安装流程

### 普通用户流程（GUI）

```
1. 下载 RevitCopilot-Installer-{version}.msi
2. 双击运行，UAC 提权
3. 选择要注册的 Revit 版本（默认全选已安装版本）
4. （可选）勾选"配置远程访问"
5. （可选）勾选"安装默认插件包"
6. 点击"安装"
7. 等待完成，启动 Revit 验证
```

**预期结果**：
- `%ProgramData%\RevitCopilot\Runtime\` 目录创建。
- 每个 Revit 版本的 Addins 目录下生成 `RevitAddinPlatform.addin`。
- Revit 启动后功能区出现"Revit Copilot"选项卡。

---

### 高级用户流程（CLI）

```powershell
# 1. 下载并解压 CLI 工具包
Expand-Archive RevitCopilot-CLI-1.0.0.zip -DestinationPath C:\Tools\RevitCopilot-CLI

# 2. 安装 Runtime
C:\Tools\RevitCopilot-CLI\revit-copilot.exe install --runtime RevitCopilot-Runtime-1.0.0.zip

# 3. 配置网络（可选）
C:\Tools\RevitCopilot-CLI\revit-copilot.exe config network --port 18181 --allow-remote

# 4. 安装插件（可选）
C:\Tools\RevitCopilot-CLI\revit-copilot.exe plugin install Copilot.ElementCRUD.zip

# 5. 验证
C:\Tools\RevitCopilot-CLI\revit-copilot.exe doctor
```

---

### 开发者流程

```powershell
# 1. 克隆仓库
git clone https://github.com/ryanchan720/copilot-for-revit.git
cd copilot-for-revit

# 2. 使用开发环境搭建脚本
.\scripts\setup-dev.ps1 -RevitVersion 2024

# 3. 打开 Visual Studio，编译
# （脚本已自动添加 Revit API 引用）

# 4. 部署到本地测试
.\scripts\deploy-local.ps1

# 5. 打包
.\scripts\package.ps1 -Version 1.0.0
```

---

## 升级流程

### Runtime 升级

```
1. 检测当前已安装版本
2. 下载新版本 Runtime 包
3. 备份当前 Runtime（可选）
4. 替换文件（保留 Addins、User、Configs）
5. 更新 manifest.json
6. 重启 Revit 生效
```

**兼容性检查**：
- 检查新版本支持的 Revit 版本是否包含当前已注册版本。
- 检查插件兼容性（通过 manifest.json）。

---

### 插件升级

```
1. 检测已安装插件版本
2. 下载新版本插件包
3. 替换插件目录（保留配置）
4. 更新插件 manifest.json
5. 刷新 MCP Client（提示用户）
```

---

## 卸载流程

### 完整卸载

```
1. 删除 %ProgramData%\RevitCopilot\ 目录
2. 删除所有 Revit 版本的 .addin 文件
   - %ProgramData%\Autodesk\Revit\Addins\2019\RevitAddinPlatform.addin
   - %ProgramData%\Autodesk\Revit\Addins\2020\RevitAddinPlatform.addin
   - ...
3. 移除网络配置（URL ACL、防火墙规则）
4. 清理注册表（如有）
```

### 保留数据卸载

```
1. 仅删除 Runtime 文件
2. 保留 Addins、User、Configs 目录
3. 删除 .addin 文件
```

---

## 自检流程

### 安装后验证

```powershell
revit-copilot doctor
```

**检查项**：
1. Runtime 文件完整性（校验文件哈希）
2. .addin 文件存在性
3. Revit 版本检测
4. 网络配置状态（如已配置）
5. 插件加载状态
6. MCP 服务启动测试

**输出示例**：
```
✓ Runtime 文件完整
✓ .addin 文件已生成（2019, 2020, 2021, 2022, 2023, 2024）
✓ Revit 版本检测正常
✓ 网络配置正常（端口 18181）
✓ 插件加载正常（4 个插件）
✓ MCP 服务启动成功

所有检查通过！
```

---

## 错误处理

### 常见错误与解决方案

| 错误 | 原因 | 解决方案 |
|------|------|----------|
| "Access is denied" | URL ACL 未配置 | 运行 `revit-copilot config network --port 18181 --allow-remote` |
| "Revit API not found" | Revit 未安装或路径错误 | 确认 Revit 已安装，或手动指定路径 |
| "Plugin load failed" | 依赖缺失或版本不兼容 | 检查插件 manifest.json，安装缺失依赖 |
| "Port already in use" | 端口被占用 | 更换端口或停止占用进程 |

---

## 配置文件

### 全局配置

**位置**：`%ProgramData%\RevitCopilot\config.json`

**内容**：
```json
{
  "runtimeVersion": "1.0.0",
  "installedRevitVersions": ["2019", "2020", "2021", "2022", "2023", "2024"],
  "network": {
    "enabled": true,
    "port": 18181,
    "allowRemote": true
  },
  "plugins": [
    {
      "name": "Copilot.ElementCRUD",
      "version": "1.0.0",
      "enabled": true
    }
  ]
}
```

---

## 第一期明确不做什么

### 不做的功能

1. **多语言支持**：第一期仅支持中文和英文。
2. **自动更新**：不实现自动检查更新，需手动下载新版本。
3. **插件市场**：不实现插件在线浏览、下载功能。
4. **配置 GUI**：不提供配置管理界面，通过 CLI 或配置文件。
5. **回滚机制**：升级失败时需手动卸载重装。
6. **企业部署**：不支持 Group Policy、SCCM 等企业部署工具集成。
7. **macOS/Linux 支持**：仅支持 Windows。

### 原因

- 控制第一期范围，确保核心功能稳定。
- 部分功能需用户反馈后再决定是否实现。
- 企业部署需求不明确，暂不投入。

---

## 测试策略

### 单元测试

- Deploy Core 所有公开 API。
- 覆盖正常流程、边界条件、错误场景。

### 集成测试

- 端到端安装、升级、卸载流程。
- 在干净虚拟机中测试（避免环境污染）。
- 测试所有支持的 Revit 版本（2019-2024）。

### 手动测试

- GUI 安装器用户体验。
- CLI 命令易用性。
- 错误提示清晰度。
