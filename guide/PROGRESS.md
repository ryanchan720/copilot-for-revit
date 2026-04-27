# 阶段规划与进度跟踪

本文档定义新安装体系的实施阶段、具体任务与进度。

---

## 总览

| 阶段 | 目标 | 预计工期 | 状态 |
|------|------|----------|------|
| **Phase 0** | 设计基线文档 | 1 周 | ✅ 已完成 |
| **Phase 1** | 基础安装器 | 2-3 周 | 🔄 进行中 |
| **Phase 2** | 插件管理 | 1-2 周 | ⏳ 待开始 |
| **Phase 3** | 升级与卸载 | 1-2 周 | ⏳ 待开始 |
| **Phase 4** | 开发者工具 | 1 周 | ⏳ 待开始 |
| **Phase 5** | 文档与发布 | 1 周 | ⏳ 待开始 |

---

## Phase 0: 设计基线

**目标**：建立设计基线文档，为后续实施提供统一蓝图。

**状态**：✅ 已完成

**产出**：
- [x] `guide/GUIDE.md` - 总览与导航
- [x] `guide/DESIGN.md` - 架构设计与分层模型
- [x] `guide/FRAMEWORK.md` - 实施框架与产物定义
- [x] `guide/PROGRESS.md` - 阶段规划与进度跟踪

**完成时间**：2026-04-27

---

## Phase 1: 基础安装器

**目标**：实现基础安装功能，替代当前 `deploy.ps1` 和 `Setup.vdproj`。

**预计工期**：2-3 周

### 任务清单

#### 1.1 Runtime 包构建（3 天）

- [ ] 创建 `manifest.json` 模板
- [ ] 编写打包脚本 `scripts\package-runtime.ps1`
- [ ] 验证打包产物完整性
- [ ] 测试在不同 Revit 版本上的兼容性

**产出**：`RevitCopilot-Runtime-{version}.zip`

---

#### 1.2 Deploy Core 类库开发（5 天）

- [x] 创建 `DeployCore` 项目（.NET Standard 2.0）
- [x] 实现 `IDeployCore` 接口
  - [x] `InstallRuntime()`
  - [x] `DetectRevitVersions()`
  - [x] `RegisterRevitVersion()`
  - [x] `UnregisterRevitVersion()`
- [x] 编写单元测试（27 个测试，覆盖全部 4 个核心 API）
- [ ] 集成测试（在干净虚拟机中）

**产出**：`DeployCore.dll`

---

#### 1.3 CLI 工具开发（3 天）

- [x] 创建 `RevitCopilot.CLI` 项目（net8.0，引用 DeployCore）
- [x] 实现核心命令
  - [x] `install` - 安装 Runtime（支持 --source、--target、--revit-versions、--no-overwrite）
  - [x] `doctor` - 自检（支持 --json 输出）
  - [x] `uninstall` - 卸载（支持 --revit-version 和 --all，仅移除 .addin 文件）
- [x] 错误码映射（DeployErrorCode → exit code 0-10）
- [x] 命令行帮助信息（--help、--version）
- [x] 构建验证（0 warnings, 27 existing tests pass）
- [ ] 集成测试（在 Windows + Revit 环境中）

**产出**：`revit-copilot.exe`（.NET 8 console app）

---

#### 1.4 GUI 安装器重构（4 天）

- [ ] 评估当前 `Setup.vdproj` 可用性
- [ ] 决定：重构 MSI 或迁移到 WiX Toolset
- [ ] 集成 Deploy Core
- [ ] 实现版本检测 UI
- [ ] 实现安装选项 UI
- [ ] 测试安装、卸载流程

**产出**：`RevitCopilot-Installer-{version}.msi`

---

#### 1.5 网络配置功能（2 天）

- [ ] 实现 `ConfigureNetwork()` API
- [ ] 实现 `RemoveNetworkConfig()` API
- [ ] CLI 命令：`config network`
- [ ] 测试 URL ACL 配置
- [ ] 测试防火墙规则配置

---

#### 1.6 迁移与兼容性（2 天）

- [ ] 编写迁移指南（从旧版本升级）
- [ ] 测试与现有 `deploy.ps1` 的兼容性
- [ ] 保留 `deploy.ps1` 作为降级方案（调用 Deploy Core）

---

### 验收标准

- [ ] 用户可通过 MSI 或 CLI 完成 Runtime 安装。
- [ ] 安装后 Revit 可正常加载插件。
- [ ] 支持所有 Revit 版本（2019-2024）。
- [ ] 网络配置功能正常。
- [ ] 卸载后无残留文件。
- [ ] 文档清晰，用户可自助完成安装。

---

## Phase 2: 插件管理

**目标**：实现插件包标准化、安装、卸载、列表功能。

**预计工期**：1-2 周

### 任务清单

#### 2.1 插件包格式定义（2 天）

- [ ] 定义 `manifest.json` schema
- [ ] 编写插件包打包脚本
- [ ] 更新 `general-copilot-addins-for-revit` 构建流程
- [ ] 示例插件包文档

**产出**：标准 Plugin Package 格式

---

#### 2.2 插件管理 API（3 天）

- [ ] 实现 `InstallPlugin()`
- [ ] 实现 `UninstallPlugin()`
- [ ] 实现 `ListPlugins()`
- [ ] 实现依赖检查逻辑
- [ ] 单元测试

---

#### 2.3 CLI 插件命令（2 天）

- [ ] `plugin install`
- [ ] `plugin uninstall`
- [ ] `plugin list`
- [ ] `plugin update`

---

#### 2.4 默认插件包集成（2 天）

- [ ] 打包 `general-copilot-addins-for-revit` 为标准包
- [ ] MSI 集成默认插件包安装选项
- [ ] 测试默认插件包安装流程

---

### 验收标准

- [ ] 插件包格式标准化，包含 manifest.json。
- [ ] 用户可通过 CLI 安装、卸载、列出插件。
- [ ] 默认插件包可正常安装和使用。
- [ ] 插件依赖检查正常。

---

## Phase 3: 升级与卸载

**目标**：实现 Runtime 和插件的升级、完整卸载功能。

**预计工期**：1-2 周

### 任务清单

#### 3.1 Runtime 升级（3 天）

- [ ] 实现 `UpgradeRuntime()`
- [ ] 版本兼容性检查
- [ ] 备份与回滚（可选）
- [ ] CLI 命令：`upgrade`

---

#### 3.2 插件升级（2 天）

- [ ] 实现插件版本检查
- [ ] 实现插件升级逻辑
- [ ] CLI 命令：`plugin update`

---

#### 3.3 完整卸载（2 天）

- [ ] 实现 `UninstallRuntime()`
- [ ] 清理所有相关文件、注册表
- [ ] 移除网络配置
- [ ] CLI 命令：`uninstall --clean-all`

---

#### 3.4 自检工具增强（2 天）

- [ ] 增强 `doctor` 命令
- [ ] 检查文件完整性
- [ ] 检查配置一致性
- [ ] 提供修复建议

---

### 验收标准

- [ ] Runtime 可正常升级到新版本。
- [ ] 插件可正常升级。
- [ ] 卸载后无残留。
- [ ] 自检工具可检测常见问题。

---

## Phase 4: 开发者工具

**目标**：提供开发环境快速搭建工具，改善开发者体验。

**预计工期**：1 周

### 任务清单

#### 4.1 开发环境搭建脚本（3 天）

- [ ] `scripts\setup-dev.ps1`
- [ ] 自动添加 Revit API 引用
- [ ] 自动配置编译选项
- [ ] 测试脚本在不同环境下的表现

---

#### 4.2 本地部署脚本（2 天）

- [ ] `scripts\deploy-local.ps1`
- [ ] 快速部署到本地 Revit
- [ ] 支持热重载（开发时）

---

#### 4.3 打包脚本（2 天）

- [ ] `scripts\package.ps1`
- [ ] 一键打包 Runtime、CLI、MSI
- [ ] 版本号自动管理

---

### 验收标准

- [ ] 开发者可在 5 分钟内搭建开发环境。
- [ ] 本地部署脚本正常工作。
- [ ] 打包脚本生成完整产物。

---

## Phase 5: 文档与发布

**目标**：完善文档、发布第一版新安装体系。

**预计工期**：1 周

### 任务清单

#### 5.1 用户文档（3 天）

- [ ] 更新 `README.md`
- [ ] 更新 `QUICKSTART.md`
- [ ] 编写安装指南（GUI、CLI）
- [ ] 编写升级指南
- [ ] 编写卸载指南
- [ ] 编写故障排查指南

---

#### 5.2 开发者文档（2 天）

- [ ] 编写插件开发指南
- [ ] 编写插件打包指南
- [ ] 编写贡献指南

---

#### 5.3 发布准备（2 天）

- [ ] 准备 GitHub Release
- [ ] 准备发布说明（Release Notes）
- [ ] 测试发布产物
- [ ] 发布到 GitHub Releases

---

### 验收标准

- [ ] 文档清晰、完整、可执行。
- [ ] 发布产物完整、可用。
- [ ] 用户可按文档完成安装、升级、卸载。

---

## 风险与依赖

| 风险/依赖 | 影响 | 缓解措施 |
|----------|------|----------|
| Revit API 版本差异 | 编译失败 | 提供多版本编译脚本 |
| MSI 工具学习曲线 | 延期 | 考虑使用 WiX Toolset（文档丰富） |
| 测试环境不足 | 测试不充分 | 使用虚拟机、CI/CD 自动化测试 |
| 用户迁移问题 | 用户流失 | 提供迁移工具、详细指南 |

---

## 下一步行动

**Phase 1 切入点**：

1. **优先级 1**：创建 `DeployCore` 类库项目，实现核心 API。
   - 理由：所有后续工作依赖此基础。

2. **优先级 2**：实现 Runtime 包构建脚本。
   - 理由：需要产物进行测试。

3. **优先级 3**：开发 CLI 工具基础命令（`install`, `doctor`）。
   - 理由：快速验证 Deploy Core 功能，提供早期可用工具。

4. **优先级 4**：重构 MSI 安装器。
   - 理由：GUI 安装器是普通用户主要入口，但可延后。

**建议顺序**：
1. 创建 `DeployCore` 项目骨架
2. 实现 `DetectRevitVersions()` 和 `InstallRuntime()`
3. 编写 Runtime 包构建脚本
4. 开发 CLI `install` 命令
5. 测试端到端安装流程
6. 实现 `RegisterRevitVersion()`
7. 开发 CLI `doctor` 命令
8. 重构 MSI 安装器
9. 实现网络配置功能
10. 完善文档

---

## 更新日志

- **2026-04-27**：Phase 0 完成，创建设计基线文档。
- **2026-04-27**：Phase 1.2 Deploy Core 类库骨架完成。创建 `DeployCore` 项目（netstandard2.0），实现 `IDeployCore` 接口及 `DeployCoreService`，包含 `DetectRevitVersions()`、`InstallRuntime()`、`RegisterRevitVersion()`、`UnregisterRevitVersion()` 四个核心 API。
- **2026-04-27**：Phase 1.2 单元测试初版完成。创建 `DeployCore.Tests` 项目（xUnit / net8.0），为全部 4 个核心 API 编写 27 个测试。对 `DeployCoreService` 做最小可测试性改造（注入 `programFilesPath` / `programDataPath` 构造函数参数）。
- **2026-04-27**：Phase 1.3 CLI 工具初版完成。创建 `RevitCopilot.CLI` 项目（net8.0），实现 `install`、`doctor`、`uninstall` 三个核心命令。手工参数解析，无第三方依赖。错误码映射 DeployErrorCode → exit code。支持 `--json` 输出便于脚本化。
