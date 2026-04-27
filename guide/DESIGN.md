# 架构设计

本文档定义新安装体系的架构设计与分层模型。

---

## 分层模型

新安装体系采用三层架构，自下而上为：

```
┌─────────────────────────────────────────────────────────┐
│                  Installer Shell                        │
│  （用户交互层：GUI/CLI、参数解析、错误提示）              │
└─────────────────────────────────────────────────────────┘
                          ↓ 调用
┌─────────────────────────────────────────────────────────┐
│                   Deploy Core                           │
│  （部署逻辑层：文件复制、注册表、.addin 生成）           │
└─────────────────────────────────────────────────────────┘
                          ↓ 操作
┌─────────────────────────────────────────────────────────┐
│                     Runtime                             │
│  （运行时层：Main.dll、SharedLibrary.dll、依赖）         │
└─────────────────────────────────────────────────────────┘
```

### Layer 1: Runtime（运行时层）

**职责**：
- 提供 Revit Copilot 核心功能（MCP 服务、插件加载、命令调度）。
- 无安装逻辑，纯运行时组件。

**组成**：
- `Main.dll` - 主程序集
- `SharedLibrary.dll` - 共享库
- `Newtonsoft.Json.dll` - JSON 序列化
- `Microsoft.Web.WebView2.*.dll` - WebView2 依赖
- `runtimes/win-x64/native/WebView2Loader.dll` - WebView2 运行时

**特性**：
- **版本无关**：同一 Runtime 可服务于多个 Revit 版本（2019-2024）。
- **位置固定**：统一安装到 `%ProgramData%\RevitCopilot\Runtime\`。
- **自包含**：所有依赖打包在一起，无需 GAC 或外部依赖。

---

### Layer 2: Deploy Core（部署逻辑层）

**职责**：
- 实现 Runtime 的安装、升级、卸载逻辑。
- 为每个 Revit 版本生成 `.addin` 清单文件。
- 管理插件包的注册与卸载。
- 配置网络访问（URL ACL、防火墙）。

**组成**：
- `DeployCore.dll` - 部署核心库（.NET Standard 2.0）
- `DeployCore.psm1` - PowerShell 模块封装（可选）

**核心 API**：

```csharp
namespace RevitCopilot.Deploy
{
    public interface IDeployCore
    {
        // Runtime 管理
        DeployResult InstallRuntime(string sourcePath, string targetPath, DeployOptions options);
        DeployResult UpgradeRuntime(string sourcePath, string targetPath);
        DeployResult UninstallRuntime();

        // Revit 版本管理
        IEnumerable<RevitInstance> DetectRevitVersions();
        DeployResult RegisterRevitVersion(RevitInstance revit);
        DeployResult UnregisterRevitVersion(RevitInstance revit);

        // 插件管理
        DeployResult InstallPlugin(string pluginPath);
        DeployResult UninstallPlugin(string pluginName);
        IEnumerable<PluginInfo> ListPlugins();

        // 网络配置
        DeployResult ConfigureNetwork(int port, bool allowRemote);
        DeployResult RemoveNetworkConfig();
    }
}
```

**特性**：
- **幂等性**：所有操作可重复执行，无副作用。
- **原子性**：失败时回滚，不留中间状态。
- **可测试**：纯逻辑，无 UI 依赖，可单元测试。

---

### Layer 3: Installer Shell（用户交互层）

**职责**：
- 提供用户界面（GUI 安装器 / CLI 工具）。
- 解析用户输入参数。
- 调用 Deploy Core 执行操作。
- 显示进度、错误、成功信息。

**组成**：
- `RevitCopilotInstaller.msi` - Windows Installer 包（普通用户）
- `revit-copilot-cli.exe` - 命令行工具（高级用户、CI/CD）
- `install.ps1` / `uninstall.ps1` - PowerShell 脚本（开发者）

**特性**：
- **多入口**：GUI、CLI、脚本三种方式，满足不同用户需求。
- **参数化**：支持自定义安装路径、Revit 版本选择、网络配置等。
- **友好提示**：清晰的错误信息、进度反馈、成功确认。

---

## 仓库职责边界

### 本仓库（copilot-for-revit）

| 组件 | 当前状态 | 目标状态 |
|------|----------|----------|
| **Runtime** | `Main.dll` + 依赖 | 保持不变，产出 Runtime 包 |
| **Deploy Core** | `deploy.ps1`（脚本） | 迁移到 `DeployCore.dll`（类库） |
| **Installer Shell** | `Setup.vdproj`（MSI） | 重构为新 MSI + CLI 工具 |
| **文档** | README.md、QUICKSTART.md | 保持，补充安装体系文档 |

**发布产物**：
1. `RevitCopilot-Runtime-{version}.zip` - Runtime 包
2. `RevitCopilot-Installer-{version}.msi` - GUI 安装器
3. `RevitCopilot-CLI-{version}.zip` - CLI 工具包

---

### general-copilot-addins-for-revit

| 组件 | 当前状态 | 目标状态 |
|------|----------|----------|
| **插件包** | 多个独立目录 | 打包为标准 Plugin Package |
| **安装方式** | 手动复制 | 通过 Installer 或 CLI 安装 |

**发布产物**：
- `GeneralAddins-{version}.zip` - 包含所有插件的标准包
- 或每个插件独立打包：`Copilot.ElementCRUD-{version}.zip` 等

**职责**：
- 提供现成的命令插件。
- 定义插件包格式标准（供其他开发者参考）。

---

### copilot-addins-for-revit（开发模板）

| 组件 | 当前状态 | 目标状态 |
|------|----------|----------|
| **项目模板** | Visual Studio 模板 | 保持，补充打包脚本 |
| **文档** | 开发指南 | 补充插件打包、发布流程 |

**职责**：
- 提供插件开发脚手架。
- 定义插件开发规范。
- 提供打包工具（生成 Plugin Package）。

---

### openclaw-bridge

| 组件 | 当前状态 | 目标状态 |
|------|----------|----------|
| **桥接器** | Python CLI 工具 | 保持不变 |
| **安装方式** | 手动 `git clone` + `uv sync` | 可选：提供 pip 包 |

**职责**：
- 连接 OpenClaw 和 Revit Copilot。
- 不涉及 Windows 端安装，仅 Linux 端部署。

---

### copilot-for-revit-skill

| 组件 | 当前状态 | 目标状态 |
|------|----------|----------|
| **Skill 包** | OpenClaw skill | 保持不变 |

**职责**：
- 提供 OpenClaw skill 定义。
- 不涉及安装体系改造。

---

## 数据流与依赖关系

### 安装流程数据流

```
用户输入（GUI/CLI）
    ↓
Installer Shell（解析参数）
    ↓
Deploy Core（执行逻辑）
    ├─→ 检测 Revit 版本
    ├─→ 复制 Runtime 文件
    ├─→ 生成 .addin 文件
    ├─→ 配置网络（可选）
    └─→ 注册插件（可选）
    ↓
文件系统 + 注册表
```

### 运行时依赖关系

```
Revit 2019/2020/.../2024
    ↓ 加载
RevitAddinPlatform.addin
    ↓ 指向
%ProgramData%\RevitCopilot\Runtime\Main.dll
    ↓ 加载
├─ SharedLibrary.dll
├─ Newtonsoft.Json.dll
├─ Microsoft.Web.WebView2.*.dll
└─ Addins\*.dll（插件）
```

---

## 关键设计决策

### 决策 1：Runtime 统一安装位置

**选择**：`%ProgramData%\RevitCopilot\Runtime\`

**理由**：
- `%ProgramData%` 为系统级、所有用户共享。
- 避免权限问题（`%ProgramFiles%` 需要管理员）。
- 与当前 `deploy.ps1` 行为一致，降低迁移成本。

**替代方案**：
- `%LocalAppData%` - 用户级安装，不支持多用户共享。
- `%ProgramFiles%` - 需要管理员权限，安装门槛高。

---

### 决策 2：.addin 文件按版本分散

**选择**：每个 Revit 版本独立 `.addin` 文件，指向同一 Runtime。

**理由**：
- Revit 要求 `.addin` 文件在版本特定目录。
- Runtime 版本无关，避免重复安装。
- 卸载时只需删除 `.addin` 文件，不影响其他版本。

---

### 决策 3：Deploy Core 独立为类库

**选择**：将部署逻辑从 PowerShell 脚本迁移到 .NET 类库。

**理由**：
- 类型安全、可测试、可维护。
- 可被 MSI、CLI、脚本共同调用。
- 便于后续扩展（如支持 macOS、Linux）。

**迁移策略**：
- Phase 1：保留 `deploy.ps1`，新增 `DeployCore.dll`。
- Phase 2：`deploy.ps1` 调用 `DeployCore.dll`。
- Phase 3：废弃 `deploy.ps1`，推荐 CLI 工具。

---

### 决策 4：插件包标准化

**选择**：定义标准 Plugin Package 格式。

**格式**：
```
{PluginName}/
├── {PluginName}.dll        # 主程序集
├── README.md               # 元数据（版本、兼容性）
├── manifest.json           # 清单（依赖、Revit 版本）
└── libs/                   # 第三方依赖
    └── *.dll
```

**理由**：
- 当前 README.md 格式非结构化，不便解析。
- `manifest.json` 可描述依赖关系、版本约束。
- 便于 Installer 验证、依赖解析。

---

## 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 用户已安装旧版本 | 升级路径不清晰 | 提供迁移工具，自动检测并升级 |
| Revit API 引用问题 | 编译失败 | 提供开发环境快速搭建脚本 |
| 网络配置失败 | 远程访问不可用 | 提供诊断工具、详细错误提示 |
| 插件依赖冲突 | 运行时错误 | manifest.json 描述依赖，安装时检查 |
| MSI 卸载不完整 | 残留文件 | 卸载时清理所有相关文件、注册表 |
