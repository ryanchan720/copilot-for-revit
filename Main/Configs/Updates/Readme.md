此文件夹用于存储与应用程序更新相关的信息文件，包括：
- `update-info.json` 文件，用于更新版本号和指定更新包路径。本文件在分发时不需要拷贝到客户机，而是拷贝至指定的公共路径，客户机启动 Revit 时将访问此文件以检查是否有新版本可用
- `update-replace.bat` 文件，用于程序更新。客户机在比对版本发现需要更新并在 Revit 关闭后，自动执行该批处理文件以完成更新操作

更新步骤：
1. 升级 `AssemblyInfo.cs` 中 `AssemblyFileVersion` 版本号
1. 切换至 Release 模式，编译项目
1. 将生成的更新包（即整个 `Release` 目录）压缩为 Release.zip
1. 对应修改 `update-info.json` 文件中的版本号和更新包路径
1. 将 `Release.zip` 和 `update-info.json` 上传至公共路径
1. 后续客户机启动 Revit 时将自动检测并执行更新

注意事项：
- 确保 `update-info.json` 中的版本号与 `AssemblyFileVersion` 保持一致
- `User` 和 `Addins` 文件夹中的内容不应包含在更新包中，其内容是用户配置或数据，不能被覆盖