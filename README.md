# Copilot For Revit - 面向 AI 的 Revit 命令执行框架

本框架将 Revit 与 AI 能力相结合，以 AI 为脑、插件为手，实现 AI 对 Revit 的自主控制。

## Copilot 生态

本仓库是 Copilot For Revit 生态的核心组件：

| 仓库 | 定位 | 说明 |
|------|------|------|
| **本仓库** | 主框架 | AI 驱动 Revit 的核心平台，支持 MCP 协议。负责插件加载、命令调度、与 AI 对话工具（Cline、Claude、Cherry Studio 等）通信。**需先安装此框架才能使用插件。** |
| [copilot-addins-for-revit](https://github.com/ryanchan720/copilot-addins-for-revit-template) | 开发模板 | AI 友好的插件开发脚手架。提供项目模板、开发规范、最佳实践，帮助开发者快速创建符合框架标准的命令插件。适合想要开发自定义命令的用户。 |
| [general-copilot-addins-for-revit](https://github.com/ryanchan720/general-copilot-addins-for-revit) | 通用插件 | 提供现成的常用命令，覆盖元素查询、参数修改、标注创建、视图管理等高频场景。开箱即用，可直接安装到框架中。适合普通用户和快速上手。 |

**快速选择指南**：
- 想用 AI 控制 Revit → 安装本框架 + 通用插件
- 想开发自己的命令 → 使用开发模板
- 想直接用现成功能 → 安装通用插件

---

## 核心特性

- MCP Client 支持。Cline、Cherry Studio、Claude 等支持 MCP 协议的对话工具可驱动 Revit 执行操作。

- 持续新增的原生通用工具。覆盖元素修改、视图编辑、注释创建、图纸打印等功能的原生插件，满足大量基础需求。

- AI 友好的插件开发模板。基于此模板开发可自动接入 AI 框架，协助你专注于功能实现。插件开发模板仓库：`https://github.com/ryanchan720/copilot-addins-for-revit`

- 低成本迁移已有插件。若已有存量插件，可根据迁移指南，以最低成本激活插件价值。

- 插件热加载。编译好的插件可直接被框架识别和调用，无需额外修改代码。

- （开发中）接入 OpenClaw。

- ---

## 环境要求

| 项目             | 要求                              |
| -------------- | ------------------------------- |
| Revit          | 2019 - 2024 任意版本                |
| Visual Studio  | 2019 或更高，需安装 **.NET 桌面开发** 工作负载 |
| .NET Framework | 4.8（VS 安装时自动附带）                 |

---

## 快速开始

### 第一步：克隆仓库

```bash
git clone https://github.com/ryanchan720/copilot-for-revit.git
cd CopilotForRevit
```

### 第二步：打开并编译

用 Visual Studio 打开 `CopilotForRevit.sln`。

添加 Revit API 引用：

1. 右键 `Main` 项目 → 添加 → 引用
2. 浏览到 Revit 安装目录（通常在 `C:\Program Files\Autodesk\Revit 20XX`），选择 `RevitAPI.dll` 和 `RevitAPIUI.dll` 添加引用。

编译 `Main` 项目。**默认配置针对 Revit 2019**，如果你安装的是其他版本，在编译前修改 `Main\Main.csproj` 第 16 行的默认值：

```xml
<RevitVersion Condition="'$(RevitVersion)' == ''">20XX</RevitVersion>
```

或直接在命令行传入版本号（无需修改文件）：

```
msbuild Main\Main.csproj /p:Configuration=Release /p:Platform=x64 /p:RevitVersion=20XX
```

选择 **`Release | x64`** 配置，执行编译（Ctrl+Shift+B）。

### 第三步：部署到 Revit

以**管理员身份**启动 PowerShell，执行以下命令（非管理员无法向正确位置复制程序文件）

```powershell
cd Main\deploy
.\deploy.ps1
```

脚本会自动检测本机已安装的所有 Revit 版本（2019~2024），完成以下操作：

1. 将编译产物复制到 `%ProgramData%\RevitCopilot\RevitAddinPlatform\`
2. 在每个已安装版本的 Addins 目录写入 `.addin` 清单

```
%ProgramData%\RevitCopilot\RevitAddinPlatform\   ← DLL 主程序集唯一安装位置
%ProgramData%\Autodesk\Revit\Addins\2019\RevitAddinPlatform.addin
%ProgramData%\Autodesk\Revit\Addins\2024\RevitAddinPlatform.addin
...
```

### 第四步：启动 Revit 验证

打开 Revit，功能区应出现 **Revit Copilot** 选项卡，有 Execute 按钮，即部署成功。

### 第五步：插件安装与使用

#### 1）安装插件（命令包）

将插件目录（目录名需与主 DLL 同名）复制到：

```
%ProgramData%\RevitCopilot\RevitAddinPlatform\Addins\
```

插件目录必须包含：

- `xxx.dll`（与目录同名的主程序集）
- `README.md`（用于声明命令兼容的 Revit 版本与语言）
- 其他你插件需要依赖的第三方库

框架会在检测到新目录后自动完成注册（可以观察到插件目录会被自动添加一串字母后缀）。

插件安装可以在 Revit 运行时进行，安装后需要手动在 MCP Client 刷新一下 MCP server 以确保新安装的工具被加载。

#### 2）使用插件

1. 打开 Revit 项目（`*.rvt`）。
2. 由 MCP 客户端（如 Cline、Cherry Studio、Claude）发起命令调用，框架会按名称匹配并执行已注册命令。

#### 3）更新插件

关闭 Revit，将新编译的文件替换已安装插件目录中的文件即可，文件夹名称不需要变动，重新启动 Revit 即可生效（旧的 dll 加载到程序后只能通过关闭程序释放）。

---

## 项目结构

```
CopilotForRevit/
├── Main/                        # 主插件项目（.NET Framework 4.8）
│   ├── PlatformApplication.cs   # Revit ExternalApplication 入口
│   ├── Core/
│   │   ├── Initializer.cs       # 核心服务初始化
│   │   ├── AddinRegistry.cs     # 插件注册与管理
│   │   └── Services/
│   │       ├── RevitService.cs  # Revit API 全局访问
│   │       ├── SocketService.cs # 外部通信（Socket）
│   │       └── Mcp/McpService.cs# MCP 协议支持
│   └── deploy/
│       └── deploy.ps1           # 多版本部署脚本
└── SharedLibrary/               # 框架与插件通信库（.NET Framework 4.7）
    └── AppEventHub.cs           # 全局事件总线
```

---

## 多版本构建说明

| Build Configuration | 目标 Revit | 输出目录                | DefineConstants         |
| ------------------- | -------- | ------------------- | ----------------------- |
| `Debug\|x64`        | 2019     | `bin\Debug\`        | `DEBUG;TRACE;REVIT2019` |
| `Release\|x64`      | 2019     | `bin\Release\`      | `TRACE;REVIT2019`       |
| `Debug_2024\|x64`   | 2024     | `bin\2024\Debug\`   | `DEBUG;TRACE;REVIT2024` |
| `Release_2024\|x64` | 2024     | `bin\2024\Release\` | `TRACE;REVIT2024`       |

> **说明**：针对 Revit 2019 编译的产物可直接在 2019~2024 所有版本上运行（API 向下兼容）。`Debug_2024` / `Release_2024` 配置用于验证代码与最新 API 的兼容性，并不需要单独部署。

---

## 常见问题

**Q：编译报错"找不到 RevitAPI.dll"**  
A：确认 Revit 已安装，且 `RevitVersion` 与本机安装版本一致。

**Q：deploy.ps1 提示"跳过（未安装）"**
A：脚本检测 Revit 安装目录是否存在 Revit.exe 来判断版本安装状态，某个版本未安装则自动跳过，属正常现象。

**Q：如何卸载？**
A：删除 `%ProgramData%\RevitCopilot\` 目录，并删除各 Revit 版本 Addins 目录下的 `RevitAddinPlatform.addin` 文件。

**Q：我开发的命令插件需要使用 `\*.addin` 在 Revit 中注册吗？**  
A：不需要。命令插件按指引放置到指定位置，框架会自动识别并在需要的时候调用，不需要通过 Revit 的注册机制。
