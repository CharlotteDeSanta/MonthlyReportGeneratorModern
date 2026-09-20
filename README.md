# 科烽智能AGV月报生成工具（WinUI 3 版）

面向 AGV 项目实施工程师的月报 / 日报 / 周报填写与 XLSX 导出工具。本仓库是 WPF 版的全新 **WinUI 3** 重写版，业务逻辑与导出数值与原版完全一致。

## ✨ 功能特性

- **月报**：整月明细（地点 / 多行工作内容 / 上下班时间下拉）、工时 / 加班 / 项目出勤自动计算（跨午夜、补班日、调休混合规则）、休息日自动红底、月度汇总统计与项目地点汇总实时刷新
- **日报**：实施记录日报，今日完成 / 明日计划 / 技术负责人 / 实施人员，导出含跨行合并与休息日红底
- **周报**：ISO 周（52/53 动态）、本周总结 / 下周计划两张明细表、元信息齐全
- **导出 XLSX**：数值与黄金样本逐项一致（见[回归测试](#-回归测试)）
- **数据安全**：草稿每 10 秒自动保存，切换月份 / 周次自动落盘；**首启自动迁移旧 WPF 版本地数据**
- **节假日**：内置 2025/2026 官方节假日与补班日，其他年份联网刷新并缓存，失败自动回退

## 🛠 技术栈

| 组件 | 版本 |
|---|---|
| .NET | 10.0（`net10.0-windows10.0.19041.0`） |
| Windows App SDK | 2.5.1（打包式 MSIX） |
| CommunityToolkit.WinUI.UI.Controls.DataGrid | 7.1.2 |
| ClosedXML | 0.105.1 |

## 🚀 构建与运行

要求：Windows 10 1809+ / Windows 11，Visual Studio 2022（含 Windows 应用开发负载）或 .NET SDK 10。

```powershell
# 常规构建（打包）
dotnet build -c Release -p:Platform=x64 -p:PublishTrimmed=false

# 未打包方式运行（调试冒烟用）
dotnet build -c Release -p:Platform=x64 -p:PublishTrimmed=false -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true
```

- 在 VS 中直接 F5 / “开始执行，不调试”即可以打包方式运行
- 生成 MSIX 安装包：VS 右键项目 → 打包和发布 → 创建应用包（旁加载）

## 📁 项目结构

```
MonthlyReportGeneratorModern/
├── Models/          # 业务模型（工时/加班/出勤/ISO周/工作日历）
├── Services/        # 导出布局与渲染、草稿、节假日、对话框
├── ViewModels/      # 月报/日报/周报/壳层 VM（MVVM）
├── Views/           # 三个页面 + 窗口壳（WinUI XAML）
├── Tests/ExportSmokeTest/   # 黄金样本回归（控制台）
├── Assets/          # 应用图标
└── MIGRATION.md     # WPF → WinUI 3 移植全程记录（含全部坑位）
```

## 🧪 回归测试

`Tests\ExportSmokeTest` 链接主项目 Models/Services 源码，按黄金样本（2026-09）录入后断言六项汇总 `6.0 / 6.00 / 52.68 / 12.68 / 1.58 / 1.0` 与地点汇总 `无锡国药 6.0 / 12.68`，并回读 XLSX 校验数值与存储格式：

```powershell
dotnet run --project Tests\ExportSmokeTest -c Release
```

## 📦 发布

- GitHub Releases：见各版本 [Release Notes](RELEASE_NOTES_v1.0.0.md)
- 微软商店公开上架规划中（见 `MIGRATION.md` §10）

## 📄 许可证

见 [LICENSE.txt](LICENSE.txt)。
