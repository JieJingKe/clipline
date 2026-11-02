# Clipline · 剪贴板晾衣绳

**Clipboard, hung on a line.**

把复制过的文字和图片，挂在屏幕顶边的一根绳上。需要时把鼠标顶到上边缘，绳子垂下来；用完移开，绳子收起，桌面什么都不留。

*Your clipboard history becomes a clothesline strung across the top edge of the screen. Push the cursor up and the line drops down with everything you copied; move away and it's gone — no window, no panel, no clutter.*

---

## 它能干什么 · What it does

- **复制即上绳** —— 你在任何应用里 `Ctrl+C` 的内容，自动挂上绳子，最新的一张在最右边
- **单击卡片** —— 重新复制到剪贴板，焦点仍然留在你原来的应用里，光标不会跳走
- **双击卡片** —— 直接粘贴到当前光标处，不用再按一次 `Ctrl+V`
- **拖动图片** —— 把图片卡拖进聊天窗、文件夹或桌面，直接发送
- **拉绳头** —— 屏幕顶边挂着一根小绳头，鼠标放上去或点一下就展开，不必精确瞄准边缘
- **托盘与热键** —— 免安装也能随时收起来、退出去
- **中文 / English** —— 托盘菜单里一键切换，选择会记住

- **Copies hang themselves** —— anything you copy in any app appears on the line, newest on the right
- **Click a card** —— copies it back to your clipboard, focus stays exactly where it was
- **Double-click a card** —— pastes straight at your cursor, no second keystroke
- **Drag an image out** —— drop it into a chat window, a folder or the desktop
- **Pull the cord** —— a small cord hangs on the top edge; hover or click it, no precision aiming needed
- **Tray and hotkeys** —— show, hide or quit at any time, even without installing anything
- **中文 / English** —— switch from the tray menu, the choice is remembered

## 四条不妥协的原则 · Four rules we don't break

1. **零网络** —— 没有账号、没有遥测、没有云同步，代码里不存在任何网络请求。
   **No network.** No account, no telemetry, no cloud sync — there is no networking code to run.
2. **绝不抢焦点** —— 所有交互都不会改变你当前的前台应用，你正在打字的地方不会被打断。
   **Never steals focus.** Whatever you're typing into stays typing into.
3. **尊重系统的隐私开关** —— 标记为"不进剪贴板历史"的内容一律不入绳；密码管理器的复制按前台进程拦截，判断不了就不入绳。
   **Honours the system's privacy flags.** Anything marked as not-for-history never reaches the line, and password managers are excluded by default.
4. **用完即清** —— 文字历史默认随进程退出清空；若开启持久化则本地加密，卸载时一并抹掉。
   **Gone when you quit.** Text history is cleared on exit by default; if you opt into persistence it's encrypted locally and wiped on uninstall.

## 上手 · Get started

需要 Windows 10 / 11 与 .NET 10 桌面运行时（或直接用发布产物）。

```sh
git clone https://github.com/JieJingKe/clipline.git
cd clipline/spike/SpikeLine
dotnet build -c Debug
./bin/Debug/net10.0-windows/CliplineSpike.exe
```

运行后：把鼠标顶到屏幕**上边缘中间**，或者点那根**小绳头**。

- 热键：`Ctrl+Alt+T` 展开 / 收起，`Ctrl+Alt+L` 显示 / 隐藏绳头，`Ctrl+Alt+Shift+Q` 退出
- 托盘：右下角图标，左键展开收起，右键菜单里可以切换语言和退出
- 绳子上最初的四张便签就是说明书：单击复制、双击粘贴、拖图片

Then: push the cursor to the **middle of the top edge**, or click the **little cord**.

- Hotkeys: `Ctrl+Alt+T` show/hide, `Ctrl+Alt+L` toggle the cord, `Ctrl+Alt+Shift+Q` quit
- Tray: left-click to show or hide, right-click for the menu (language switch and quit live there)
- The first four notes on the line are the manual: click to copy, double-click to paste, drag images

## 现在做到哪了 · Where it stands

**当前是 M0 验证阶段**：整套交互已经能完整跑通 —— 顶边呼出、复制自动入绳、隐私闸门、拖出到第三方应用、托盘退出。接下来是 M1，也就是真正的产品代码：设置窗口、历史持久化、安装包与代码签名、多显示器支持。

**This is the M0 milestone.** The whole interaction works end to end — call the line out from the edge, copies hang themselves, privacy-gated capture, drag-out to other apps, tray-based quit. M1 is next: the actual product code, with a settings window, history persistence, an installer with code signing, and multi-monitor support.

已知限制 · Known limitations:

- 常驻内存偏高，还没归因清楚 / resident memory is higher than the budget, still being tracked down
- 从托盘图标展开时，鼠标不在绳上，绳子会在短暂停留后自动收起 / opening from the tray auto-hides again after a moment, because the cursor isn't on the line
- 文字卡拖出只对接收纯文本的目标有效，图片几乎到处都行 / dragging text works in targets that accept plain text; images work almost everywhere

## 文档 · Documentation

- [DESIGN.md](./DESIGN.md) —— 设计文档：交互决策、技术选型、架构、风险登记表
- [spike/README.md](./spike/README.md) —— 验证阶段的门禁清单、实测结论与踩过的坑

## 参与 · Contributing

Issues 和 PR 都欢迎。功能类改动建议先开一个 issue 对一下方向 —— 上面那四条原则是评审的第一道关。

Issues and pull requests are welcome. For anything beyond a small fix, please open an issue first — the four rules above are the first thing a review looks at.

## 缘起 · Credit

交互蓝本是 macOS 上的 [Tendedero](https://github.com/alejandrobujan/tendedero)（MIT）。Clipline 是它面向 Windows 的重新实现与扩展：文字剪贴板、双击直贴、拖出为文件、系统隐私闸门与托盘分发形态。名字 `Clipline = clipboard × clothesline`。

*Built on the interaction design of [Tendedero](https://github.com/alejandrobujan/tendedero) for macOS (MIT). Clipline is a Windows reimplementation and extension of it: text clipboard support, double-click paste, drag-out as files, system privacy gates and a tray-based distribution model. `Clipline = clipboard × clothesline`.*

## 许可证 · License

[MIT](./LICENSE)
