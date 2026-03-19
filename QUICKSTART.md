# 快速开始指南

本指南帮助你在 **15 分钟内**完成从零到运行的全部配置。

> **适用场景**：你想用 AI（OpenClaw、Cline、Claude 等）控制 Revit。

---

## 前置条件

| 项目 | 要求 |
|------|------|
| Revit | 2019 - 2024 任意版本 |
| Windows | 运行 Revit 的主机 |
| Visual Studio | 2019+（需安装 .NET 桌面开发） |

如果你想使用 **OpenClaw** 操作 Revit，还需要：

| 项目 | 要求 |
|------|------|
| Linux 主机 | 安装并运行 OpenClaw |
| Python | 3.10+ |
| 网络互通 | Linux 能访问 Windows 的 18181 端口 |

---

> **步骤概览**：
> - 步骤 1~2：安装框架和插件
> - 步骤 3：配置 MCP Client（本地使用）
> - 步骤 4~5：OpenClaw 集成（聊天工具使用，可选）

## 第一步：安装 Copilot 框架（Windows）

### 1.1 克隆仓库

```powershell
git clone https://github.com/ryanchan720/copilot-for-revit.git
cd copilot-for-revit
```

### 1.2 打开项目

用 Visual Studio 打开 `CopilotForRevit.sln`。

### 1.3 添加 Revit API 引用

右键 `Main` 项目 → 添加 → 引用 → 浏览：

- `C:\Program Files\Autodesk\Revit 20XX\RevitAPI.dll`
- `C:\Program Files\Autodesk\Revit 20XX\RevitAPIUI.dll`

> 将 `20XX` 替换为你安装的 Revit 版本。

### 1.4 编译

选择 `Release | x64` 配置，执行编译（Ctrl+Shift+B）。

> 默认针对 Revit 2019。如需其他版本，修改 `Main\Main.csproj` 第 16 行，或命令行传入 `/p:RevitVersion=20XX`。

### 1.5 部署

以**管理员身份**启动 PowerShell：

```powershell
cd Main\deploy
.\deploy.ps1
```

脚本参数：

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `-Build` | 编译配置：`Release` 或 `Debug` | `Release` |
| `-SetNetwork` | 配置远程访问（URL ACL + 防火墙） | 不启用 |

示例：

```powershell
# 默认部署（Release 配置）
.\deploy.ps1

# Debug 配置部署
.\deploy.ps1 -Build Debug

# Release 配置 + 远程访问
.\deploy.ps1 -SetNetwork

# Debug 配置 + 远程访问
.\deploy.ps1 -Build Debug -SetNetwork
```

脚本会自动：
- 检测本机已安装的所有 Revit 版本
- 复制文件到 `%ProgramData%\RevitCopilot\RevitAddinPlatform\`
- 为每个版本创建 `.addin` 清单
- （`-SetNetwork`）配置 URL ACL 和防火墙

> ⚠️ **安全提醒**：`-SetNetwork` 会让 MCP 服务监听所有网卡。如果你的 Windows 有公网 IP，请考虑通过防火墙规则限制访问来源 IP。

### 1.6 验证

打开 Revit，功能区应出现 **Revit Copilot** 选项卡。

---

## 第二步：安装通用命令插件（Windows）

通用插件提供开箱即用的常用命令：元素查询、参数修改、标注创建、视图管理等。

### 2.1 克隆仓库

```powershell
git clone https://github.com/ryanchan720/general-copilot-addins-for-revit.git
cd general-copilot-addins-for-revit
```

### 2.2 编译

1. 打开 `GeneralCopilotAddins.slnx`
2. 执行 NuGet 包还原
3. 添加 Revit API 引用（同上）
4. 选择 `Release | Any CPU` 配置编译

### 2.3 部署

将编译输出的文件夹复制到插件目录：

```
%ProgramData%\RevitCopilot\RevitAddinPlatform\Addins\
```

文件夹结构示例：

```
Addins/
├── Copilot.ElementCRUD/
│   ├── Copilot.ElementCRUD.dll
│   └── README.md
├── Copilot.Annotations/
│   └── ...
├── Copilot.View/
│   └── ...
└── Copilot.GeneralUtils/
    └── ...
```

---

## 第三步：配置 MCP Client（本地使用）

完成前两步后，Revit 已具备 MCP 服务能力。现在配置你的 AI 客户端连接它。

### 支持的 MCP Client

- [Cline](https://github.com/cline/cline)（VS Code 扩展）
- [Cherry Studio](https://www.cherry-ai.com/)（桌面应用）
- [Claude Desktop](https://claude.ai/download)
- 其他支持 MCP 协议的客户端

### 配置方式

MCP 服务地址：
- 本地访问：`http://localhost:18181/sse`
- 远程访问：`http://<WINDOWS_IP>:18181/sse`

### Cline 配置示例

1. 打开 VS Code，进入 Cline 扩展
2. 点击 MCP Servers 图标
3. 添加新服务器：
   - **Name**: `Revit Copilot`
   - **Type**: `SSE`
   - **URL**: `http://localhost:18181/sse`

### Cherry Studio 配置示例

1. 打开 Cherry Studio 设置
2. 进入 MCP Servers 页面
3. 添加服务器：
   - **名称**: `Revit Copilot`
   - **类型**: `SSE`
   - **地址**: `http://localhost:18181/sse`

### Claude Desktop 配置示例

编辑 Claude Desktop 配置文件（`%APPDATA%\Claude\claude_desktop_config.json`）：

```json
{
  "mcpServers": {
    "revit-copilot": {
      "url": "http://localhost:18181/sse"
    }
  }
}
```

### 验证

重启 MCP Client，在对话中尝试调用工具：

```
帮我查看当前 Revit 项目环境
```

如果返回 Revit 版本、项目名称等信息，说明配置成功。

---

## 第四步：安装 OpenClaw 桥接器（Linux）

> **何时需要**：你想在飞书、Telegram 等聊天工具中通过 OpenClaw 操作 Revit。
> 
> **如果只需本地使用 Cline/Claude 等 MCP 客户端，可跳过此步骤。**

### 4.1 安装依赖

```bash
# 安装 uv（Python 包管理器）
curl -LsSf https://astral.sh/uv/install.sh | sh

# 安装 Python 3.10+（如果没有）
# Ubuntu/Debian:
sudo apt install python3.12
```

### 4.2 克隆仓库

```bash
git clone https://github.com/ryanchan720/openclaw-bridge.git
cd openclaw-bridge
uv sync
```

### 4.3 配置环境变量

```bash
export REVIT_MCP_URL="http://<WINDOWS_IP>:18181"
```

将 `<WINDOWS_IP>` 替换为 Windows 主机的 IP 地址。

### 4.4 验证

```bash
uv run python -m openclaw_bridge.cli health
```

成功则返回：

```json
{
  "protocol_version": "2024-11-05",
  "server_info": {
    "name": "revit-copilot",
    "version": "1.0.0"
  },
  "status": "healthy"
}
```

---

## 第五步：配置 OpenClaw（Linux）

### 5.1 安装 Skill

**方式一：从 ClawHub 安装（推荐）**

```bash
openclaw skill install copilot-for-revit
```

**方式二：从 GitHub 安装**

```bash
git clone https://github.com/ryanchan720/copilot-for-revit-skill.git
cp -r copilot-for-revit-skill ~/.openclaw/workspace/skills/copilot-for-revit
```

### 5.2 配置环境变量

在 `~/.bashrc` 或 `~/.zshrc` 中添加：

```bash
# Revit MCP 服务地址（Windows 主机 IP）
export REVIT_MCP_URL="http://<WINDOWS_IP>:18181"

# openclaw-bridge 仓库路径（可选，默认 ~/repos/openclaw-bridge）
export OPENCLAW_BRIDGE_DIR="$HOME/repos/openclaw-bridge"
```

生效配置：

```bash
source ~/.bashrc
```

### 5.3 验证完整链路

在飞书/Telegram 中向 OpenClaw 发送：

```
Revit 在线吗？
```

应返回 Revit 状态信息。

---

## 常见问题

### 编译报错"找不到 RevitAPI.dll"

确认 Revit 已安装，且 `RevitVersion` 与本机版本一致。

### deploy.ps1 提示"跳过（未安装）"

脚本通过检测 Revit.exe 判断版本安装状态。未安装的版本自动跳过，属正常现象。

### Linux 连不上 Windows 的 MCP 服务

检查：
1. Windows 防火墙是否放行 18181 端口
2. URL ACL 是否配置正确
3. 两台主机是否网络互通（ping 测试）

### MCP 启动报 "Access is denied"

URL ACL 未配置或权限不足。运行：

```powershell
.\deploy.ps1 -SetNetwork
```

---

## 下一步

- **查看可用命令**：[general-copilot-addins-for-revit](https://github.com/ryanchan720/general-copilot-addins-for-revit#功能概览)
- **开发自定义命令**：[copilot-addins-for-revit](https://github.com/ryanchan720/copilot-addins-for-revit)
- **架构与原理**：[copilot-for-revit README](https://github.com/ryanchan720/copilot-for-revit)
