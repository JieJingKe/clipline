# Clipline · 剪贴板晾衣绳

**Clipboard, hung on a line.**

复制过的文字和图片，就挂在屏幕顶边的一根绳上。鼠标顶上去，绳子垂下来；移开，绳子收起——桌面干干净净，像什么都没发生过。

*Everything you copy gets hung on a clothesline strung along the top edge of the screen. Push the cursor up and the line drops down with your notes; move away and it's gone — no window, no panel, no clutter. That's the whole trick, really.*

---

## 它能干什么 · What it does

- **复制即上绳** —— 在任何应用里 `Ctrl+C`，内容自动挂上绳，最新的一张在最右边
- **打开就在绳上** —— 启动时剪贴板里有什么，就先挂什么，不用再复制一遍
- **滚动看更早的** —— 绳上放不下的便签挂在绳外，滚轮一拉就回来
- **单击卡片** —— 复制回剪贴板，焦点还留在你原来的应用里，光标不会跳走
- **双击卡片** —— 直接粘贴到光标处，省掉那一下 `Ctrl+V`
- **拖动图片** —— 把图片卡拖进聊天窗、文件夹或桌面，松手就发
- **拉绳头** —— 顶边挂着一根小绳头，鼠标放上去或点一下就行，不用瞄准边缘
- **托盘与热键** —— 免安装，也能随时收起、随时退出；忘了怎么用，托盘菜单里有"使用小 tips"
- **中文 / English** —— 托盘菜单一键切换，而且记得你的选择

- **Copies hang themselves** — hit `Ctrl+C` in any app and it shows up on the line, newest on the right
- **Already on the line at launch** — whatever is on your clipboard when Clipline starts is hung as the first card, no need to copy it twice
- **Scroll for older notes** — what doesn't fit hangs off the end of the rope; the wheel pulls it back
- **Click a card** — puts it back on your clipboard, and focus stays exactly where you left it
- **Double-click a card** — pastes straight at your cursor, saving you that second keystroke
- **Drag an image out** — drop it into a chat window, a folder or the desktop
- **Pull the cord** — a little cord hangs from the top edge; hover or click it, no precision aiming required
- **Tray and hotkeys** — show it, hide it or quit any time, no installer needed; if you forget the gestures, there's **Tips** in the tray menu
- **中文 / English** — switch in the tray menu, and it remembers your choice

## 四条不妥协的原则 · Four rules we don't break

1. **零网络** —— 没有账号、没有遥测、没有云同步，代码里根本不存在联网这回事。
   **No network.** No account, no telemetry, no cloud sync — there is no networking code to run.
2. **绝不抢焦点** —— 无论怎么点、怎么拖，都不会改变你当前的前台应用，正在打字的地方不会被打断。
   **Never steals focus.** Whatever you're typing into keeps typing into it.
3. **尊重系统的隐私开关** —— 标记为"不进剪贴板历史"的内容一律不入绳；密码管理器的复制按前台进程拦截，拿不准就不收。
   **Honours the system's privacy flags.** Anything marked as not-for-history never reaches the line, and password managers are excluded by default. When in doubt, we don't take it.
4. **用完即清** —— 文字历史默认随进程退出清空；若开启持久化则本地加密，卸载时一并抹掉。
   **Gone when you quit.** Text history is cleared on exit by default; if you opt into persistence it's encrypted locally and wiped on uninstall.

## 上手 · Get started

**最省事的办法：从 [Latest release](https://github.com/JieJingKe/clipline/releases/latest) 下载 `Clipline-0.1.0-win-x64.zip`，解压，双击 `CliplineSpike.exe`，完事。** 免安装，也不改系统设置；哪天不想用了，把文件夹一删就干净了。它是未签名的早期版本，Windows 可能会皱眉头提示"未知发布者"，属正常现象。

**The short path: grab `Clipline-0.1.0-win-x64.zip` from [Latest release](https://github.com/JieJingKe/clipline/releases/latest), unzip it, double-click `CliplineSpike.exe`, done.** Nothing is installed and no system settings are touched — deleting the folder is a perfectly good uninstall. It's an unsigned early build, so Windows may give you the "unknown publisher" look. That's expected.

喜欢折腾的话，也可以从源码跑（需要 Windows 10 / 11 与 .NET 10 桌面运行时）：

If you'd rather build it yourself (Windows 10 / 11 and the .NET 10 desktop runtime):

```sh
git clone https://github.com/JieJingKe/clipline.git
cd clipline/spike/SpikeLine
dotnet build -c Debug
./bin/Debug/net10.0-windows/CliplineSpike.exe
```

运行后：把鼠标顶到屏幕**上边缘中间**，或者点那根**小绳头**。

- 热键：`Ctrl+Alt+T` 展开 / 收起，`Ctrl+Alt+L` 显示 / 隐藏绳头，`Ctrl+Alt+Shift+Q` 退出
- 托盘：右下角图标，左键展开收起，右键菜单里可以切换语言、打开"使用小 tips"和退出
- 启动前已经复制的文字或图片会直接挂在绳上；用法想不起来时，托盘右键 → "使用小 tips"

Then: push the cursor to the **middle of the top edge**, or click the **little cord**.

- Hotkeys: `Ctrl+Alt+T` show/hide, `Ctrl+Alt+L` toggle the cord, `Ctrl+Alt+Shift+Q` quit
- Tray: left-click to show or hide, right-click for the menu (language switch, **Tips**, and quit live there)
- Whatever is already on the clipboard at launch is hung on the line; if you forget the gestures, open **Tips** from the tray menu

## 现在做到哪了 · Where it stands

**目前还在 M0 验证阶段。** 整套交互已经能跑通：顶边呼出、复制自动入绳、隐私闸门、拖出到第三方应用、托盘退出——能用的部分是真能用。接下来是 M1，也就是把验证代码变成正经产品：设置窗口、历史持久化、安装包与代码签名、多显示器支持。路还长，请多包涵。

**Still at M0, the spike stage.** The whole interaction works end to end — call the line out from the edge, copies hang themselves, privacy gates hold, drag-out works in real apps, and you can quit from the tray. What works, genuinely works. M1 is next: turning the spike into an actual product, with a settings window, history persistence, a signed installer, and multi-monitor support. Early days — thanks for your patience.

它还有几处小脾气 · A few quirks it still has:

- 从托盘图标展开时，鼠标不在绳上，绳子会在短暂停留后自己收起 —— 从托盘打开的话，记得把鼠标挪上去
  Opening from the tray: the cursor isn't on the line, so it tucks itself away after a moment — move the cursor up quickly
- 文字卡拖出，只对接收纯文本的目标有效；图片卡几乎拖到哪都行
  Dragging text only works in targets that accept plain text; images work almost anywhere
- **Win+V 里的旧内容搬不上来** —— Windows 自带的剪贴板历史里，点一条旧内容虽然能粘贴出来，但它不会挂到绳子上：系统在还原这类条目时会明确标记"不要给剪贴板监视工具看"，我们尊重这个标记。想把旧内容搬上绳，可以先粘到输入框，再在那里 `Ctrl+A`、`Ctrl+C` 复制一次。绳子上的记录从启动那一刻开始算，启动时会把剪贴板里当时的那一条挂上去。
  **Old items from Win+V won't land on the line** — selecting an item in Windows' own clipboard history does paste it, but it won't hang on the line: Windows explicitly marks restored items as "don't show this to clipboard monitors", and we honour that mark. To bring an old item across, paste it first, then select and copy it again. The line's own history starts when Clipline starts, plus whatever was already on the clipboard at launch.

## 文档 · Documentation

- [DESIGN.md](./DESIGN.md) —— 设计文档：交互决策、技术选型、架构、风险登记表 / the design document: interaction decisions, tech choices, architecture, risk register
- [spike/README.md](./spike/README.md) —— 验证阶段的门禁清单、实测结论与踩过的坑 / the spike notes: gate checklist, measured results, and the holes we fell into

## 参与 · Contributing

Issues 和 PR 都欢迎。改功能之前，建议先开个 issue 聊聊方向——上面那四条原则是这个小项目的底线，也是我们唯一比较固执的地方。

Issues and pull requests are welcome. For anything bigger than a small fix, please open an issue first so we can agree on the direction — the four rules above are the one thing this little project is stubborn about.

## 缘起 · Credit

交互灵感来自 macOS 上的 [Tendedero](https://github.com/alejandrobujan/tendedero)（MIT），一个很有意思的小工具；也谢谢科技博主'玄离199'，让我们知道了这个项目，顺便谢谢所有愿意把好东西开源出来的博主和开发者。Clipline 是 Tendedero 面向 Windows 的一次重写与扩展：文字剪贴板、双击直贴、拖出为文件、系统隐私闸门、托盘分发——说到底，它只是站在巨人肩膀上做出来的一个小玩具，路还不太会走。名字嘛，`Clipline = clipboard × clothesline`。

*The interaction is borrowed from [Tendedero](https://github.com/alejandrobujan/tendedero), a delightful little macOS app (MIT). Thanks also to the tech creator 玄离199, who put it on our radar, and to everyone who shares good work openly. Clipline rethinks Tendedero for Windows and then takes it a few steps further: text clipboard, double-click paste, drag-out as files, system privacy gates, and a tray-first way to ship it. All the same, it's a small toy standing on the shoulders of giants, and still learning to walk. The name: `Clipline = clipboard × clothesline`.*

## 许可证 · License

[MIT](./LICENSE) —— 拿去做点好玩的东西吧。

*MIT — go make something fun with it.*
