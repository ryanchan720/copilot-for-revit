# Revit Copilot 安装体系设计指南

本文档集定义了 Revit Copilot 新安装/部署体系的设计基线，为后续实施提供统一蓝图。

---

## 文档结构

| 文档 | 定位 | 读者 |
|------|------|------|
| **GUIDE.md**（本文档） | 总览与导航 | 所有人 |
| [DESIGN.md](DESIGN.md) | 架构设计与分层模型 | 架构师、开发者 |
| [FRAMEWORK.md](FRAMEWORK.md) | 实施框架与产物定义 | 开发者、测试者 |
| [PROGRESS.md](PROGRESS.md) | 阶段规划与进度跟踪 | 项目管理者、开发者 |

---

## 背景与目标

### 当前问题

1. **安装路径不统一**：同时存在 `deploy.ps1` 脚本部署和 `Setup.vdproj` MSI 安装包两种方式，用户易混淆。
2. **权限要求不清晰**：`deploy.ps1` 要求管理员权限，但文档说明不足。
3. **版本管理缺失**：无统一版本号、无升级机制、无卸载流程。
4. **插件安装分散**：用户需手动复制插件目录，无验证、无依赖管理。
5. **网络配置复杂**：`-SetNetwork` 参数涉及 URL ACL 和防火墙，用户易出错。
6. **开发者体验差**：需手动添加 Revit API 引用、手动修改 csproj 版本号。

### 目标

建立**分层、可扩展、用户友好**的安装体系：

- **普通用户**：一键安装、自动检测 Revit、开箱即用。
- **高级用户**：支持自定义路径、网络配置、插件管理。
- **开发者**：提供开发环境快速搭建工具、调试支持。

---

## 核心设计原则

1. **分层隔离**：Runtime / Deploy Core / Installer Shell 三层清晰分离。
2. **单一产物**：每个仓库产出单一、明确的发布产物。
3. **幂等操作**：安装、升级、卸载均可重复执行，无副作用。
4. **渐进增强**：基础功能零配置，高级功能按需启用。
5. **自包含**：安装产物包含所有依赖，无需额外下载。

---

## 快速导航

### 我想了解...

- **整体架构** → [DESIGN.md - 分层模型](DESIGN.md#分层模型)
- **各仓库职责** → [DESIGN.md - 仓库职责边界](DESIGN.md#仓库职责边界)
- **发布产物定义** → [FRAMEWORK.md - 标准发布产物](FRAMEWORK.md#标准发布产物)
- **安装流程** → [FRAMEWORK.md - 安装流程](FRAMEWORK.md#安装流程)
- **当前进度** → [PROGRESS.md](PROGRESS.md)

### 我想实施...

- **Phase 1：基础安装器** → [PROGRESS.md - Phase 1](PROGRESS.md#phase-1-基础安装器)
- **Phase 2：插件管理** → [PROGRESS.md - Phase 2](PROGRESS.md#phase-2-插件管理)
- **Phase 3：升级与卸载** → [PROGRESS.md - Phase 3](PROGRESS.md#phase-3-升级与卸载)

---

## 术语表

| 术语 | 定义 |
|------|------|
| **Runtime** | Revit Copilot 核心运行时，包含 Main.dll、SharedLibrary.dll 及依赖。 |
| **Deploy Core** | 部署核心逻辑，负责文件复制、注册表写入、.addin 文件生成。 |
| **Installer Shell** | 用户交互层，提供 GUI/CLI 界面、参数解析、错误提示。 |
| **Plugin Package** | 插件包，包含命令 DLL、README.md、依赖项的目录。 |
| **Manifest** | 清单文件，描述产物版本、支持的 Revit 版本、依赖关系。 |

---

## 相关资源

- [README.md](../README.md) - 项目总览
- [QUICKSTART.md](../QUICKSTART.md) - 快速开始指南
- [Main/deploy/deploy.ps1](../Main/deploy/deploy.ps1) - 当前部署脚本
- [Setup/Setup.vdproj](../Setup/Setup.vdproj) - 当前 MSI 安装包项目
