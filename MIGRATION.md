# MonthlyReportGenerator WinUI 3 移植需求文档（Agent 交接版）

> 本文档供没有历史上下文的新 Agent 会话继续移植工作使用。按章节顺序执行即可。
> 最后更新：2026-09（项目骨架已建立，进入移植阶段）

## 1. 背景与目标

- **源项目（WPF 版，已完成且稳定）**：`C:\Users\20953\Documents\DesktopProjects\MonthlyReportGenerator`（git 仓库，分支 `main`）。.NET 10 WPF + WPF-UI 4.3 + ClosedXML 0.105.1，三标签页（月报/日报/周报）+ XLSX 导出，全部业务逻辑已验收。**这是移植的唯一事实来源**，移植时逐文件对照。
- **目标项目（WinUI 3 版）**：`C:\Users\20953\Documents\DesktopProjects\MonthlyReportGeneratorModern`（独立 git 仓库，分支 `master`）。Windows App SDK 2.5.1、net8.0-windows10.0.19041.0（模板生成）、打包式（MSIX）、Mica 背景。
- **发布目标**：微软商店**公开上架**（个人开发者账户）；上线前先用 GitHub Releases 分发当前 WPF exe（EXE/MSI 商店渠道）。
- **验收标准**：WinUI 3 版功能与 WPF 版一致，导出 XLSX 数值与黄金样本完全一致。

## 2. 立即要做的基础修正（动移植代码前）

1. `MonthlyReportGeneratorModern.csproj`：`TargetFramework` 改为 **`net10.0-windows10.0.19041.0`**（源项目是 net10，本机 SDK 10.0.400 已装）；
2. `Package.appxmanifest`：
   - `Properties/DisplayName` 与 `uap:VisualElements/DisplayName` 改为 **科烽智能AGV月报生成工具**；
   - `Capabilities` 增加 **`<Capability Name="internetClient" />`**（节假日 API 联网，商店认证必查）；可删除 `systemAIModels`；
   - 包标识（Identity）与发布者暂不动——之后用 VS"关联商店"自动写入；
3. 首启 **Release 配置**做一次 `PublishTrimmed` 验证：写一个最小导出（见第 8 节 ClosedXML 风险）确认裁剪不会破坏导出；若崩溃，把 `PublishTrimmed` 改为 `False`；
4. 建议 `git commit` 记录骨架基线。

## 3. 源项目功能清单（逐条对照移植）

### 3.1 窗口壳
- 自绘标题栏（标题"科烽智能AGV月报生成工具"+ 应用图标）；Mica/主题；
- 公共区：姓名、工程师等级（下拉：实习工程师/新进工程师/初级工程师/中级工程师/高级工程师）——三页共享，变更即持久化；
- 三个标签（月报/日报/周报），**页面视图常驻 + 仅切换 Visibility**，页面 VM 懒创建 + **空闲时段分帧预构建**（消除首次切换顿挫）。

### 3.2 月报页（源：`Views/MonthlyReportView.*`、`ViewModels/MonthlyReportViewModel.cs`）
- 年份（2026–2035）/月份选择；标题预览"YYYY年AGV项目工作月报"；
- 整月明细表：日期(只读)、星期(只读)、上班地点(文本)、工作内容(多行，Enter 换行)、上班/下班时间（**小时+分钟两个下拉**，00–23/00–59，单击即展开，聚焦预填 09:00/17:00，列尾"×"单击清空单条时间）、工作时长/加班时长/项目出勤(只读派生)；
- 休息日行红底（`#FEE2E2`，周末/法定节假日且非补班日）；
- 底部两张实时汇总表：月度汇总统计（6 项）+ 项目地点汇总；
- 工具栏：导出 XLSX、清空表单；清空=整表重建；每 10 秒自动存草稿；
- 校验：姓名必填、时间格式 HH:mm（跨午夜合法）。

### 3.3 日报页（源：`Views/DailyReportView.*`、`ViewModels/DailyReportViewModel.cs`）
- 年份/月份选择；整月表格（每天一行记录）：日期、星期、今日完成(多行)、明日计划(多行)、项目技术负责人、项目实施人员（后两列默认=公共区姓名）；
- 导出"实施记录日报"：5 列，每天两行（今日完成/明日计划），日期与人员列跨两行合并，休息日日期格红底。

### 3.4 周报页（源：`Views/WeeklyReportView.*`、`ViewModels/WeeklyReportViewModel.cs`）
- 年份/周数选择（**ISO 周，周一开头**，按年动态 52/53 周，显示周范围"YYYY-Www（MM-dd ~ MM-dd）"）；
- 元信息两行：项目名称、填写人、是否按上周计划完成(是/否下拉)、未完成项、填表日期(自动今天，加粗)；
- 两个 7 行表格：本周工作总结 / 下周工作计划，列=星期(含日期,只读)/类别/工作内容(多行)/阶段要点/需要协调的内容/计划完成时间/进度状态(自由文本)/备注；
- 导出"周工作总结计划"：8 列，两区块标签在首列跨 7 行合并。

### 3.5 月报业务规则（与旧网页版系统一致，必须精确复刻）

| 指标 | 规则 |
|---|---|
| 工作时长(h) | 下班−上班；跨午夜 +24h；2 位小数 |
| 加班时长(h) | 周末/法定节假日(含补班周末)→全部时长；工作日混合调休→超 4h；其余→超 8h（8 小时制） |
| 项目出勤 | 纯调休/休息或"公司"→0；"公司/项目"→0.5；其余按时长 <4h→0、4~8h→0.5、≥8h→1 |
| 实际上班天数 | ≥8h→1、4~8h→0.5、<4h→0；排除纯调休/休息 |
| 项目出勤天数 | Σ项目出勤 |
| 总工时/总加班 | Σ |
| 累计可调休天数 | 总加班÷8，2 位小数（银行家舍入：1.585→1.58） |
| 已调休天数 | 仅工作日统计：纯调休+1、混合+0.5 |
| 项目地点汇总 | 每行第一个项目名，全额计入；按项目名文化排序升序 |

地点规则：`公司`/`项目名`/`调休`/`休息`（调休与休息同义）/`公司/项目名`/`调休/项目名`。

### 3.6 草稿与配置（本地 JSON，`DraftService`）
- WPF 版路径 `%LocalAppData%\MonthlyReportGenerator\`；**WinUI 3 打包版路径为 `%LocalAppData%\Packages\<包标识>\LocalState\`**；
- 文件：`profile.json`、`draft-{yyyy-MM}.json`（月报）、`draft-daily-{yyyy-MM}.json`、`draft-weekly-{yyyy}-W{ww}.json`、`holidays-{yyyy}.json`；
- **必须做一次性迁移**：首启检测旧目录，存在则把上述文件拷入新 `LocalState`（WinUI 3 打包应用是完全信任桌面应用，可直接读旧路径）。

### 3.7 节假日（`WorkCalendar` + `HolidayService`）
- 内置 2025/2026 年节假日与补班日；其他年份启动时联网 `https://timor.tech/api/holiday/year/{year}` 刷新并缓存，失败回退缓存/仅周末；加载完成后刷新各行派生值（加班/出勤/行高亮）。

## 4. 分层移植清单（文件级）

源目录 = WPF 版 `MonthlyReportGenerator/`；目标目录 = `MonthlyReportGeneratorModern/`。

| 源文件 | 处理方式 | 注意事项 |
|---|---|---|
| `Models/*.cs`（BindableBase、DailyEntry、EngineerLevel、ReportMeta、ReportLine、SummaryValues、WorkCalendar、WeekHelper、DailyDayEntry、WeekDayEntry） | **直接拷贝** | 纯 .NET，应零修改；命名空间保留 `MonthlyReportGenerator.Models` 或统一改为 Modern 根命名空间 |
| `Services/ReportLayoutBuilder.cs`、`DailyReportLayoutBuilder.cs`、`WeeklyReportLayoutBuilder.cs`、`LayoutUtil.cs`、`XlsxExportService.cs`、`ReportExporter.cs`、`DraftService.cs`、`HolidayService.cs` | **拷贝+微调** | 无 UI 依赖；`DraftService.Folder` 改为 `ApplicationData.Current.LocalFolder.Path`（Windows.Storage）；`HolidayService` 的 HttpClient 不变 |
| `ViewModels/Infrastructure.cs`（ObservableObject/RelayCommand/SummaryItem） | 拷贝或换 CommunityToolkit.Mvvm | 手写版可直接用（System.ComponentModel 通用） |
| `ViewModels/ProfileViewModel.cs`、`ShellViewModel.cs`、`MonthlyReportViewModel.cs`、`DailyReportViewModel.cs`、`WeeklyReportViewModel.cs` | **改写 UI 触点** | 见第 5 节 API 映射 |
| `ViewModels/ExportHelper.cs` | 拆分 | 保留错误日志逻辑；对话框部分改 FileSavePicker |
| `Views/*` 与 `MainWindow` | **全部重写** | XAML 方言不同 |

## 5. WPF → WinUI 3 API 映射（本项目实际用到的）

| WPF | WinUI 3 | 备注 |
|---|---|---|
| `MessageBox.Show` | `ContentDialog`（`XamlRoot` 必须设置，异步 await） | 所有弹窗点都要改 |
| `SaveFileDialog` | `FileSavePicker` + `WinRT.Interop.InitializeWithWindow(hwnd, picker)` | 窗口句柄取 `WinRT.Interop.WindowNative.GetWindowHandle(window)` |
| `DispatcherTimer` | `DispatcherQueueTimer` | |
| `DataGrid`（WPF 内置） | **无内置！** 用 `CommunityToolkit.WinUI.UI.Controls.DataGrid`（NuGet 包） | 见第 6 节 spike |
| `ui:ToggleButton` 标签 | `SelectorBar` / `Pivot` / `NavigationView` | 推荐 SelectorBar（顶部分段式） |
| 自绘标题栏（WPF-UI FluentWindow） | `Window.ExtendsContentIntoTitleBar` + `AppWindow.TitleBar` | |
| 弹层预热 + AutomationPeer 禁用（PerformanceDataGrid） | **不需要**（#5807/#9881 是 WPF 内部问题）；但首次弹层体验建议在低配机实测 | |
| `ObservableCollection`/INPC | 通用，不变 | |
| 保存设置对话框初始目录 | `PickerLocationId.DocumentsLibrary` | |

## 6. Spike 验证清单（先于正式页面开发，四项全过再动 UI）

在临时页面验证，通过标准写明：

1. **DataGrid**（CommunityToolkit）：模板列内 ComboBox/TextBox 编辑、单击进入编辑、行背景样式（红底）、固定行高、7/31 行性能；
2. **FileSavePicker**：InitializeWithWindow + 默认文件名（`FileSavePicker.SuggestedFileName`）；
3. **自定义标题栏**：`ExtendsContentIntoTitleBar` + 应用图标显示；
4. **时间选择**：小时+分钟双 ComboBox（与 WPF 版交互一致）vs `TimePicker` 取舍——**交互一致性优先**。

## 7. 性能要点（从 WPF 版沉淀的经验，照搬）

- 三页视图**常驻 + Visibility 切换**，禁止每次切换重建页面（WPF 版曾因此卡顿）；
- 页面 VM 懒创建 + **启动后空闲时段分帧预构建**日报/周报页；
- WinUI DataGrid 天然虚拟化——行内控件的性能画像与 WPF 版不同，低配机（i5-8265U）实测；
- 导出、草稿加载保持轻量；节假日网络请求异步。

## 8. 已知坑与注意事项（本会话历史沉淀）

1. **`PublishTrimmed` × ClosedXML**：Release 默认开启裁剪，ClosedXML 大量反射，**首发前必须验证导出**；异常则 `PublishTrimmed=false`；
2. 数字格式：ClosedXML 在"未物化样式"单元格上 `Style.NumberFormat` 可能为 null，赋 `.Format` 前判空，失败退化为写格式化文本（源代码已处理，拷贝时保留）；
3. XLSX 合并单元格：`MergeDownCols/MergeDownCount` 由布局构建器给出、渲染器执行；合并区域内侧边框可能残留，验收时检查；
4. 字符串单元格数组默认元素为 null——布局辅助统一填 `""`（源代码已处理）；
5. 可调休天数用银行家舍入（`MidpointRounding.ToEven`），否则黄金样本 1.585 会变成 1.59；
6. 商店认证必查：隐私政策 URL（应用有联网能力）、崩溃率达标（建议接 App Center Crashes 免费档）、描述与功能一致；
7. 应用图标：模板占位图必须替换（商店素材含 512×512 StoreLogo、44/50/71/150/310 系列）；
8. 旧 WPF 版草稿迁移（见 3.6）不能漏，否则老用户数据丢失。

## 9. 回归验证基准（黄金样本）

`C:\Users\20953\Documents\DesktopProjects\MonthlyReportGenerator\ExampleFiles\AGV月报_曹铮_2026_9.csv`：
录入 2026-09（6 个工作日 09-02/03/04/05/07/08 + 09-01 调休）后导出 XLSX，六项汇总必须等于 **6.0 / 6.00 / 52.68 / 12.68 / 1.58 / 1.0**，地点汇总 **无锡国药 6.0 / 12.68**。日报/周报结构对照 `ExampleFiles` 中模板与源项目导出。

## 10. 商店发布清单（移植完成后执行）

1. VS 右键项目 → 发布 → **将应用与应用商店关联**（登录个人开发者账号，写入包标识）；
2. 准备商店素材：图标、截图、简介、支持邮箱、隐私政策 URL（可托管 GitHub Pages 或公司官网）；
3. 发布 → 创建应用包 → **面向 Microsoft Store** → 生成 `.msixupload`；
4. Partner Center 上传 → 填信息 → 提交认证（数小时~3 工作日）；
5. 每次发版：版本号递增 → 重新打包 → 提交。

## 11. 里程碑顺序

1. 基础修正（第 2 节）→ 提交基线；
2. Models 直拷 → 编译通过；
3. 导出管线打通（布局构建器 + XlsxExportService + ClosedXML）→ **用黄金样本做 XLSX 回归**（此时无需 UI，可写控制台/单元测试式验证）；
4. Spike 四件事；
5. Services 全量拷贝（草稿 + 节假日）→ 数据迁移逻辑；
6. 三个 ViewModel 改写（对话框/文件选择/计时器）；
7. 三个页面 View + 窗口壳重写（标签=SelectorBar，页面常驻+空闲预构建）；
8. 低配机性能验证 + 全功能回归（对照 WPF 版逐条过 3.2–3.5）；
9. 商店关联、素材、打包提交。

## 12. 移植进度记录

- **b1e489e 基础修正**：§2.1 TFM → net10.0-windows10.0.19041.0；§2.2 显示名"科烽智能AGV月报生成工具"、+internetClient、-systemAIModels；Release 编译通过；§2.4 提交基线。
- **f73e107 里程碑2**：Models 10 文件直拷（命名空间保留 `MonthlyReportGenerator.Models`）；csproj 启用 `<ImplicitUsings>enable</ImplicitUsings>`（模板默认关闭，源项目开启，零修改直拷的前提）。
- **里程碑3 导出管线**：拷贝 6 个 Service（ReportLayoutBuilder/DailyReportLayoutBuilder/WeeklyReportLayoutBuilder/LayoutUtil/XlsxExportService/ReportExporter）+ ClosedXML 0.105.1；新增 `Tests\ExportSmokeTest` 控制台回归（链接主项目 Models/Services 源码），黄金样本全部断言通过（六项汇总 6.0/6.00/52.68/12.68/1.58/1.0 + 无锡国药 6.0/12.68，回读 XLSX 校验数值与存储格式）。主 csproj 加 `<Compile Remove="Tests\**" />`。
- **§2.3/§8.1 PublishTrimmed 验证**：ExportSmokeTest 以 PublishTrimmed+单文件自包含发布后运行全量回归通过 → **保留 PublishTrimmed=True**。
- **坑 9（新增）**：ClosedXML 0.105.1 的 `GetString()` 对整数值丢尾零（如 6 + 格式 0.00 → "6"），但格式已正确写入文件、Excel 显示正常；回归测试改用"数值 + 存储格式"双重校验，勿用 GetString 校验数值列显示。
- **环境坑 10（新增）**：本机 .NET 10.0.401 首次运行需在 `%USERPROFILE%\.dotnet` 写哨兵；且 ILLink 裁剪任务的 MSBuild 任务宿主依赖命名管道，在受限沙箱/CI 中会报 MSB4216——日常构建用 `-p:PublishTrimmed=false`，裁剪发布需完整权限执行。
- **389b66d 里程碑5**：DraftService/HolidayService 移植。`DraftService.Folder` 改 `ApplicationData.Current.LocalFolder.Path`（未打包运行回退旧路径）；新增 `MigrateLegacyData()`：首启把 `%LocalAppData%\MonthlyReportGenerator\` 全部 JSON 拷入 LocalState（`migrated.flag` 防重跑、已存在不覆盖），`App.OnLaunched` 调用。
- **81bbc8e 里程碑6**：Infrastructure/ProfileViewModel 直拷；新增 `Services\DialogService`（ContentDialog + FileSavePicker+InitializeWithWindow，MainWindow 激活时初始化）；ExportHelper 异步化、错误日志写入 LocalState；三个页面 VM 改写（DispatcherTimer→DispatcherQueueTimer、MessageBox→异步 ContentDialog）；ShellViewModel 改 SelectorBar 事件驱动 + `PreloadPages(DispatcherQueue)`；新增 `ValueFormatConverter`（WinUI Binding 无 StringFormat，数字/日期格式用它）。
- **里程碑7（UI 重写）**：DataGrid 包必须用 `CommunityToolkit.WinUI.UI.Controls.DataGrid` **7.1.2**（该 ID 最新即 7.1.2，net5.0-windows10.0.18362 + WindowsAppSDK≥1.0，即 WinUI 3 包；**WCT 8.x 没有 DataGrid，8.x 版本号不存在**）。MainWindow：ExtendsContentIntoTitleBar + SetTitleBar + AppWindow.SetIcon(app.ico) + Mica + SelectorBar + 三视图常驻；三个视图：DataGrid 用 LoadingRow 事件实现休息日红底（WinUI 无 DataTrigger）、PointerPressed 单击进入编辑、时间双 ComboBox+×清空+GotFocus 预填、CellTemplate/CellEditingTemplate 多行编辑；MonthlyReportViewModel 加 `CalendarRefreshed` 事件驱动节假日加载后红底重绘。Release 编译通过。
- **坑 11（新增）**：WinUI 3 `Window` 无 `Resources`/`Loaded`（资源放根 Grid，初始化用 `Activated`+一次性标志）；`DataGridSelectionMode` 无 `None`（用 IsReadOnly 代替）；XAML 报 WMC0001“Unknown type”往往是本地 C# 编译失败（如 CS0103）的连锁反应，先修 C# 错误。
- **坑 12（严重，实测踩中）**：**WinUI 3 的 `ElementName` 绑定不可靠**——用于视图 DataContext/Visibility 时绑定静默失败（Visibility 保持默认 Visible → 三个常驻页面全部叠在一起显示；DataContext 为 null → 下拉无选项）。**禁止用 ElementName 跨元素传 DataContext/Visibility**：改由代码隐藏管理（`_vm.PropertyChanged` 同步视图 DataContext + 显式切换 Visibility）；DataGrid 模板列内也禁止 ElementName 取外层 DataContext（实测下拉空选项），选项列表改用 `StaticResource` + `{Binding ..., Source=...}`（见 `ViewModels\TimeOptions.cs`）。
- **坑 13（新增）**：重写 MainWindow 时勿丢初始窗口尺寸——WinUI 3 模板默认小窗口会导致各区域挤压重叠；已用 `AppWindow.MoveAndResize`（1280×880 按工作区钳制居中）恢复 WPF 版行为。
- **坑 14（新增）**：DataGrid 虚拟化会回收行容器，`LoadingRow` 里给休息日设红底时，**非休息日分支必须显式把 Background/BorderBrush 清空（置 null）**，否则红底残留在无关日期（尤其 ItemsSource 重建/滚动后大片错红）。
- **坑 15（新增）**：行容器回收同样会让模板列里的 **ComboBox 显示旧行的选中值**（空时间的行滚动后显示时间）。修法：时间下拉 Tag 细分为 `start:h`/`start:m`/`end:h`/`end:m`，在 `LoadingRow` 后经 `DispatcherQueue.TryEnqueue` 遍历行内 ComboBox 显式复位 `SelectedItem`（空→null）；行若已被再次回收则跳过（ReferenceEquals 判断）。单元格模板内元素要 `VerticalAlignment="Center"`（含 StackPanel），否则下拉/×按钮不垂直居中。
- **运行冒烟（已完成）**：`-p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true` 未打包构建后实际启动应用，20 秒无崩溃（无 XAML 运行时错误）。窗口壳/三页 DataGrid/标题栏均正常实例化。
- **待用户交互验证（移植代码已完成，剩余为人工验收）**：① F5 或打包安装后对照 §3.2–3.5 逐条验收三页功能；② Spike 交互细节实测：DataGrid 单击进编辑、时间列单击即展开下拉、× 清空、焦点预填 09:00/17:00（`PointerPressed` 方案若单击不生效，退化为双击编辑）；③ 低配机（i5-8265U）性能验证；④ 导出 XLSX 与 WPF 版逐格对照（数值已由 ExportSmokeTest 保证）；⑤ §10 商店发布流程（VS 关联商店→素材→打包）。

