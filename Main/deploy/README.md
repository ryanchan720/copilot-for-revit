# Deploy 使用说明（整机级部署）

本目录提供 `deploy.ps1`，用于将编译产物部署到**整台机器**，并为已安装的 Revit 2019~2024 写入 `ProgramData` 级别的 `.addin` 文件。

---

## 1. 部署策略说明

- 插件文件安装到：
  - `%ProgramData%\RevitCopilot\RevitAddinPlatform`
- `.addin` 写入到：
  - `%ProgramData%\Autodesk\Revit\Addins\2019\RevitAddinPlatform.addin`
  - ...
  - `%ProgramData%\Autodesk\Revit\Addins\2024\RevitAddinPlatform.addin`
- 脚本会自动删除用户级同名 `.addin`（`%APPDATA%\Autodesk\Revit\Addins\{year}`），避免加载优先级冲突。

> 注意：该模式需要管理员权限。

---

## 2. 前置条件

1. 已安装至少一个 Revit 版本（2019~2024）。
2. 项目已完成编译，默认使用 `Release|x64`。
3. 使用**管理员身份**打开 PowerShell。

---

## 3. 标准部署流程

在仓库根目录执行：

```powershell
cd Main
# 先编译（Visual Studio 或 msbuild）
```

编译完成后执行部署：

```powershell
cd deploy
.\deploy.ps1
```

如果你要部署 Debug 产物：

```powershell
.\deploy.ps1 -Build Debug
```

> `-Build` 参数对大小写不敏感，`Debug` / `debug` / `DEBUG` 都可用。

---

## 4. 脚本会做什么

`deploy.ps1` 会按顺序执行：

1. 校验管理员权限。  
2. 校验构建产物是否存在（`bin\{Build}\Main.dll`）。  
3. 复制 `bin\{Build}` 下文件与子目录到 `%ProgramData%\RevitCopilot\RevitAddinPlatform`。  
4. 检测已安装 Revit（通过 `C:\Program Files\Autodesk\Revit {year}\Revit.exe`）。  
5. 为已安装版本写入 `%ProgramData%\Autodesk\Revit\Addins\{year}\RevitAddinPlatform.addin`。  
6. 删除用户目录下同名 `.addin`（如存在）。

---

## 5. 部署后验证

1. 检查目标文件是否存在：
   - `%ProgramData%\RevitCopilot\RevitAddinPlatform\Main.dll`
2. 检查 `.addin` 是否已写入对应年份目录：
   - `%ProgramData%\Autodesk\Revit\Addins\{year}\RevitAddinPlatform.addin`
3. 启动对应 Revit 版本，确认功能区出现 `Revit Copilot`。

---

## 6. 常见问题

### Q1: 提示需要管理员权限
请右键 PowerShell，选择“以管理员身份运行”，再执行脚本。

### Q2: 提示 `Build output not found`
说明还没有对应配置的构建产物。请先编译：
- 默认：`Release|x64`
- Debug 部署：`Debug|x64`

### Q3: 某个版本显示 `Skipped (not installed)`
这是正常行为，表示该 Revit 年份未检测到安装。

---

## 7. 回滚 / 卸载（手动）

如需手动回滚：

1. 删除安装目录：
   - `%ProgramData%\RevitCopilot\RevitAddinPlatform`
2. 删除所有年份 `.addin` 文件：
   - `%ProgramData%\Autodesk\Revit\Addins\{year}\RevitAddinPlatform.addin`

如后续有 Setup 安装包，建议将上述流程接入安装/卸载动作自动执行。
