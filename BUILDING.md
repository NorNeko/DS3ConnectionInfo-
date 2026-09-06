# 构建与发行

## 环境

- Windows x64
- Visual Studio 2022 或更新版本，或对应的 Build Tools
- “.NET 桌面生成工具”工作负载及 .NET Framework 4.7.2 targeting pack
- PowerShell 5.1 或更高版本
- 仓库内已还原的 `packages/` 依赖

## 构建单文件发行版

在仓库根目录运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-release.ps1
```

脚本先以 Release 配置构建应用并执行 ILRepack，再把主程序、`steam_api64.dll` 和 ETW 的 x64 原生组件嵌入单文件启动器。产物写入 `artifacts/release/`，该目录已由 `.gitignore` 排除，不应提交到源码仓库。

启动器首次运行时将内部文件解压到 `%LOCALAPPDATA%\DS3ConnectionInfo\v4.5.0-cn.1\`，以后会校验 SHA-256 并复用相同文件。实际应用仍按原设计请求管理员权限，以便读取 ETW 网络事件。

## 回归检查

```powershell
MSBuild.exe .\tests\DS3ConnectionInfo.Tests.csproj /t:Rebuild /p:Configuration=Debug
.\tests\bin\Debug\DS3ConnectionInfo.Tests.exe "$PWD" "$PWD\tests\bin\Debug\overlay-preview.png"
```

联机与游戏内验证范围见 [玩家属性显示说明](docs/player-attributes.md)。
