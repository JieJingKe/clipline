# Spike：Clipline 晾绳门禁验证（G1–G6）

只回答"能不能"，不做产品代码。全过 → 进 M1；任一不过 → 回 DESIGN §8 改栈或改交互。

| 门禁 | 问题 | 不过的后果 |
|---|---|---|
| **G1** | 顶边透明置顶窗口能否**完全不抢焦点** | 交互根基塌了，必须换 Win32 分层窗口自绘 |
| **G2** | 屏幕顶边中部悬停 → 绳子弹性垂下、移开收起 | 触发模型要改成纯热键 |
| **G3** | 数据双向：复制/直贴/拖出到第三方应用 | 拖出若要手写 OLE，Tauri/Flutter 路线成本重估 |
| **G4** | 顶边锚点（现为可交互的"下拉绳"绳头，2/3 处 34×50 DIP）能否既不抢焦点又不挡住贴顶窗口，首次气泡只出现一次 | 锚点方案要改成纯热键或常驻把手 |
| **G5** | 真实剪贴板监听 + 隐私闸门（R7）：外部复制自动入绳、敏感复制被拦、自写不自我循环 | 「晾衣绳」退化成手动摆放，产品卖点不成立 |
| **G6** | 免安装形态下能否在右下角托盘显示图标并提供退出入口（`Shell_NotifyIcon` + 手工 HICON） | 只剩热键可退，普通用户不敢双击运行 |

## 构建与运行

本机 SDK 装在用户目录（`C:\Users\KeKe\AppData\Local\dotnet`，10.0.403），未加入系统 PATH，所以：

```sh
DN=/c/Users/KeKe/AppData/Local/dotnet/dotnet.exe
cd spike/SpikeLine
"$DN" build -c Debug          # 0 警告 0 错误
./bin/Debug/net10.0-windows/CliplineSpike.exe --selftest   # 自动跑：展开/收起 → 外部复制入绳 → 隐私拦截 → 自写抑制
./bin/Debug/net10.0-windows/CliplineSpike.exe              # 手动门禁（默认带锚点绳头）
./bin/Debug/net10.0-windows/CliplineSpike.exe --no-anchor   # 关掉锚点，回到"完全无痕迹"
./bin/Debug/net10.0-windows/CliplineSpike.exe --copy-text "任意文本" [--with-ignore] [--history-opt-out]  # 辅助进程：模拟外部程序复制
./bin/Debug/net10.0-windows/CliplineSpike.exe --paste-gate  # G3-a 半自动：点一下探针输入框，剩下的自动注入并比对
./bin/Debug/net10.0-windows/CliplineSpike.exe --render-preview C:\path\preview.png  # 离屏把绳/绳头画成 PNG，用来看观感
./bin/Debug/net10.0-windows/CliplineSpike.exe --render-tips C:\path\tips.png        # 离屏把托盘"使用小 tips"面板画成 PNG
./bin/Debug/net10.0-windows/CliplineSpike.exe --lang en --render-preview ...         # 只改这次运行的语言（不落盘），用来出中英各一张自检图
./bin/Debug/net10.0-windows/CliplineSpike.exe --quit-after 3000                      # 自检用：跑 3 秒后正常退出，不必杀进程
```

发布产物统一放在仓库根的 `dist/`（已 gitignore，不进版本库）：

```sh
cd spike/SpikeLine
"$DN" publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtraction=true \
  -p:EnableCompressionInSingleFile=true -o ../../dist/Clipline-0.1.0-win-x64
```

产物 = 单文件 `CliplineSpike.exe` + 5 个 WPF 原生 DLL + `README.txt`；发布 zip 也放在 `dist/` 下。若正在运行的实例锁住了 `bin/`，把源码复制到临时目录再 publish，最后仍需把产物拷回 `dist/`。

快捷键：`Ctrl+Alt+T` 开关绳子；`Ctrl+Alt+L` 运行时开关顶边下拉绳（与托盘菜单里的"显示下拉绳"同一条开关）；`Ctrl+Alt+Shift+Q` 退出。
托盘：图标在右下角通知区（Win11 默认收在 `^` 折叠区里，可拖出固定），左键/双击开关绳子，右键菜单 = 显示/收起绳子 · 显示下拉绳（带勾选） · 使用小 tips · 中文 / English（带勾选） · 退出 Clipline。
语言：托盘菜单里勾选即实时切换（卡片文案、右键菜单、引导气泡、托盘提示全部跟着变），选择写入 `%LOCALAPPDATA%\CliplineSpike\language.txt`；没有这个文件时跟随系统界面语言（`zh` 开头 → 中文，其余 → 英文）。
首次引导气泡：删掉 `%LOCALAPPDATA%\CliplineSpike\onboarded.flag` 即可重看。

日志：`%TEMP%\clipline-spike.log`（每个动作都记录前台窗口，用于判定焦点是否被抢）。
绳子上的内容：**启动时先把剪贴板里已有的文字/图片挂成第一张卡**（用户要求"打开软件就能看到当前内容"，而不是只收启动后的新复制），之后每次外部复制再往右挂。启动读取和常规捕获走同一条隐私闸门 + 去重路径；剪贴板里没有可入绳的内容时绳子就是空的，不塞样例卡。原来挂在绳子上的四条操作提示已移到托盘菜单的**"使用小 tips"**：点开在右下角弹出说明面板，点一下或 12 秒后自动收起，全程不抢焦点。

## 手动清单（逐项打勾）

准备：打开记事本或微信聊天框，让光标停在输入区，然后把手移到屏幕顶边中部。

- [ ] **G1-a** 鼠标移到顶边中部 → 绳子垂下，**记事本里的光标仍在闪烁、没有停**
- [ ] **G1-b** 单击绳上任意一张文字卡 → 日志出现 `G1 focus check ... unchanged=True`
- [ ] **G1-c** 右键卡片 → 菜单出现，日志 `context menu opened ... focus now=` 仍指向记事本（这条最容易翻车，WPF 菜单会抓鼠标捕获）
- [ ] **G1-d** `Alt+Tab` 列表里**看不到**这个程序（`WS_EX_TOOLWINDOW` 生效）
- [ ] **G2-a** 指针沿顶边左右移动，超出中部 40% 宽度 → 收起
- [ ] **G2-b** 指针从顶边**向下**移到卡片上 → 不收起（可从容点击）
- [ ] **G2-c** 最大化窗口贴着顶边时悬停仍能触发；全屏游戏/`mstsc` 时日志出现 `G2 suppressed reveal` 且不弹出
- [ ] **G4-a** 收起状态下，顶边**工作区 2/3 处**挂一个"拉绳"锚点（用户提供 SVG 素材，34×50 DIP，绳圈朝下）+ 首次气泡「把鼠标顶到这条线上…」；绳子本体仍是白色半透明横线
- [ ] **G4-b** 把某个窗口最大化贴顶，**鼠标穿过细线去点它的标题栏/拖动它** → 完全正常，细线不拦点击也不吞悬停
- [ ] **G4-c** 绳子展开时细线消失，收起后细线回现（日志 `G4 anchor visible=…`）
- [ ] **G4-d** 第一次成功展开后气泡永久消失（`%LOCALAPPDATA%\CliplineSpike\onboarded.flag` 出现）；想看第二次就删掉这个文件
- [ ] **G4-e** `Ctrl+Alt+L` 实时开关细线，对比"有锚点/无锚点"两种手感
- [x] **G4-f** ~~线比绳短~~ **已修**：触发区/细线/绳子共用 `Zone.ForMonitor(..., minWidthDip)`，日志 `anchorWidth=944 ropeWidth=944 match=True`；卡片变多时线一起变长
- [ ] **G4-g** 复制到第 7 张（本机绳宽装得下 6 张）→ 最新一张仍完整挂在绳上、看得见；**滚轮在绳子上滚动** → 更早的便签从左侧拉回来，淡出绳外的那张点不到（收起再展开回到最新）。自动部分已验（`--selftest cards=12 visible=6` + 截图）；滚轮也已脚本复跑（`PostMessage(WM_MOUSEWHEEL)` → `R7 scroll=110 → 220 → 330`，顶到 `660/660` 不回绕，`aliveAfterWheel=True`）。**仅剩"真实鼠标悬停滚动"受 Windows「悬停滚动非活动窗口」开关影响，需人工确认一次**
- [x] **G5-a** 外部进程复制 → 绳子最右端自动挂上新卡片：`R7 captured Text text[1937634860+len19] owner=none total=1`（自动，无需人工）
- [ ] **G5-a2** 启动即读取现有剪贴板：复制一段文字 → 完全退出 Clipline → 重新启动 → 展开绳子，第一张就是那段文字（日志 `R7 startup captured … total=1`）
- [ ] **G5-a3** 启动时剪贴板里是带隐私标记的内容（例如密码管理器刚复制的密码，或 `--copy-text "x" --with-ignore` 模拟）→ 不入绳（日志 `R7 BLOCKED … source=startup`）
- [x] **G5-b** 再复制同一段内容 → `R7 duplicate skipped`，不重复挂：`duplicates=1`
- [ ] **G5-c** 复制一张截图（`Win+Shift+S` 或 Snipaste 存剪贴板）→ 挂出图片卡，可拖出为 `.png`（**仍需人工**：spike 的图片入绳路径已实现并有单测级证据，但真实截图工具行为矩阵属 R3）
- [x] **G5-d** 带隐私标记的复制 → `R7 BLOCKED ignore format present: Clipboard Viewer Ignore` 与 `R7 BLOCKED opt-out flag CanIncludeInClipboardHistory=0`，绳子上不出现该内容
- [x] **G5-e** 本程序自己复制 → `R7 self-write ignored (owner=CliplineSpike)`，不自挂
- [x] **G5-f** 第 13 条挤掉最旧一张：`R7 LRU evicted …`，`cards=12`（= `MaxItems`）
- [x] **G3-a** 双击绳上一张文字卡 → 代码直接出现在记事本里。**2026-11-02 用户实测通过**（"经过测试，双击确实能自动粘贴了"）。根因见 bug #8；`--paste-gate` 可自动复跑判定
- [ ] **G3-b** 拖一张文字卡到记事本 → 文字落进去（**已有部分证据**：日志 `G3 drag start item=… formats=UnicodeText,System.String,Text` → `drag end effect=Copy`，说明文字拖出在接收纯文本的目标里是通的；同一张卡拖到桌面/资源管理器则 `effect=None`，那是目标拒绝文本，不是我方缺格式）
- [ ] **G3-c** 拖一张图片卡到画图/微信 → 图片落进去
- [ ] **G3-d** 拖一张图片卡到资源管理器文件夹 → 得到真实 `.png` 文件
- [ ] **G3-e** 拖一张文字卡到 Word/浏览器富文本框 → 只有一段文字，无异常
- [ ] **G6-b** 托盘右键 → "使用小 tips" → 右下角弹出说明面板，**当前应用的光标/焦点不变**；点一下面板或等 12 秒自动收起，再点菜单能再次打开（日志 `G6 tips shown at …` / `G6 tips dismissed by click` / `G6 tips auto-hidden`）
- [x] 常驻观察：任务管理器里「内存」= 私有工作集，实测 **28–36MB**（预算 80MB）：空载 commit 46MB / 私有 WS 28MB / 总 WS 99MB，12 张挂满 54MB / 36MB / 114MB。归因与对照数据见下

## 已经拿到的结论（自动部分）

| 项 | 结果 |
|---|---|
| 非激活样式下发 | `WS_EX_NOACTIVATE \| WS_EX_TOOLWINDOW` + `WM_MOUSEACTIVATE → MA_NOACTIVATE` 均下发成功，`AllowsTransparency` 与工具窗口共存无冲突 |
| DPI | manifest PerMonitorV2 生效，实测 `scale=2.00`（1560×1040 DIP / 3120×2080 物理），窗口 DIP 与物理边界换算一致：`Left=308 DIP → 616 px`，居中正确 |
| 显示器信息 | `\\.\DISPLAY1 work=(0,0,3120x1984)`，任务栏 48 DIP 在底部；顶边 `rcMonitor.Top=0` 可放绳 |
| 触发阈值 | 从 2 物理像素提到 **6**（`Zone.EdgeThresholdPx`），高分屏上 2px 一划就过；触发区宽度 = 工作区 40% 与 900 DIP 取小，并**不小于绳子所需宽度** |
| 锚点（G4） | `WS_EX_TRANSPARENT` 下发成功：`exstyle before=0x80008 → after=0x80800a8`，`transparent=True`；锚点与气泡都是独立非激活窗，绳子展开时隐藏、收起时恢复；首次展开后写 `onboarded.flag` 并永久收起气泡。**形态三轮迭代（都是用户反馈驱动）**：整条白色横杠 → 程序化小绳头（"还是不太好看"）→ 换成**用户提供的拉绳 SVG 素材**，位置从居中改到**工作区 2/3 处**；绳子本体回退成白色半透明横线 |
| 锚点可交互（G4-g） | 绳头从"纯视觉"升级成**悬停变亮 + 手型光标 + 悬停/单击都拉起绳子**。实现：`GhostWindow` 加 `ClickThrough` 虚属性区分两类窗（装饰窗 = `WS_EX_TRANSPARENT` + 不参与命中测试；绳头 = 参与命中测试但 `WM_MOUSEACTIVATE → MA_NOACTIVATE`，所以点了也不会抢走前台焦点，R1 不破）。**踩坑**：第一版写成 `IsHitTestVisible = ClickThrough`，逻辑正好反了，绳头收不到任何鼠标事件；正确写法是 `!ClickThrough`。代价：绳头那 34×50 DIP 会挡住贴顶窗口的对应小区域，G4-b 需按新形态复验 |
| 托盘（G6，免安装分发的退出入口） | 用户提出"免安装可以，但要能在右下角折叠托盘里显示图标并 quit"。实现走纯 Win32：`Shell_NotifyIcon(NIM_ADD/NIM_SETVERSION/NIM_MODIFY/NIM_DELETE)` + `CreateIcon`（手工填 32bpp BGRA 像素画：白绳 + 银夹 + 牛皮纸便签），图标尺寸按 `SM_CXSMICON` 取（本机 200% DPI → 32×32）；回调消息 `WM_APP+1` 挂在绳子窗口的 `HwndSource` 上，左键/双击 = 开关绳子，右键 = `TrackPopupMenuEx`（`TPM_RETURNCMD`）菜单"显示 / 收起绳子 · 显示下拉绳（顶边绳头，`MF_CHECKED` 反映当前状态，与 `Ctrl+Alt+L` 共用 `LineWindow.ToggleAnchor()`）· 中文 / English（各带勾选，见下一行）· 退出 Clipline"。**没有引 WinForms**：`UseWindowsForms=true` 会带来 `System.Windows.Forms` 隐式 using，让 `Application`/`Clipboard` 与 WPF 的同名类型变二义，代价大于收益。实测 `G6 tray icon added size=32x32 shellOk=True lastError=0`，退出时 `NIM_DELETE` + `DestroyIcon`。**已知取舍**：从托盘左键拉起时指针在右下角而非绳上，2.5s 宽限期一到就自动收起 —— M1 需要"托盘点开=钉住展开"这个状态，不能只靠指针位置判定 |
| 拖动出绳（G3-b/c/d） | 图片与文字走同一条 `DragDrop.DoDragDrop`，差别只在**给的格式**：图片给 `Bitmap + FileDrop + FileNameW`（spike 用真实临时文件），几乎任何目标都收，实测 `effect=Copy`；文字只给 `UnicodeText`（WPF 自动补 `Text`/`System.String`），实测落到记事本/编辑器是 `Copy`、落到桌面/资源管理器是 `None`。**结论：文字拖动不是没实现，是看目标收不收纯文本**，所以首屏第三条提示明确写"拖动图片直接快速粘贴"。生产要覆盖文字需补 `FileGroupDescriptorW` 伪文件或走"拖出即写入临时 .txt"（R2） |
| 语言（中英文切换） | 用户可见文案全部收进 `Strings.cs` 的一张 `key → [zh, en]` 表（`Lang.T` / `Lang.Format`），日志与命令行参数**故意不进表**（那是给开发者看的，不该被语言开关影响）。切换走托盘菜单两个带勾选的菜单项，`Lang.Set` 触发 `Changed` 事件 → 卡片文字、右键菜单、引导气泡、"使用小 tips"面板、托盘提示一起刷新；绳子宽度按固定卡片宽度算，所以换语言不需要重量绳子。默认值跟随 `CultureInfo.CurrentUICulture`，用户改过一次就落盘。实测 `language=Zh (inferred from UI culture zh; no setting file yet)` / `language=En (from …\language.txt)`；`--render-tips` 配合 `--lang en` 出中英两图，英文最长一条在 360 DIP 面板内折两行放得下 |
| 锚点素材接入 | 用户给的 `<path d="…">` 直接喂 `Geometry.Parse`（WPF path mini-language），`Stretch=Uniform` 缩到 34×50 DIP，**没引任何 SVG 库**。SVG 默认 nonzero 填充、WPF 解析出 EvenOdd，正好把绳圈内孔挖掉，观感正确。实测 `physical=(2046,0,68x100) ratio=0.67 interactive=True` |
| 直贴注入（G3-a） | 根因是 `INPUT` 结构体只有 32 字节（`InputUnion` 里缺 `MOUSEINPUT`），`cbSize` 与系统期望的 40 不符 → `SendInput` 返回 0、`lastError=87`，**看起来像权限问题其实不是**。补齐后 `sizeofINPUT=40 accepted=4 lastError=0`。跨进程自动化卡在 Windows 前台锁（后台进程 `SetForegroundWindow`/`SwitchToThisWindow` 均失败，`acquired=False`），所以这条降级为半自动 `--paste-gate`；产品路径不受影响：用户双击卡片时目标应用本来就是前台。**已用户实测通过** |
| 触发区与绳宽解耦（G4-f） | 用户反馈"只有 1–2 张卡片时能触发的横向长度太短，卡片多了又太长"→ `Zone` 拆成两个函数：`Zone.Trigger(monitor, scale)` = **恒定工作区中间 1/3**（上限 900 DIP），`Zone.Span(monitor, scale, minWidthDip)` = 绳子本体宽度（`min(工作区×0.4, 900)` 起步，随卡片加宽，但封顶在屏幕装得下的张数，见下面「溢出与沿绳滚动」）。实测 3 张卡时 `window=(428,-181,704x180)`（绳宽 704 DIP）而 `triggerZone=(1040..2080)` 物理像素 = 3120 的中间 1/3，两者不再联动；`RopeChanged` 事件随之删除（锚点位置不再需要跟随绳宽重排） |
| 剪贴板监听（G5） | **全绿**：`R7 clipboard listener attached=True lastError=0`，六条断言由 `--selftest` 自动验证（2026-11-02 08:54，改锚点/卡片视觉后 09:23 复跑仍 `PASS`，11:41 换提示卡文案 + 加双语后再次 `PASS`，14:51 修完"卡片溢出绳外看不见"后仍 `PASS`，15:47 加第五张提示卡 + `--lang` 后复跑 `PASS`，并新增 `visible=6`）：`captures=10 rejections=2 selfWrites=1 duplicates=1 staleSkips=1 evictions=3 cards=12 visible=6`，`RESULT PASS`。拦截原因两条分别是 `Clipboard Viewer Ignore` 与 `CanIncludeInClipboardHistory=0`。**启动即读上线后**：初始卡片数不再固定，selftest 改成按基线计数（`baselineCaptures` / `baselineRejections`），LRU 写者补到 13 次以保证仍能挤出最旧一张；上面这组数字是改动前的历史证据 |
| 启动即读剪贴板（用户 11-02 要求） | `LineWindow.OnSourceInitialized` 在 `ParkOffscreen()` 之后调 `ClipboardWatcher.CaptureCurrent()`：先把当前序列号记为已处理（紧接着的同序号通知不会再读一遍），再走与常规捕获**完全相同**的 `PrivacyGate → ReadItem → 去重` 路径。隐私闸门新增"剪贴板 owner 进程"判定——启动时"当前前台"已回不到复制发生的时刻，owner 通常就是写它的进程，密码管理器照样拦得住。实测：剪贴板有 11 字文本 → `R7 startup captured Text text[…+len11] owner=none total=1`；换成 `--with-ignore` 写入 → `R7 BLOCKED ignore format present: Clipboard Viewer Ignore source=startup`。剪贴板为空/不可读时不放任何样例卡。图片卡落在 `%TEMP%\clipline-spike-clip-*.png`，**退出时删除**（去重命中未入绳的也删），启动时先清掉上次异常退出残留，避免"每次启动读取都留一张截图" |
| 托盘"使用小 tips" | 卡片不再挂操作提示。`TipsPanel : GhostWindow`（`ClickThrough=false` + `WM_MOUSEACTIVATE → MA_NOACTIVATE`）由托盘菜单项打开，贴主屏工作区右下角、任务栏之上；高度按内容量出来，中英各自放得下；点一下收起 + 12 秒自动收起。`--render-tips` 双语出图核对过深浅两种桌面上的对比度 |
| 全局热键 | `RegisterHotKey` 首次 `True`；**第二个实例返回 `False`** → 证实必须单实例守卫（已加 Mutex） |
| 展开/收起 | 状态机与动画在 900ms 节奏下正常：`reveal → open=True → retract → open=False`，进程干净退出 |
| 卡片观感（R8/R4） | 四条按用户反馈修正：① **图文同宽** —— 原来文字卡 320、图片卡 200，统一成 `LineWindow.CardWidth = 200`；② **底色换牛皮纸** —— 半透明白玻璃 + 纯白字在浅色桌面上完全看不清，改成牛皮纸渐变 + 墨色字 `#332211`；③ **牛皮纸再提亮一档**（用户："有些深"）—— `#FDF7EA → #F8EFDA → #F1E2C4`（约 97% 不透明），边线 `#C9AE86`，投影不透明度 0.4 → 0.28；④ **夹子换成用户提供的晾衣夹 SVG**，银色线性渐变 `#FFFFFF → #D6DDE4 → #B9C2CC → #EDF1F5` + 淡灰描边做出金属反光，34×21.5 DIP 横跨绳线（两段 path 合并成一个 `Geometry`，靠 EvenOdd 挖出内框和两颗铆钉孔），并按用户反馈把重叠从 ~59% 压到**只有夹子下缘 1/4 高度压在便签上**（`Margin.Top = -PinHeight × 0.75`，夹口正好咬在绳线上）。配色与夹子几何收在 `KraftPaper` / `CardView.BuildClothespin` 一处，卡片本体和 `--render-preview` 共用，不会走样 |
| 新增方向（R7） | 用户要求"最新的放在最右边"：`Hang()` 由 `_cards.Insert(0, …)` 改成 `_cards.Add(…)`，LRU 淘汰从 `_cards[^1]` 改成 `_cards[0]`（最左=最旧）。这也让 §3 的去重规则（"重复项滑到最右"）和新增方向一致，不再互相矛盾 |
| 溢出与沿绳滚动（R7 / G4-g） | 用户报"便签一直在右侧新增，但桌面上看不到了"。根因：`Zone.Span` 把绳宽夹回工作区内，`Layout()` 却按**全部**卡片排布，12 张需要 2684 DIP > 绳宽 1536 DIP，最新（最右）几张直接排到窗口外被裁掉。改成蓝本 Tendedero 的两段式：`CapacityDip()` 先算屏幕装得下几张（本机 6），绳宽只长到这个张数；`PlaceCards()` 把最新一张钉在"满绳最后一格"，更旧的往左排到绳外，滚轮沿绳滑动（一格半卡宽 / notch，夹在 `(count − visible) × step`，收起归零），出绳的卡片按 90 DIP 渐隐且 `IsHitTestVisible=false`。滚轮**不走 WPF 路由**：窗口从不取得焦点，由窗口过程直接收 `WM_MOUSEWHEEL` 再调 `ScrollBy`（`PostMessage` 已验证 `scroll=110→220→330`，20 格顶到 `660/660` 不回绕，不动 = 不重复写日志）。**实测**：`--selftest` `cards=12 visible=6`，截图确认 6 张铺满绳、最新一张完整可见、旧的整齐排在左端之外；`aliveAfterWheel=True` 后仍能干净退出 |
| 视觉自检 | `--render-preview out.png` 离屏 2× 渲染"收起态（白线 + 2/3 处拉绳锚点）"和"展开态（白线 + 四张牛皮纸便签 + 金属银夹）"，背景从左到右由白渐变到深灰，一张图同时检查两种桌面；`--render-tips out.png` 用同样的渐变底看"使用小 tips"面板，配合 `--lang en` 出中英各一张；卡片内容现在来自用户剪贴板，预览里用固定示意文本。改完先自己看图，再麻烦用户，省一轮往返 |
| 内存（R9） | **已归因，回到预算内**。真实进程：空载 commit **46.0MB** / 私有工作集 **28.0MB** / 总 WS 99.1MB；展开 5 张 46.7 / 28.6 / 102.5；挂满 12 张 53.6 / 35.3 / 113.6。空闲 CPU 0.08%，展开动画峰值 6%、滚轮 1.7%（单核百分比）。两笔大账：①每张卡的 `DropShadowEffect` ≈ 3.5MB → 改为共享 1× 阴影位图；②WPF 的 D3D 管线 ≈ 48MB private → `SoftwareOnly`。对照数据见下 |

### 内存归因（R9：171MB → 28–36MB 私有工作集）

口径：任务管理器「内存」列 = **私有工作集**（`Win32_PerfFormattedData_PerfProc_Process.WorkingSetPrivate`）；下文另列 `PrivateMemorySize64`（提交）与 `WorkingSet64`（总工作集，含与系统共享的 WPF/.NET 页面）。所有采样都在动画结束后进行——刚挂完卡片立刻读会读到尚未释放的动画表面，数字虚高。

独立实验室（1364×180 DIP 非激活窗、11 张卡、PerMonitorV2 / 2×，源码在仓库外）：

| 实验窗配置 | 提交 | 总工作集 |
|---|---|---|
| 硬件渲染 + 每卡 `DropShadowEffect` | 129.4MB | 175.8MB |
| 硬件渲染 + 共享 1× 阴影位图 | 91.7MB | 140.3MB |
| 硬件渲染 + 完全无阴影 | 90.9MB | 138.5MB |
| 软件渲染 + 共享 1× 阴影位图 | 43.1MB | 88.9MB |
| 纯运行时（连窗口都没有） | 10.6MB | 37.6MB |

两笔账：

1. **每张卡一个 `DropShadowEffect` ≈ 3.5MB**（11 张多 38.5MB）。它不像元素属性那样只占卡片大小，WPF 会为效果准备中间表面。改成启动时离屏烘一张 **1× 的共享阴影位图**（240×172 DIP ≈ 0.16MB），每张卡只贴一次图：11 张仅比无阴影多 0.8MB，截图对比观感一致。位图必须按 **1×** 烘——2× 源图会让 WPF 给每张卡再开一块缩放表面，实测多占 16MB。
2. **WPF 的 D3D 管线本身 ≈ 48MB 提交**（同窗：硬件 91.7MB → 软件 43.1MB）。绳子只是一条 180 DIP 高的窄带，软件光栅完全够画：`RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly`。观感用真实截图对照过（文字、渐变、共享阴影都一致），CPU 代价可忽略：空闲 0.08%、展开动画峰值 6%、滚轮 1.7%（均为单核百分比）。顺带不再依赖显卡驱动，用户机器上没有"D3D 初始化失败"这一类兼容问题。

改完后的真实进程（含锚点、托盘、剪贴板监听）：

| 状态 | 提交 | 私有工作集 | 总工作集 |
|---|---|---|---|
| 空载，5 张卡（测量时预置样例）收起 | 46.0MB | 28.0MB | 99.1MB |
| 展开 5 张 | 46.7MB | 28.6MB | 102.5MB |
| 挂满 12 张 | 53.6MB | 35.3MB | 113.6MB |
| 12 张收起 | 54.1MB | 35.8MB | 114.4MB |

结论：按 Task Manager 口径（私有工作集）已回到 80MB 预算内，且挂满 12 张也只比空载多约 8MB。总工作集仍高于 80MB，其中约 60MB 是与系统共享的 WPF/.NET 运行库页面，不是本进程独占成本。

### Spike 期间发现并修掉的 12 个真实 bug

1. `MONITORINFOEX` 用 ANSI 封送，`GetMonitorInfo` 静默失败返回全 0 → 窗口坐标算成 `(-120,-181)`。修：结构体标 `CharSet.Unicode`。
2. `DependencyProperty.Register` 写在实例字段初始化里，第二个卡片就抛异常 → 进程启动即崩。修：改静态字段。
3. 热键呼出后 32ms 轮询立刻判定"指针不在绳上"并收起 → 手动模式永远看不见绳子。修：`Reveal(manual: true)` 加 2.5s 宽限期。
4. 无单实例守卫，两个实例并存互相抢热键，日志误导。修：命名 `Mutex`。
5. 辅助进程与主进程共用一个日志文件，`File.AppendAllText` 独占打开导致行丢失。修：`FileShare.ReadWrite` 追加 + 短重试。
6. 辅助进程 `OpenClipboard(NULL)` 失败后立刻打印 `GetLastWin32Error()`，但中间那次字符串构造把 last-error 冲掉，日志显示 `err=0` 误导排查。修：**先取错误码再输出**，并加 8 次重试；真实错误是 `err=5`。
7. **批量复制会塌成一次捕获**（真实产品缺陷，不是测试瑕疵）：第一轮 selftest 连发 9 次复制，结果 `captures=2 duplicates=9`。根因是我在 UI 线程 `Thread.Sleep` 做节流，9 条 `WM_CLIPBOARDUPDATE` 在队列里堆积，等 UI 空闲时剪贴板内容早已是最后一条 —— 前 8 次读到的都是同一份文本。修：用 `GetClipboardSequenceNumber()` 做**陈旧通知判定**（`staleSkips`），并把写者改成每次 tick 只派一个、绝不阻塞 UI。生产必须保留这个序列号闸门。
8. **双击直贴一直"没反应"，被误判成需要管理员权限**：日志 `SendInput(Ctrl+V) requested=4 accepted=0 lastError=87`。真实原因是 `INPUT` 结构体封送成 32 字节 —— `InputUnion` 里只放了 `KEYBDINPUT`，而 x64 上 `INPUT` = 类型(4) + 补齐(4) + `MOUSEINPUT`(32) = **40 字节**，`cbSize` 传 32 系统直接拒收。修：union 里补 `MOUSEINPUT`，并把 `sizeofINPUT` 打进日志。教训：**`accepted=0 err=87` 是参数错，不是 UIPI/权限**，别拿管理员权限当诊断手段。
9. **`Ctrl+Alt+Shift+Q` 退出只关了绳子窗口**：进程留着不退出（锚点窗、气泡窗仍是"打开"状态，`OnLastWindowClose` 不满足），exe 被占用无法重编，日志里还留下一行 `hotkey → quit` 让人以为退了。修：`QuitRequested` 事件统一走 `app.Shutdown()`。产品里这条要写成硬约束：**退出路径必须显式 shutdown，不能依赖"最后一个窗口关闭"**。
10. **绳头加交互时命中测试写反**：`GhostWindow` 构造函数里写成 `IsHitTestVisible = ClickThrough`，于是"可点击的绳头"（`ClickThrough=false`）反而拿到 `IsHitTestVisible=false`，`OnMouseEnter`/`OnLeftButtonUp` 一次都不触发 —— 现象是"鼠标放上去完全没反应"，而 exstyle 日志看起来一切正常。修：`IsHitTestVisible = !ClickThrough`。教训：**分层窗口 + WPF 命中测试是两套机制**，`WS_EX_TRANSPARENT` 只管 Win32 层，WPF 内部还有一层 `IsHitTestVisible`，两者必须同向。
11. **卡片被排到窗口外面，最新的反而看不见**（用户 11-02 报）：绳宽由 `Zone.Span` 算，里面 `Math.Clamp(..., workWidthDip - 24)` 把窗口夹回工作区；而 `Layout()` 用**全部**卡片所需宽度（12 张 = 2684 DIP）当 `minWidthDip` 去排布，窗口只有 1536 DIP，于是第 7 张之后统统落在窗口右边界之外被裁掉 —— 卡片一直在正常新增，只是永远画在看不见的地方。修：容量与排布共用同一个算法（`CapacityDip()` 先定"装得下几张"，绳宽按它长，多出来的挂左端绳外 + 滚轮取回）。教训：**同一份几何不许两处各算各的**，夹宽度的人和排卡片的人必须引用同一个张数。
12. **便签一多、滚轮一滚就闪退**（用户 11-02 报，紧接着 #11）：`PlaceCards()` 每次调用都把卡片 `_canvas.Children.Add(card)`，而上一秒 `Layout()` 已经挂过；滚一格就重复添加同一个 `UIElement`，WPF 抛 `InvalidOperationException`，UI 线程无人接住 → CLR 直接终止进程（事件日志 `APPCRASH`、`P7=e0434352`）。日志恰好停在 `G2 reveal`、没有 `R7 scroll`，因为异常发生在 `Log.Write` 之前。修：**画布归属只在 `Layout()` 里变更**，`PlaceCards()` 只负责 `Canvas.SetLeft/SetTop/Opacity`（可反复调用）；同时给 UI/后台线程都加了兜底日志 + 显式 `app.Shutdown()`，以后任何未处理异常至少留下堆栈。复验：`PostMessage(WM_MOUSEWHEEL)` 连滚 20 格 → `scroll=660/660`、不崩、`aliveAfterWheel=True`，随后 `Ctrl+Alt+Shift+Q` 正常退出。教训：**幂等的"摆位置"和一次性的"挂画布"必须分开**。

### G5 曾被本机剪贴板"锁死"阻塞（次日自愈，已复验）

2026-11-01 22:07 我用 `Stop-Process -Force` 强杀了一个正处于剪贴板写入中的实例，之后全机打不开剪贴板：

```
22:21:24.874 R7 clipboard listener attached=True lastError=0   ← 监听侧正常
22:21:27.516 helper OpenClipboard attempt 1..8 failed err=5    ← 任何写/读都进不去
```

PowerShell `Set-Clipboard` / `Get-Clipboard` 同样失败，`GetClipboardOwner()=NULL`，`CloseClipboard()` 返回
`err=1418 (ERROR_CLIPBOARD_NOT_OPEN)`。次日 08:47 再探时**已自行恢复**（`helper exit=0`），无需注销或重启 ——
所以"永久锁死"的说法要降级为"持锁方退出前一直不可用，可能是几秒也可能是几小时"。
08:54 重跑 `--selftest` 得 `RESULT PASS`。

**对产品的设计约束**（已写入 DESIGN R13）：`OpenClipboard` 成对调用且立即关闭；隐私标志位优先用
`IsClipboardFormatAvailable`（**无需打开剪贴板**）判定；取不到值时 fail-closed 不入绳；剪贴板长时间不可用时
降级为"手动挂"并提示用户，绝不静默无限重试。

## 已知简化（不代表生产实现）

- 剪贴板监听已有（`ClipboardWatcher`，观察者模式），但**没有截图目录监听**，也没有把内容存盘/建索引；隐私判定里的"前台进程白名单"只做了密码管理器名单，未做浏览器密码框形态匹配（R7 完整方案见 DESIGN §3）。
- 历史不落盘：进程退出即清空；LRU 上限在 spike 里是 12（生产 100）。
- 拖出用真实临时文件走 `DataFormats.FileDrop`；生产要做 `FileGroupDescriptorW` 伪文件 + 延迟渲染（R2）。
- 单击/双击共用 `MouseUp`：双击会先触发一次复制再直贴。生产需要 `InteractionRouter` 区分（DESIGN §5）。
- 全屏抑制靠"前台窗口覆盖整个显示器 + 进程名不是 explorer/mstsc"，是启发式，需要实测收敛（R5）。
- `DllImport` 而非 DESIGN 规定的 `[LibraryImport]`：spike 少一层 unsafe，生产代码统一改回。
- 语言只有中英两门，且是一整张硬编码表（`Strings.cs`）+ 一个落盘开关；生产按 DESIGN §6 换成 `.resw` 卫星资源，并把西/日补齐。设置仍只有"语言"和"下拉绳显不显示"两项，没有设置窗口。
- 滚轮沿绳滑动只在**绳子展开时**生效，代价是绳带那一条（约 180 DIP 高）内的滚轮被我们吃掉、不再滚下面的页面；收起状态窗口在屏幕外，不影响任何页面滚动。生产要收敛成"指针在卡片带上才接管"，或者换成拖拽/横向滑动（DESIGN §7.3）。
- 滚轮依赖 Windows 的「悬停滚动非活动窗口」设置（本机 `MouseWheelRouting=2`，Win10/11 默认开启）：关掉它，滚轮会投给前台应用而不是这根永不取焦点的绳子。生产若要完全自给，需要 `WH_MOUSE_LL` 一类的全局钩子或改为拖拽手势。
- 无撤销栈（`Ctrl+Z`）。
