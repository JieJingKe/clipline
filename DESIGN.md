# Clipline · 剪贴板晾衣绳 —— Windows 版设计文档

> 产品基准：[Tendedero](https://github.com/alejandrobujan/tendedero)（macOS，Swift/AppKit+SwiftUI，MIT）
> 本项目：把 Tendedero 的"屏幕顶边晾绳"交互移植到 Windows，并**扩展到文字剪贴板**。
> 文档状态：v1.0（Spike 前）。Spike 结论要回写进 §8 风险登记表。

---

## 0. 命名

**Clipline**（英文品牌 / 仓库名）· **剪贴板晾衣绳**（中文副标题）。`clipboard × clothesline` 的拼词，一个词同时说清"剪贴板"和"绳子"两件事。

定名时实测的重名情况（2026-11-02，GitHub `in:name` 搜索 + 包管理器）：

| 候选 | GitHub 同名仓库 | npm | PyPI | 结论 |
|---|---|---|---|---|
| `clipline` | 49（全为无关个人项目，无剪贴板工具） | 空闲 | 空闲 | **采用** |
| `lineclip` | — | **已被 Mapbox 的折线简化库占用** | 空闲 | 否决：反向拼写会一直被压住 |
| `clip-line` | 201 | 空闲 | 空闲 | 否决：语义污染最重 |
| `clothesline` | 有 `b20n/Clothesline`、`JamiesWhiteShirt/clothesline` | — | — | 否决：单词太通用 |
| `clothespin` / `clothesclip` / `clipline-clipboard` | 24 / 0 / 0 | 空闲 | 空闲 | 备选：若日后 `clipline` 撞车可迁到 `clipline-clipboard` |

落地约定：命名空间 `Clipline.*`、程序集 `CliplineSpike`（spike 阶段）→ `Clipline`（M1）、数据目录 `%LOCALAPPDATA%\Clipline\`、日志 `clipline-spike.log`、单实例 Mutex `Local\CliplineSpike`。托盘 tooltip 与菜单显示 `Clipline · 剪贴板晾衣绳`。

**许可证：MIT**（2026-11-02 定）。蓝本 Tendedero 就是 MIT，本项目是学习性质的再实现，没必要再加一层约束；曾考虑的 Apache-2.0（含专利授权条款）在这里不划算 —— 它会让 LICENSE 长三倍，而"深度调用 Windows 底层 API"并不产生需要专利保护的权利要求。仓库根 `LICENSE` 落 `Copyright (c) 2026 Clipline contributors`，正式建仓时替换为作者名。

---

## 1. 一句话定义

截图、录屏、复制的文字与图片，全部挂在屏幕顶边的一条绳上；指针停在顶边中部绳子垂下，用完移开即收起；桌面从此不留垃圾，也不需要 `Alt+Tab` 去找刚才复制的东西。

三条不可妥协的产品原则（继承自 Tendedero）：

1. **零网络**：没有账号、没有遥测、没有云同步。网络请求代码不允许存在于生产路径。
2. **不抢焦点**：所有交互都不改变用户当前的前台应用。这是"顺手"的根本原因，也是本项目最大的技术风险（§8-R1）。
3. **瞬时存在**：只有被你拖出去/用过的内容才落地保存，其余在退出时消失。

---

## 2. 交互规范：Tendedero → Windows 映射

| Tendedero（macOS） | 本项目（Windows） | 实现要点 |
|---|---|---|
| 指针停在菜单栏中部 → 绳子垂下 | 指针停在**主屏顶边中部热区** → 绳子垂下 | 光标点轮询（`GetCursorPos`，32ms），不用低级鼠标钩子（全系统输入延迟不可接受） |
| 指针移开 → 收起 | 同 | `WM_MOUSELEAVE` + 轮询双保险；收起后窗口移出屏幕或转点击穿透 |
| **（Windows 新增决策）** 收起后不留任何痕迹 vs 留锚点 | **默认在工作区顶边 2/3 处挂一个"拉绳"锚点**（用户提供 SVG 素材 → `Geometry.Parse` + `Stretch.Uniform`，34×50 DIP，绳圈朝下）：**可悬停、可点击**（两者都拉起绳子），`WS_EX_NOACTIVATE` + `MA_NOACTIVATE` 保证点它不抢焦点；设置里可关闭回到"完全无痕迹" | 因为 Windows 顶边没有 macOS 菜单栏那种天然锚，零痕迹=零可发现性。形态迭代三轮均由用户反馈驱动：整条白色横杠（"不合理"）→ 程序化小绳头（"还是不好看"）→ 用户提供的拉绳素材并移到 2/3 处；第一版素材只能"看着"（点击穿透），用户随即要求"加入鼠标放上去和点击就展开"。见 §7.2、R11 |
| **（Windows 新增决策）** 触发区宽度跟谁走 | **恒定 = 工作区中间 1/3**（上限 900 DIP），与卡片数量、绳宽无关；绳子本体仍按内容变宽（`min(工作区×0.4, 900 DIP)` 且不小于全部卡片所需） | 早期版本触发区=绳宽，只有 1–2 张卡片时热区短到划半天才碰到，卡片多了又变得过长。用户明确要求固定成中间 1/3。见 §7.3 |
| **（Windows 新增决策）** 卡片底色 / 夹子 / 新增方向 | 牛皮纸便签**调浅**：渐变 `#FDF7EA → #F8EFDA → #F1E2C4`（约 97% 不透明）+ 墨色字 `#332211`，边线 `#C9AE86`，投影降到 0.28 不透明度；夹子换成**用户提供的晾衣夹 SVG**，用银色线性渐变（`#FFFFFF → #D6DDE4 → #B9C2CC → #EDF1F5`）做出金属银白反光，34×21.5 DIP 横跨绳线、**只让夹子下缘 1/4 高度压在便签上**（`Margin.Top = -PinHeight × 0.75`，再叠多就糊住纸面）；新复制的卡片**挂在最右边**（最新在右），超容量时挤掉最左边的旧卡片 | 第一版半透明白玻璃 + 纯白字在浅色桌面上完全看不清；改成牛皮纸后用户仍反馈"底色有些深"，故整体提亮一档。图文卡片宽度统一 200 DIP（原来文字 320 / 图片 200）。"最新在右"对齐 §3 的去重规则（重复项滑到最右）。见 R8 |
| **（Windows 新增决策）** 首次使用的引导 | 首次成功展开之前，细绳下方显示小气泡"把鼠标移到这条线上 ↓"；一旦用户成功展开（或手动关闭）即永久消失 | 状态存 `%LOCALAPPDATA%\Clipline\onboarded.flag`，只在锚点显示时出现 |
| **（Windows 新增决策）** 首屏就是说明书 | 绳子上最初的三张卡片直接写操作提示：**单击复制剪贴板** / **双击直接粘贴到当前光标处** / **拖动图片直接快速粘贴**，再配一张真实生成的图片卡供试拖出 | 用户第一次拉起绳子时最容易"不知道能干什么"，把提示做成便签挂在绳上比弹对话框便宜，而且它本身就是卡片样式的验收样本。第三条刻意只承诺图片：文字拖出实测只在接收 `CF_UNICODETEXT` 的目标里成立（编辑器 `Copy`、桌面与资源管理器 `None`），不能对全体目标许愿。见 spike/README「拖动出绳」 |
| **（Windows 新增决策）** 语言 | **中文 / English** 双语，托盘菜单里两个带勾选的项直接切、立即生效不用重启；选择落盘，没有落盘值时跟随系统界面语言 | 免安装分发面向的第一批人里有英文使用者，硬编码中文等于把托盘菜单之外的一切挡在门外。实现约定见 §7.10 |
| 单击 → 复制图片 | 单击 → 复制内容并把**前台应用还回去** | 图片：`CF_DIBV5`/PNG；文字：`CF_UNICODETEXT` |
| 长按 → 标注 | v1：长按 → 打开系统编辑器（`mspaint.exe` / Photos）；v2：自研标注层 | 自研标注是独立子项目，不进 MVP |
| 双击 → 预览 | 双击 → `ShellExecute` 默认程序打开 | 文字卡片双击=直接粘贴到前台（见 §3） |
| Force click / Space → 快速查看 | Space → 悬停卡片大图预览 | 文字则放大显示 |
| 拖进应用 → 发送，仍留在绳上 | `DragDrop.DoDragDrop`，返回效果为 Move 之外的一切 → 保留 | 自定义 `IDataObject` + 延迟渲染 |
| 拖进文件夹 → 保存，离开绳 | 拖进资源管理器 → 复制落地并从绳上移除 | 用 `FileGroupDescriptorW` 伪文件流投递；落地后校验 `DoDragDrop` 返回值 |
| 拖进回收站 / 点叉 → 丢弃 | 同：拖到任务栏回收站 或 卡片右上角 ✕ | 真正删除走 `IFileOperation`（含确认与回收站策略） |
| `⌘Z`（指针在绳上）→ 把刚丢弃的挂回来 | `Ctrl+Z` | 软删除 + 撤销栈（最近 20 项） |
| 右键 → Pin（灰夹子，固定在结的左侧） | 右键 → 固定：钉在绳子左侧，容量淘汰时不淘汰它 | 固定项跨重启保留 |
| 右键 → Hold（拿在手上对照） | v2 | 需要额外的置顶浮窗 + 鼠标跟随 |
| `⌃⌥T` 显示/隐藏 | `Ctrl+Alt+T`（可改） | `RegisterHotKey`，冲突时提示并允许重绑 |
| Finder 右键 → Hang on Tendedero | 拖文件到顶边热区 → 绳子自动展开，松手即挂上（`IDropTarget`） | 避开 COM Shell 扩展（注册表 COM 成本高、升级易坏）；v2 再评估右键菜单 |
| 截图跳过桌面（无浮动缩略图） | **v1 做不到**：Win+Shift+S 由 Snipping Tool 决定落盘或仅进剪贴板 | v1 做"观察者"（监听剪贴板 + 截图目录）；v2 自研截图（`Windows.Graphics.Capture`）才能真正接管 |
| 菜单栏图标 | 系统托盘图标 + 顶边热区 | `Hardcodet.NotifyIcon.Wpf` |
| 录屏卡片带播放键，长按裁剪 | 监听 `%USERPROFILE%\Videos\Captures`，悬停静音预览 | 裁剪交给系统 Photos |

---

## 3. 文字剪贴板的专有设计（Windows 版新增，非移植）

文字没有缩略图，"一字排开"天然比图片难读。规则：

- **卡片尺寸**：高度与图片卡片一致（默认 120px），宽度按内容自适应，上限 420px；超出显示省略。
- **内容呈现**：等宽字体用于识别出的代码/JSON；首行为标题，第二行为摘要；空行折叠。
- **类型识别**：URL（可右键"在浏览器打开"）、代码（语言标注）、多行段落、纯短语。
- **交互分级**（**已定**）：
  - 单击 = 只复制到剪贴板（最安全，用户自己 `Ctrl+V`）
  - **双击 = 复制 + 向原前台应用注入 `Ctrl+V`（默认语义，已定）**；注入失败（返回按键数 < 请求数，典型是 UIPI 权限屏障或目标不认 `Ctrl+V`）时**必须静默退化成"只复制"**并给一次轻提示，绝不出现"内容进了剪贴板但用户以为贴上了"的静默丢失
  - 右键 = 纯文本粘贴 / 保留格式粘贴 / 作为引用插入 / 删除；语义可在设置里重绑（例如 URL 项双击=用浏览器打开）
- **去重**：与最近一项内容哈希相同则把它滑到最右，不新增卡片。
- **敏感过滤（硬性要求）**：出现下列任一情况**不入绳**，且从历史中清除同值残留：
  - 剪贴板注册了 `Clipboard Viewer Ignore`、`ExcludeClipboardContentFromMonitorProcessing`、`CanIncludeInClipboardHistory=false`、`CanUploadToCloudClipboard=false` 任一格式；
  - 前台进程在白名单里（1Password、Bitwarden、Keepass、浏览器密码气泡窗口）；
  - 内容匹配密码/卡号/密钥形态（长度 8–64 的混合字符且窗口标题含"密码/Passkey/OTP"等）。
- **容量**：最近 100 项（图片+文字共享），LRU 淘汰，固定项不淘汰；文字全文入 SQLite，UI 只渲染前 2KB。
- **默认退出即清**：文字历史默认不持久化；用户可在设置里打开"加密保留"（DPAPI，当前用户范围）。

---

## 4. 技术选型

**结论：C# + WPF，目标框架 `net10.0-windows`，`UseWPF=true`。**

理由：本项目 4 个核心难点全是 Win32 系统集成，而非视觉——非激活透明遮罩、原生 OLE 拖出、剪贴板链、Shell 已知文件夹。WPF/.NET 在这四点上的路径最成熟：`DragDrop.DoDragDrop` 一行 API 就能把数据投递给任意应用（这是 Tauri/Flutter 的最大软肋），P/Invoke + `[LibraryImport]` 源生成器写 interop 干净，`RenderTransformOrigin` 足够实现卡片轻摇。

依赖清单（刻意保持极少）：

| 用途 | 选择 | 备注 |
|---|---|---|
| MVVM | `CommunityToolkit.Mvvm` | 只用于设置窗口 |
| Win32 | 自研 `src/Win32`，`[LibraryImport]` | 不引入第三方 interop 大包 |
| 托盘 | `Hardcodet.NotifyIcon.Wpf` | 避免引用 WinForms |
| 存储 | `Microsoft.Data.Sqlite`（FTS5） | WAL 模式 |
| 图片解码 | `WIC`（`BitmapFrame.Create` + `DecodePixelWidth`） | 缩略图缓存到本地 |
| 日志 | `Serilog`（滚动文件，仅本地） | Release 默认 Warning 级 |
| 打包更新 | Velopack（后期） | 先不做自动更新 |

被排除项与原因：

- **Tauri v2**：视觉还原成本最低（CSS `transform-origin` 摆动天然贴合），但非激活置顶窗口 + 带自定义 payload 的 OLE 拖出需要手写 Rust FFI/`IDropSource`，属于未验证就押注。**若 Spike 阶段发现 WPF 的绳子质感明显不足，卡片渲染层可单独换成 Direct2D/Win2D，架构不动**，因此不必现在换栈。
- **WinUI 3**：Composition 弹簧动画最原生，但要背 Windows App SDK 运行时，且全屏透明遮罩支持不如 WPF 干脆。
- **Flutter**：桌面端窗口遮罩、点击穿透、拖出是长期弱项。
- **Electron**：常驻 300MB+，作为背景小工具不可接受。

---

## 5. 架构与数据流

```
        ┌──────────── 生产者 (Capture) ────────────┐
剪贴板链 WM_CLIPBOARDUPDATE ─┐                     │
截图目录 ReadDirectoryChangesW ─┤→ 归一化 → 过滤/去重 → ItemStore（有序、可撤销）
录屏目录 Captures ────────────┘        ↑                    │
                                      │                 事件：Added/Removed/Pinned
                               PrivacyGate               ↓
        ┌──────────── 消费者 (Delivery) ────────────┐   Overlay（绳子窗口）
        │ 单击 → ClipboardWriter（自写抑制）        │   ├─ HotZone 触发状态机
        │ 双击 → FocusRestore + SendInput(Ctrl+V)   │   ├─ LineWindow 非激活遮罩
        │ 拖出 → DragSource/IDataObject             │   ├─ CardTemplate（图片/文字）
        │ 落地 → IFileOperation + ItemStore.Remove  │   └─ SwingAnimator
        └───────────────────────────────────────────┘
                     ↑ 共享：Settings、Persistence(SQLite)、Win32 interop
```

模块职责边界（一个模块只对外暴露一个接口，跨模块通信走事件）：

| 模块 | 职责 | 明确不做 |
|---|---|---|
| `Win32` | P/Invoke、扩展样式、钩子、DPI、已知文件夹 GUID | 任何业务逻辑 |
| `Overlay` | 顶边热区、展开/收起状态机、卡片渲染、动画 | 读写剪贴板、访问磁盘 |
| `Capture` | 监听剪贴板/目录，产出原始条目 | 决定 UI 呈现 |
| `PrivacyGate` | 敏感内容判定与白名单 | 存储 |
| `Items` | 有序集合、LRU、固定、软删除撤销栈 | I/O 细节 |
| `Delivery` | 复制、粘贴注入、拖出、落地保存、删除 | UI 动画 |
| `Persistence` | SQLite 索引、缩略图缓存、settings.json | 业务规则 |

---

## 6. 目录结构

```
Clipline/
├─ Clipline.sln
├─ src/
│  ├─ Clipline/                        # 主程序（WPF）
│  │  ├─ App.xaml(.cs)                 # 单实例 Mutex、STA 消息泵、异常兜底
│  │  ├─ Win32/
│  │  │  ├─ NativeMethods.cs           # [LibraryImport] 声明
│  │  │  ├─ WindowEx.cs                # WS_EX_NOACTIVATE|TOOLWINDOW、WM_MOUSEACTIVATE
│  │  │  ├─ CursorPolling.cs           # GetCursorPos 定时器
│  │  │  ├─ ForegroundTracker.cs       # SetWinEventHook(EVENT_SYSTEM_FOREGROUND)
│  │  │  ├─ KnownFolders.cs            # Pictures/Videos + OneDrive 重定向
│  │  │  └─ Dpi.cs                     # Per-Monitor V2 换算
│  │  ├─ Overlay/
│  │  │  ├─ LineWindow.xaml(.cs)       # 透明、Topmost、非激活遮罩
│  │  │  ├─ HotZone.cs                 # 中部热区判定 + 展开/收起状态机
│  │  │  ├─ CardItemView.xaml          # 图片卡片 / 文字卡片两种模板
│  │  │  ├─ Rope.xaml + SwingAnimator  # 绳线绘制、transform-origin 轻摇
│  │  │  └─ RevealController.cs        # 弹性下落动画、与容量联动的横向布局
│  │  ├─ Capture/
│  │  │  ├─ ClipboardWatcher.cs        # AddClipboardFormatListener + WndProc
│  │  │  ├─ FolderWatcher.cs           # ReadDirectoryChangesW 封装（防抖）
│  │  │  └─ CaptureOrchestrator.cs     # 归一化为 ClipItem
│  │  ├─ Items/
│  │  │  ├─ ClipItem.cs                # Kind=Text|Html|Rtf|Image|File|Video
│  │  │  ├─ ItemStore.cs               # 排序/Pin/LRU/软删除/撤销
│  │  │  ├─ PrivacyGate.cs
│  │  │  └─ Thumbnails.cs
│  │  ├─ Delivery/
│  │  │  ├─ ClipboardWriter.cs         # 自写抑制（owner/sequence 比对）
│  │  │  ├─ PasteInjector.cs           # AttachThreadInput + SendInput
│  │  │  ├─ CliplineDataObject.cs      # 延迟渲染 + FileGroupDescriptorW
│  │  │  ├─ DragSource.cs
│  │  │  └─ FileDrop.cs                # IFileOperation 移动/删除/回收站
│  │  ├─ Interactions/InteractionRouter.cs  # 单击/双击/长按/键盘的集中判定
│  │  ├─ Persistence/
│  │  │  ├─ HistoryDb.cs               # schema v1 + FTS5
│  │  │  └─ SettingsStore.cs           # %APPDATA%\Clipline\settings.json
│  │  ├─ Settings/SettingsWindow.xaml
│  │  ├─ Resources/                    # 图标、resw（zh-Hans / en / es / ja）
│  │  └─ app.manifest                  # PerMonitorV2、长路径
│  └─ Clipline.Interop.Tests/          # 单测（格式解析、去重、隐私判定）
├─ spike/                              # §9 的三项验证，独立小工程，用完归档
├─ docs/                               # 本文件、交互对照表、隐私声明
├─ installer/                          # Velopack / 签名脚本 / 便携 zip
└─ .editorconfig
```

---

### 6.1 分发、安装、关闭与卸载

**先说清当前 spike 的形态**（M0 阶段刻意不做安装包，用户已确认"免安装也可以"）：`dotnet build` 产出 `spike/SpikeLine/bin/Debug/net10.0-windows/CliplineSpike.exe`，双击即运行，无安装、无注册表写入、无服务、无计划任务。关闭方式：**右下角托盘图标右键 → 退出**（11-02 已实现，纯 `Shell_NotifyIcon` + 手工像素画 HICON，不引 WinForms）、`Ctrl+Alt+Shift+Q`（同样走 `app.Shutdown()`）、或注销登录；**不要 `/F` 强杀**（剪贴板监听进程被强杀有锁死全机剪贴板的风险，见 §11 与运维备忘）。卸载 = 删掉 `bin` 目录 + `%LOCALAPPDATA%\CliplineSpike\`（只剩 `onboarded.flag`）。剪贴板监听器由 `RemoveClipboardFormatListener` 在 `Closed` 时摘除，托盘图标在 `app.Exit` 里 `NIM_DELETE` + `DestroyIcon`，进程一死系统自动回收其余，不留残余。

**M1/M2 的正式方案（已定，按此实现）**：

| 环节 | 决定 | 理由 / 约束 |
|---|---|---|
| 运行时 | **self-contained 单文件**（`PublishSingleFile` + `IncludeNativeLibrariesForSelfExtraction` + `EnableCompressionInSingleFile`，`win-x64`） | 用户机器上没有 .NET 桌面运行时也不能假设他装；不依赖 `dotnet` 命令。产物约 90–120MB，可接受。同时保留一份 framework-dependent 便携 zip 给开发机 |
| 载体 | **免安装便携为主、安装器为辅**（用户 11-02 明确"免安装也可以，方便分发就好"）：官方产物是一个 zip，解开即可跑；Inno Setup 安装器只做"解压 + 建快捷方式 + 可选写 Run 键"这一层薄壳，不做任何独占安装 | 免安装最贴合"零网络、无账号、不留痕迹"的产品形象，也绕开 UAC；但**免安装 ≠ 免托盘**：没有安装器就更依赖托盘图标作为唯一可见的退出入口 |
| 身份 | 代码签名（OV/EV 证书）+ SmartScreen 申报 | 未签名的 exe 会被 SmartScreen 拦，且**剪贴板/输入注入类程序**在无签名时更容易被杀软误判。这是分发的硬门槛，不是可选项 |
| 首次运行 | 解压到上述目录 → 写默认 `settings.json` → 注册热键 → 创建锚点窗口。**不**自动开机自启，设置里"登录时启动"默认关 | 剪贴板常驻程序必须让用户主动开启，否则第一次被发现就永久不信任 |
| 自启 | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 一条值，指向 exe 绝对路径；关闭开关时删该值 | 用户级 Run 键无需管理员，卸载时一并清 |
| 常驻入口 | 系统托盘图标（左键/双击=拉起或收起绳子，右键菜单=显示/收起绳子 · **显示下拉绳（顶边绳头，带勾选）** · **中文 / English（各带勾选）** · 退出 Clipline）+ 全局热键 `Ctrl+Alt+T`（开关）/`Ctrl+Alt+L`（开关下拉绳，与托盘同一开关）/`Ctrl+Alt+Shift+Q`（退出）。**spike 已实现**：`Shell_NotifyIcon` + `CreateIcon` 手工像素画 HICON（32×32 @200% DPI，按 `SM_CXSMICON` 取尺寸），未引 WinForms —— 引了会让 `Application`/`Clipboard` 在隐式 using 下变二义 | 免安装分发时托盘是"如何关掉一个没有窗口的程序"的唯一直觉入口，spike 阶段缺它只能靠热键，可发现性为零。Win11 默认把新图标收进 `^` 折叠区，所以 tooltip 和菜单文字必须自带产品名 |
| 单实例 | 命名 Mutex `Local\Clipline.App`；第二实例提示"已在运行"后退出 | spike 实测第二个实例 `RegisterHotKey` 返回 `False`，多实例会互相抢热键并让剪贴板重复入史 |
| 关闭 | 托盘"退出" → 停轮询定时器 → `RemoveClipboardFormatListener` → 落盘（若开启持久化则加密写历史）→ `Shutdown()`。**关窗口 ≠ 退进程**，点 X 只收起绳子 | 文字历史默认"退出即清"，所以退出路径必须先把该清的清掉再结束进程 |
| 更新 | Velopack（`win-x64` 便携 + 增量更新），或 v1 先"下载新版覆盖"手动更新 | 零网络是产品原则之一 —— 更新必须是**用户显式点击**的一次性动作，或干脆走 Windows 商店/手动下载，不能后台静默拉包。这条要在 M1 明确取舍 |
| 卸载 | Inno 卸载器：删安装目录、删 Run 键、删 `%LOCALAPPDATA%\Clipline\`（`settings.json`、DPAPI 加密的 `history.db`、`onboarded.flag`）；**保留**用户主动"另存到磁盘"的图片文件 | 卸载必须把剪贴板历史一起抹掉 —— 那是本产品最敏感的数据；DPAPI 密文虽绑定当前用户，也不该留在机器上 |
| 卸载后残留检查 | 无服务、无驱动、无计划任务、无全局钩子（低级鼠标钩子已明确否决）；只剩 Windows 自己管理的剪贴板监听注册，随进程消失 | 用于回答"这软件到底在我机器上留了什么"，写进 README 的隐私声明 |

---

## 7. 关键实现约定

1. **窗口**：`WindowStyle=None` + `AllowsTransparency=true` + `ShowInTaskbar=false` + `Topmost=true`，`SourceInitialized` 时追加 `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`，并在 `WM_MOUSEACTIVATE` 返回 `MA_NOACTIVATE`。**绳子窗口**在收起状态下整体移出屏幕上边缘（不留任何可交互残留）。
2. **收起锚点（已定：默认开启，且可交互）**：顶边常驻一个**独立的**小绳头窗口，位于**工作区宽度的 2/3 处**（用户指定，不居中），尺寸 `34×50 DIP`，`WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`，素材是用户提供的拉绳 SVG（`Geometry.Parse` 直接吃 path 数据，不引 SVG 依赖）。它**参与命中测试**（悬停变亮 + 手型光标 + 悬停/单击都拉起绳子），但**绝不参与激活**：`WM_MOUSEACTIVATE` 一律返回 `MA_NOACTIVATE`。纯装饰窗（引导气泡）仍走 `WS_EX_TRANSPARENT` 点击穿透。代价是绳头那 34×50 DIP 会挡住贴顶窗口的对应小区域（G4-b 待复验）。设置项 `Anchor.enabled=false` 时该窗口不创建，回到"完全无痕迹"。**首次引导气泡**同样是非激活窗口，仅在 `onboarded.flag` 不存在且锚点开启时出现，用户首次成功展开即写标记并永久消失。
3. **触发**：`GetCursorPos` 32ms 轮询；**触发区恒定 = 工作区顶边中间 1/3 宽**（上限 900 DIP），与卡片数量、绳子实际宽度**解耦** —— 早期版本触发区跟着绳宽走，只有 1–2 张卡片时热区短到难以命中，用户明确要求改成固定 1/3。绳子本体宽度另算（`min(工作区×0.4, 900 DIP)`，且不小于容纳全部卡片所需宽度，最宽不超工作区）。阈值 **6 物理像素**（原 2px 在高分屏上过薄，指针一划就过；6px 仍不足以在日常横向移动中误触发）。
4. **前台还原**：`ForegroundTracker` 常驻记录最近一个非本进程的前台 HWND；`PasteInjector` 用 `AttachThreadInput` 绕过 `SetForegroundWindow` 限制，失败退化为"只复制，用户自己粘贴"。
5. **剪贴板读取要重试 + 序列号闸门**：别的进程可能正持有剪贴板（`CLIPBRD_E_CANT_OPEN`），统一走"最多 5 次、间隔 50ms"的封装，永不阻塞 UI 线程。`WM_CLIPBOARDUPDATE` 在 UI 忙时会在队列里堆积，一次批量复制可能只对应一次真实内容变化 —— 处理前必须比 `GetClipboardSequenceNumber()`，同序列直接丢（spike 实测：9 次连发复制塌成 1 次捕获 + 8 次"重复"，见 spike/README bug 7）。
6. **自写抑制**：我们写剪贴板会触发 `WM_CLIPBOARDUPDATE`，用 `GetClipboardOwner()` 比对自身 HWND + 序号标记，避免自己入史。
7. **DPI / 多屏**：manifest 声明 Per-Monitor V2；每个物理显示器顶部各一条绳（v1 可先只支持主屏，但布局计算必须按屏工作区，不能写死 1920）。锚点同样按屏放置，且必须在**该屏自己的 DPI** 下算 4px。
8. **性能预算**：常驻内存 ≤ 80MB（**spike 实测 171MB，见 R9，M1 前必须归因**），空闲 CPU ≤ 0.5%，绳子展开到可交互 ≤ 120ms。图片一律降采样解码，原图不进内存。锚点是常驻窗口，其内存与唤醒开销要单独计量。
9. **命名/代码风格**：`.editorconfig` 强制 `Nullable=enable`、file-scoped namespace、`async` 后缀、`var` 优先、**默认不写注释**（只在写"为什么"时加一行）；禁止 `Console.WriteLine` 调试残留，走 Serilog。
10. **文案与语言**：用户可见文案**一律走文案表，不允许字面量散在控件里**。spike 用 `Strings.cs` 一张 `key → [zh, en]` 表 + `Lang.T/Format` + `Lang.Changed` 事件（卡片文字、右键菜单、引导气泡、托盘提示与 tooltip 全部订阅刷新，不重建窗口）；默认语言跟随 `CurrentUICulture`，用户切换后落 `%LOCALAPPDATA%\Clipline\language.txt`。**日志、命令行参数、异常信息故意不进表** —— 那些是给开发者看的，不该被语言开关影响。M1 换成 `.resw` 卫星资源（zh-Hans / en / es / ja），并把"语言"并进设置窗口；文案长度要按英文比中文长三成的余量设计（spike 已实测 200 DIP 卡片内英文两句提示占两行，引导气泡因此从固定 40 高改成 52 高 + 折行）。
11. **Git 约定**：Conventional Commits（`feat:`/`fix:`/`spike:`），Spike 分支 `spike/*`，验证通过后 squash 进 `main` 并把结论写进 §8。

---

## 8. 风险登记表（Spike 要逐条判定）

| # | 风险 | 影响 | 缓解 | 判定方式 |
|---|---|---|---|---|
| R1 | **非激活窗口能否真的不抢焦点** | 致命：交互全部失效 | WPF + `MA_NOACTIVATE`；备选：分层窗口 `UpdateLayeredWindow` 自绘 | 记事本里打字，点绳子上的卡片，观察光标是否还在记事本 |
| R2 | **拖出到第三方应用**（微信/资源管理器/Office） | 高 | 原生 OLE；伪文件用 `FileGroupDescriptorW` | 三种目标各拖一次，图片+文字+伪文件 |
| R3 | **截图接管做不到** | 中：与 Tendedero 体验有差 | v1 观察者模式；文档明示；v2 自研 `Windows.Graphics.Capture` | 记录 Snipping Tool 不同设置下的落盘/剪贴板行为矩阵 |
| R4 | **文字"一字排开"不可读** | 中：核心卖点打折 | 摘要卡 + 搜索浮层 + 双击直贴 | 用 20 条真实复制内容做纸面/原型评审 |
| R5 | 顶边热区与全屏游戏/远程桌面冲突 | 中 | 前台是全屏独占或 RDP 时自动抑制 + 开关 | 游戏与 `mstsc` 实测 |
| R6 | OneDrive 重定向截图目录 | 中 | 走 Known Folder GUID 而非拼字符串路径 | 开启"截图备份"后实测 |
| R7 | 密码泄漏进历史 | 高（隐私事故） | PrivacyGate 四重判定（§3），默认退出即清 | 用 1Password/Bitwarden 复制做红队测试 |
| R8 | 卡片质感达不到 Tendedero（玻璃/轻摇） | 中 | 卡片渲染层可替换为 Direct2D/Win2D，架构不变 | 视觉对照截图评审 |
| R9 | 常驻内存与体积 | 中 | 框架依赖 + 安装器；渲染层必要时去掉位图特效 | spike 实测 WorkingSet 171MB（预算 80MB），待归因 |
| R11 | **常驻点击穿透细线会不会干扰贴顶窗口**（锚点决策带来的新风险） | 中 | 独立窗口 + `WS_EX_NOACTIVATE \| WS_EX_TOOLWINDOW \| WS_EX_TRANSPARENT`；绳子展开时隐藏锚点 | G4：贴顶最大化窗口下，鼠标穿过细线仍能拖窗/点标题栏；细线自身不产生悬停高亮或点击拦截 |
| R12 | 锚点常驻的内存与唤醒成本（叠加 R9） | 中 | 锚点只画一条线，不加 `Effect`；两态分别计量 | 记录"仅锚点常驻"与"锚点+绳子展开"的 WorkingSet 与 CPU |
| R13 | **剪贴板被某进程持有后全机长时间不可用**（spike 实测发现的新风险） | 中高：一次异常就让用户的 Ctrl+C/Ctrl+V 失效数小时 | 绝不长期持有剪贴板句柄：`OpenClipboard` 后立刻 `CloseClipboard`；隐私标志优先 `IsClipboardFormatAvailable`（无需打开剪贴板）；取值失败 fail-closed；写侧有限次退避重试（8 次）后放弃并走降级路径，不静默无限重试 | 实测：22:07 强杀一个正处于剪贴板写入中的 spike 实例后，全机 `OpenClipboard` 一律 `ERROR_ACCESS_DENIED(5)`、`GetClipboardOwner()=NULL`、`CloseClipboard=1418`，连 PowerShell `Set-Clipboard` 都失败；**次日 08:47 自行恢复**（持锁方退出），未注销未重启 |
| R14 | **`SendInput` 直贴被静默拒收**（spike 实测） | 高：双击直贴是 R4 的核心缓解手段，失效等于卖点少一半 | `INPUT` 在 x64 必须是 **40 字节**（类型 4 + 补齐 4 + `MOUSEINPUT` 32），union 里漏掉 `MOUSEINPUT` 会让 `cbSize=32` → 返回 0；日志固定打 `sizeofINPUT/accepted/lastError`；注入前先确认前台窗口确实是用户的目标应用 | 实测修好后 `sizeofINPUT=40 accepted=4 lastError=0`。**注意**：`accepted=0 err=87` 是参数错，不是 UIPI/管理员权限问题；跨进程自动化另受 Windows 前台锁限制（后台进程无法抢前台），故 G3-a 做成半自动 `--paste-gate` |

### 8.1 Spike 进展（2026-11-01，自动部分）

| # | 状态 | 实测数据 |
|---|---|---|
| R1 | 🟡 样式下发通过 | `WS_EX_NOACTIVATE\|TOOLWINDOW` + `WM_MOUSEACTIVATE→MA_NOACTIVATE` 与 `AllowsTransparency` 共存；焦点是否真被抢需手动 `G1-a/b/c` |
| R2 | 🔴 未测 | spike 只用了真实 `FileDrop`，`FileGroupDescriptorW` 伪文件留待验证 |
| R5 | 🟡 逻辑已实现 | 全屏抑制=前台覆盖整显示器或 `mstsc`，排除 `explorer`；待实机收敛 |
| R8 | 🟡 摆动已实现，卡片改牛皮纸 | 4 段阻尼关键帧；绳子是白色半透明横线（用户要求回退，无下垂弧）。卡片按用户反馈从"半透明白玻璃 + 白字"改成**牛皮纸便签 + 墨色字**，第二版再整体提亮一档（`#FDF7EA → #F1E2C4`），并把图文卡片统一为 200 DIP 宽；夹子从纯色矩形换成用户提供的晾衣夹 SVG（金属银渐变），新卡片改挂最右侧。配色与夹子几何收在 `KraftPaper` / `CardView.BuildClothespin` 一处，`--render-preview` 离屏出图自检（白→深灰渐变背景，一次看清华白两种桌面） |
| R4 | 🟡 直贴已通，摘要仍粗 | 双击直贴 **2026-11-02 用户实测通过**（记事本里直接出内容）；卡片目前是"整段文字截断"，M1 要做真正的摘要 + 搜索浮层 |
| R9 | 🔴 **新发现，超预算** | WorkingSet 实测 **171MB**（80MB 目标的 2 倍）。待查：`DropShadowEffect` 位图缓存 vs WPF 基线 |
| R10 | ✅ 已修 | 第二实例 `RegisterHotKey=False` 复现热键争抢 → 加命名 `Mutex` 单实例守卫 |
| R7 | ✅ **G5 全绿**（2026-11-02 08:54 自动，09:23 改锚点/退出后复跑仍 `PASS`） | `--selftest` 六条断言一次跑过：`RESULT PASS clipboardUsable=True captures=10 rejections=2 selfWrites=1 duplicates=1 staleSkips=1 evictions=1 cards=12`。两条拦截原因分别是 `ignore format present: Clipboard Viewer Ignore` 与 `opt-out flag CanIncludeInClipboardHistory=0`；自写抑制靠 `GetClipboardOwner()` 进程比对。**尚未覆盖**：密码管理器真实红队测试（需真人 1Password/Bitwarden 复制）、前台白名单只做了进程名匹配 |
| R11 | ✅ 锚点成立，形态定稿 | 锚点最终形态：**用户提供 SVG 拉绳素材**经 `Geometry.Parse` 直接渲染（不引 SVG 库；SVG 的 nonzero 与 WPF 的 EvenOdd 差异正好把绳圈内孔挖对），挂在工作区 **2/3** 处而非居中；绳子本体回退为白色半透明横线。**锚点自身可交互**：`GhostWindow` 用 `ClickThrough` 虚属性区分"纯装饰（`WS_EX_TRANSPARENT` + 不参与命中）"与"可点（参与命中测试 + `MA_NOACTIVATE` 不抢焦点）"，绳头属后者；踩过的坑是 `IsHitTestVisible = ClickThrough` 写反，导致绳头永远收不到 `MouseEnter`。触发区改为恒定中间 1/3 后与绳宽解耦（实测 `triggerZone=(1040..2080)` 物理像素 = 3120 的中间 1/3，`anchor physical=(2046,0,68x100) ratio=0.67`） |
| R14 | ✅ 注入管线通过，端到端待人工 | `sizeofINPUT=40 accepted=4 lastError=0`（此前 32 字节 → `accepted=0 err=87`，被误当成权限问题）。G3-a 端到端改为半自动 `--paste-gate`：用户点一下探针窗口交出前台，程序注入 Ctrl+V 并比对长度+哈希 |

环境事实：`PerMonitorV2` 生效，`\\.\DISPLAY1` 物理 3120×2080 / 工作区 3120×1984 / `scale=2.00`；居中 `Left=308 DIP → 616 px` 换算正确。Spike 期间修掉 10 个真实缺陷（ANSI 封送导致 `GetMonitorInfo` 静默失败、`DependencyProperty.Register` 写在实例字段、热键呼出被 32ms 轮询立即收起、无单实例守卫、日志跨进程追加互相覆盖、`OpenClipboard` 无重试且 `GetLastWin32Error` 被后置的日志写入冲掉、UI 线程阻塞让 9 次批量复制塌成 1 次捕获、`INPUT` 结构体 32 字节让 `SendInput` 静默失败、退出热键只关一个窗口导致进程残留、锚点命中测试写反导致绳头收不到鼠标事件），详见 `spike/README.md`。

> G5 首轮（11-01 晚）曾被本机剪贴板不可用（R13）阻塞，跑出的是"未判定"而非失败；11-02 08:54 复验为 `PASS`。

---

## 9. 里程碑与验收

### M0 · Spike（只回答"能不能"，不写业务）
独立小工程 `spike/`，五个门禁，全过才进 M1：

- **G1 非激活遮罩**：顶边透明置顶窗口显示 3 张卡片，窗口能收放；全程**不改变前台焦点**。
- **G2 顶边悬停触发**：指针进入顶边中部 2px → 弹性垂下（带轻摇）；移开 → 收起；全屏应用下不触发。
- **G3 数据双向**：单击复制（图片+文字）后前台可 `Ctrl+V`；拖出到记事本(文字)/画图(图片)/资源管理器(伪文件)成功。
- **G4 锚点常驻**（因 §7.2 决策新增）：一条 4px 点击穿透细线常驻顶边中部，贴顶最大化窗口不受其阻挡；绳子展开时它消失、收起时回现；`onboarded.flag` 不存在时首次气泡出现，首次成功展开后永久消失。
- **G5 剪贴板监听 + 隐私闸门**（M1 第一优先级，提前在 spike 内做）：外部进程复制 → 自动入绳；带 `Clipboard Viewer Ignore` / `CanIncludeInClipboardHistory=0` 的复制 → 被拦且不落库；本程序自己点卡片的复制 → 被自写抑制忽略，不产生自我循环；条目超上限按 LRU 挤掉最旧一张。

### M1 · MVP（约 2–3 周节奏）
剪贴板历史（文字+图片）上绳、去重、100 项 LRU、固定项、单击复制、双击直贴、拖出、删除、`Ctrl+Alt+T`、托盘、设置（热区/容量/快捷键）、PrivacyGate。

### M2 · v1 完整（对齐 Tendedero 主干）
截图目录监听、录屏卡片与悬停预览、`Ctrl+Z` 回挂、拖进文件夹落地即离绳、多屏、PerMonitorV2、开机自启、i18n（**中英双语已在 spike 做完，见 §7.10**：M1 只需把它换成 `.resw` 卫星资源、补 es/ja 并把开关搬进设置窗口）、安装包与代码签名。

### M3 · v2（差异化）
自研截图与标注、Hold 悬浮对照、OCR 取词、按应用策略、拖文件到顶边挂绳。

每个里程碑的"完成"定义 = §10 测试矩阵对应行全绿。

---

## 10. 测试矩阵（手动，Release 前逐行走）

| 维度 | 必测项 |
|---|---|
| 焦点 | 记事本输入中 / Chrome 表单 / Office / 远程桌面 → 单击与双击卡片后光标仍在原处 |
| 显示器 | 单屏、主屏在上、主屏在右、混合 DPI（100%/150%/200%） |
| 内容 | 纯文本、超长文本(50KB)、emoji/代理对、富文本 HTML、RTF、PNG/JPEG/HEIC、MOV/MP4、文件列表、`CF_DROP` 伪文件 |
| 隐私 | 1Password/Bitwarden 复制→不入绳；退出后历史清空；手动开启加密后可恢复 |
| 拖出 | 记事本、微信、资源管理器、Excel、浏览器富文本框 |
| 干扰 | 全屏游戏独占、Windows 全屏应用、贴顶最大化窗口、屏保/锁屏 |
| 生命周期 | 首装（无 .NET 运行时）→ 启动 → 托盘退出 → 卸载后 `%LOCALAPPDATA%\Clipline\` 与 Run 键均无残留；第二实例提示已在运行；锁屏/解锁、显示器热插拔后锚点与绳宽重算正确 |
| 资源 | 8 小时常驻内存/CPU 曲线；100 项满载展开时延 ≤120ms |
| 单测 | 格式解析、去重哈希、PrivacyGate 规则、LRU+Pin 交互、撤销栈 |

---

## 11. 下一步（按顺序）

1. ~~定工具链~~：SDK 10.0.403 已装到 `C:\Users\KeKe\AppData\Local\dotnet`（未进系统 PATH，构建用绝对路径）。
2. ~~`ClipboardWatcher` + `PrivacyGate`~~：spike 内完成并**自动验证通过**（G5 `RESULT PASS`，2026-11-02）；`AddClipboardSequenceNumber` 闸门、fail-closed 隐私判定、LRU 都留下了可复用的实现与日志证据。
3. ~~解除本机剪贴板锁死~~：11-02 早自愈，无需注销。后续原型一律用 `Ctrl+Alt+Shift+Q` 干净退出，不再 `-Force`。（该热键本身曾有个 bug：只关绳子窗、进程残留 —— 已改为显式 `app.Shutdown()`。）
4. **手动门禁**：照 `spike/README.md` 清单跑，还缺的人工项是 G1-a/b/c（焦点是否真被抢，尤其右键菜单 + **新加的"点绳头"**）、G3-b…e（拖出到记事本/画图/资源管理器/Word）、G4-a/b（**绳头悬停/点击是否真的拉起绳子且不抢焦点**，以及它 34×50 DIP 的命中区会不会挡住贴顶窗口）、G5-c（真实截图工具上绳）。G3-a 双击直贴已过（用户实测）。
5. 查 171MB 内存来源（去掉 `DropShadowEffect` 对比一次）。
6. 门禁全绿后，按 §6 建 `src/` 骨架：把 spike 的 `Native/Zone/PrivacyGate/ClipboardWatcher` 直接迁进 `Win32/` 与 `Capture/`，其余重写。
7. **分发链路按 §6.1 落地**：M1 先做"self-contained 单文件 + 用户级 Inno 安装 + 托盘退出 + 卸载清 `%LOCALAPPDATA%\Clipline\`"，签名与自动更新留到 M2；更新必须是用户显式动作，不能后台拉包（零网络原则）。
8. 任一门禁不过 → 回到 §4 重估栈（尤其 R1/R2 失败时，Win32 分层窗口自绘或 C++/WinRT 路线要重新摆上桌）。
