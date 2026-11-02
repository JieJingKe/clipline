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
./bin/Debug/net10.0-windows/CliplineSpike.exe --lang en --render-preview ...         # 只改这次运行的语言（不落盘），用来出中英各一张自检图
```

快捷键：`Ctrl+Alt+T` 开关绳子；`Ctrl+Alt+L` 运行时开关顶边下拉绳（与托盘菜单里的"显示下拉绳"同一条开关）；`Ctrl+Alt+Shift+Q` 退出。
托盘：图标在右下角通知区（Win11 默认收在 `^` 折叠区里，可拖出固定），左键/双击开关绳子，右键菜单 = 显示/收起绳子 · 显示下拉绳（带勾选） · 中文 / English（带勾选） · 退出 Clipline。
语言：托盘菜单里勾选即实时切换（卡片文案、右键菜单、引导气泡、托盘提示全部跟着变），选择写入 `%LOCALAPPDATA%\CliplineSpike\language.txt`；没有这个文件时跟随系统界面语言（`zh` 开头 → 中文，其余 → 英文）。
首次引导气泡：删掉 `%LOCALAPPDATA%\CliplineSpike\onboarded.flag` 即可重看。

日志：`%TEMP%\clipline-spike.log`（每个动作都记录前台窗口，用于判定焦点是否被抢）。
绳子上的样例内容：四张**操作提示卡**（`T-1` 滚轮翻看更早的便签 / `T-2` 单击复制剪贴板 / `T-3` 双击直接粘贴到当前光标处 / `T-4` 拖动图片直接快速粘贴）+ 一张启动时现生成的图片卡 `I-1`。提示卡就是首次使用时该看见的东西，不再拿门禁说明文字当装饰；滚动那条挂最左（它讲的正是"绳两头之外还有"），"拖动图片"紧挨着能拖的那张卡。

## 手动清单（逐项打勾）

准备：打开记事本或微信聊天框，让光标停在输入区，然后把手移到屏幕顶边中部。

- [ ] **G1-a** 鼠标移到顶边中部 → 绳子垂下，**记事本里的光标仍在闪烁、没有停**
- [ ] **G1-b** 单击 `T-1` 卡片 → 日志出现 `G1 focus check ... unchanged=True`
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
- [x] **G5-a** 外部进程复制 → 绳子最左边自动挂上新卡片：`R7 captured Text text[1937634860+len19] owner=none total=1`（自动，无需人工）
- [x] **G5-b** 再复制同一段内容 → `R7 duplicate skipped`，不重复挂：`duplicates=1`
- [ ] **G5-c** 复制一张截图（`Win+Shift+S` 或 Snipaste 存剪贴板）→ 挂出图片卡，可拖出为 `.png`（**仍需人工**：spike 的图片入绳路径已实现并有单测级证据，但真实截图工具行为矩阵属 R3）
- [x] **G5-d** 带隐私标记的复制 → `R7 BLOCKED ignore format present: Clipboard Viewer Ignore` 与 `R7 BLOCKED opt-out flag CanIncludeInClipboardHistory=0`，绳子上不出现该内容
- [x] **G5-e** 本程序自己复制 → `R7 self-write ignored (owner=CliplineSpike)`，不自挂
- [x] **G5-f** 第 13 条挤掉最旧一张：`R7 LRU evicted T-2`，`cards=12`（= `MaxItems`）
- [x] **G3-a** 双击 `T-2` → 代码直接出现在记事本里。**2026-11-02 用户实测通过**（"经过测试，双击确实能自动粘贴了"）。根因见 bug #8；`--paste-gate` 可自动复跑判定
- [ ] **G3-b** 拖 `T-1` 到记事本 → 文字落进去（**已有部分证据**：日志 `G3 drag start item=T-2 formats=UnicodeText,System.String,Text` → `drag end effect=Copy`，说明文字拖出在接收纯文本的目标里是通的；同一张卡拖到桌面/资源管理器则 `effect=None`，那是目标拒绝文本，不是我方缺格式）
- [ ] **G3-c** 拖 `I-1` 到画图/微信 → 图片落进去
- [ ] **G3-d** 拖 `I-1` 到资源管理器文件夹 → 得到真实 `.png` 文件
- [ ] **G3-e** 拖 `T-1` 到 Word/浏览器富文本框 → 只有一段文字，无异常
- [ ] 常驻观察：任务管理器里 WorkingSet 是否 ≤ 80MB（**当前实测 171MB，见下**）

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
| 语言（中英文切换） | 用户可见文案全部收进 `Strings.cs` 的一张 `key → [zh, en]` 表（`Lang.T` / `Lang.Format`），日志与命令行参数**故意不进表**（那是给开发者看的，不该被语言开关影响）。切换走托盘菜单两个带勾选的菜单项，`Lang.Set` 触发 `Changed` 事件 → 卡片文字、右键菜单、引导气泡、托盘提示一起刷新；绳子宽度按固定卡片宽度算，所以换语言不需要重量绳子。默认值跟随 `CultureInfo.CurrentUICulture`，用户改过一次就落盘。实测 `language=Zh (inferred from UI culture zh; no setting file yet)` / `language=En (from …\language.txt)`，`--render-preview` 双语出图英文最长两句在 200 DIP 卡内两行放得下 |
| 锚点素材接入 | 用户给的 `<path d="…">` 直接喂 `Geometry.Parse`（WPF path mini-language），`Stretch=Uniform` 缩到 34×50 DIP，**没引任何 SVG 库**。SVG 默认 nonzero 填充、WPF 解析出 EvenOdd，正好把绳圈内孔挖掉，观感正确。实测 `physical=(2046,0,68x100) ratio=0.67 interactive=True` |
| 直贴注入（G3-a） | 根因是 `INPUT` 结构体只有 32 字节（`InputUnion` 里缺 `MOUSEINPUT`），`cbSize` 与系统期望的 40 不符 → `SendInput` 返回 0、`lastError=87`，**看起来像权限问题其实不是**。补齐后 `sizeofINPUT=40 accepted=4 lastError=0`。跨进程自动化卡在 Windows 前台锁（后台进程 `SetForegroundWindow`/`SwitchToThisWindow` 均失败，`acquired=False`），所以这条降级为半自动 `--paste-gate`；产品路径不受影响：用户双击卡片时目标应用本来就是前台。**已用户实测通过** |
| 触发区与绳宽解耦（G4-f） | 用户反馈"只有 1–2 张卡片时能触发的横向长度太短，卡片多了又太长"→ `Zone` 拆成两个函数：`Zone.Trigger(monitor, scale)` = **恒定工作区中间 1/3**（上限 900 DIP），`Zone.Span(monitor, scale, minWidthDip)` = 绳子本体宽度（`min(工作区×0.4, 900)` 起步，随卡片加宽，但封顶在屏幕装得下的张数，见下面「溢出与沿绳滚动」）。实测 3 张卡时 `window=(428,-181,704x180)`（绳宽 704 DIP）而 `triggerZone=(1040..2080)` 物理像素 = 3120 的中间 1/3，两者不再联动；`RopeChanged` 事件随之删除（锚点位置不再需要跟随绳宽重排） |
| 剪贴板监听（G5） | **全绿**：`R7 clipboard listener attached=True lastError=0`，六条断言由 `--selftest` 自动验证（2026-11-02 08:54，改锚点/卡片视觉后 09:23 复跑仍 `PASS`，11:41 换提示卡文案 + 加双语后再次 `PASS`，14:51 修完"卡片溢出绳外看不见"后仍 `PASS`，15:47 加第五张提示卡 + `--lang` 后复跑 `PASS`，并新增 `visible=6`）：`captures=10 rejections=2 selfWrites=1 duplicates=1 staleSkips=1 evictions=3 cards=12 visible=6`，`RESULT PASS`。拦截原因两条分别是 `Clipboard Viewer Ignore` 与 `CanIncludeInClipboardHistory=0` |
| 全局热键 | `RegisterHotKey` 首次 `True`；**第二个实例返回 `False`** → 证实必须单实例守卫（已加 Mutex） |
| 展开/收起 | 状态机与动画在 900ms 节奏下正常：`reveal → open=True → retract → open=False`，进程干净退出 |
| 卡片观感（R8/R4） | 四条按用户反馈修正：① **图文同宽** —— 原来文字卡 320、图片卡 200，统一成 `LineWindow.CardWidth = 200`；② **底色换牛皮纸** —— 半透明白玻璃 + 纯白字在浅色桌面上完全看不清，改成牛皮纸渐变 + 墨色字 `#332211`；③ **牛皮纸再提亮一档**（用户："有些深"）—— `#FDF7EA → #F8EFDA → #F1E2C4`（约 97% 不透明），边线 `#C9AE86`，投影不透明度 0.4 → 0.28；④ **夹子换成用户提供的晾衣夹 SVG**，银色线性渐变 `#FFFFFF → #D6DDE4 → #B9C2CC → #EDF1F5` + 淡灰描边做出金属反光，34×21.5 DIP 横跨绳线（两段 path 合并成一个 `Geometry`，靠 EvenOdd 挖出内框和两颗铆钉孔），并按用户反馈把重叠从 ~59% 压到**只有夹子下缘 1/4 高度压在便签上**（`Margin.Top = -PinHeight × 0.75`，夹口正好咬在绳线上）。配色与夹子几何收在 `KraftPaper` / `CardView.BuildClothespin` 一处，卡片本体和 `--render-preview` 共用，不会走样 |
| 新增方向（R7） | 用户要求"最新的放在最右边"：`Hang()` 由 `_cards.Insert(0, …)` 改成 `_cards.Add(…)`，LRU 淘汰从 `_cards[^1]` 改成 `_cards[0]`（最左=最旧）。这也让 §3 的去重规则（"重复项滑到最右"）和新增方向一致，不再互相矛盾 |
| 溢出与沿绳滚动（R7 / G4-g） | 用户报"便签一直在右侧新增，但桌面上看不到了"。根因：`Zone.Span` 把绳宽夹回工作区内，`Layout()` 却按**全部**卡片排布，12 张需要 2684 DIP > 绳宽 1536 DIP，最新（最右）几张直接排到窗口外被裁掉。改成蓝本 Tendedero 的两段式：`CapacityDip()` 先算屏幕装得下几张（本机 6），绳宽只长到这个张数；`PlaceCards()` 把最新一张钉在"满绳最后一格"，更旧的往左排到绳外，滚轮沿绳滑动（一格半卡宽 / notch，夹在 `(count − visible) × step`，收起归零），出绳的卡片按 90 DIP 渐隐且 `IsHitTestVisible=false`。滚轮**不走 WPF 路由**：窗口从不取得焦点，由窗口过程直接收 `WM_MOUSEWHEEL` 再调 `ScrollBy`（`PostMessage` 已验证 `scroll=110→220→330`，20 格顶到 `660/660` 不回绕，不动 = 不重复写日志）。**实测**：`--selftest` `cards=12 visible=6`，截图确认 6 张铺满绳、最新一张完整可见、旧的整齐排在左端之外；`aliveAfterWheel=True` 后仍能干净退出 |
| 视觉自检 | `--render-preview out.png` 离屏 2× 渲染"收起态（白线 + 2/3 处拉绳锚点）"和"展开态（白线 + 四张牛皮纸提示便签 + 金属银夹）"，背景从左到右由白渐变到深灰，一张图同时检查两种桌面；改文案/换语言用 `--lang en`（只改这次运行、不落盘）出中、英各一张自看，再麻烦用户，省一轮往返 |
| 内存 | WPF 基线 + 一张 `RenderTargetBitmap` 样例图 → WorkingSet **171MB**，远超 80MB 预算。需要在 M1 前判断：是 `DropShadowEffect`（位图缓存）贵，还是 WPF 基线本身就高 |

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
