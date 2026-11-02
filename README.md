# Clipline · 剪贴板晾衣绳

> Clipboard, hung on a line. 把复制过的文字、图片、截图挂在屏幕顶边的一条绳上 —— 指针一碰就垂下来，用完移开即收起，桌面不留垃圾。

**当前状态：M0 / Spike。** 目标是先用最小工程验证 Windows 上几个高风险技术点（非激活置顶窗口、顶边热区、OLE 拖出、托盘、剪贴板监听与隐私闸门）到底成不成立。设计文档见 [DESIGN.md](./DESIGN.md)，spike 结论见 [spike/README.md](./spike/README.md)。产品代码尚未开始。

## 它解决什么

`Alt+Tab` 找不到刚才复制的东西；剪贴板历史面板又大又占地方，还总要防着它偷偷上云。Clipline 把历史变成**一根绳 + 几张便签**：

- 复制即上绳，最新的一张挂在最右边，超容量时挤掉最旧的
- 单击卡片 = 复制并把焦点还给你原来的应用；双击 = 复制 + 直接粘贴到当前光标处
- 图片可以直接拖进应用（以文件形式拖出，聊天窗、文件夹、桌面都吃）；文字拖动只对接收纯文本的目标有效
- 拖出去之后卡片仍挂在绳上，随时可以再用；右键才真正"从绳上取下"
- 顶边中间 1/3 是触发区，也可以直接点那根"下拉绳"绳头（悬停即展开）
- 界面中文 / English，托盘菜单里随时切换，选择会记住

## 四条不可妥协的原则

1. **零网络** —— 没有账号、没有遥测、没有云同步，网络请求代码不允许存在于生产路径。
2. **不抢焦点** —— 所有交互都不改变你当前的前台应用（`WS_EX_NOACTIVATE` + `WM_MOUSEACTIVATE → MA_NOACTIVATE`）。
3. **尊重系统的隐私开关** —— 带 `Clipboard Viewer Ignore` / `ExcludeClipboardContentFromMonitorProcessing` / `CanIncludeInClipboardHistory=0` 标记的内容一律不入绳，密码管理器的复制按前台进程白名单拦截，判定 fail-closed。
4. **瞬时存在** —— 文字历史默认退出即清空；若要持久化则 DPAPI 加密，卸载时一并抹掉。

## 快速开始（spike）

需要 .NET 10 SDK（或直接用 self-contained 产物）。

```sh
cd spike/SpikeLine
dotnet build -c Debug
./bin/Debug/net10.0-windows/CliplineSpike.exe            # 手动体验
./bin/Debug/net10.0-windows/CliplineSpike.exe --selftest # 自动跑 G5 门禁断言
```

- 快捷键：`Ctrl+Alt+T` 开关绳子，`Ctrl+Alt+L` 开关顶边下拉绳，`Ctrl+Alt+Shift+Q` 退出
- 免安装：右下角托盘图标（Win11 默认收在 `^` 折叠区）左键开关、右键菜单退出
- 语言：托盘右键菜单里勾"中文 / English"，立即生效，记录在 `%LOCALAPPDATA%\CliplineSpike\language.txt`；没有这个文件时跟随系统界面语言
- 日志：`%TEMP%\clipline-spike.log`
- 想重看首次引导：删掉 `%LOCALAPPDATA%\CliplineSpike\onboarded.flag`

## 门禁进度

| 门禁 | 内容 | 状态 |
|---|---|---|
| G1 | 透明置顶窗口完全不抢焦点 | 🟡 自动侧证据良好，人工项待验 |
| G2 | 顶边悬停 → 绳子弹性垂下、移开收起 | ✅ |
| G3 | 复制 / 双击直贴 / 拖出到第三方应用 | 🟡 直贴已实测通过（`SendInput` 的 `INPUT` 必须 40 字节）；图片以 `FileDrop` 拖出实测 `Copy`，文字拖出实测按目标分化（编辑器 `Copy`、桌面/资源管理器 `None`） |
| G4 | 顶边"下拉绳"锚点：可悬停可点击且不抢焦点 | 🟡 已实现，人工复验中 |
| G5 | 剪贴板监听 + 隐私闸门 | ✅ `--selftest` 六条断言全绿 |
| G6 | 免安装分发的托盘图标与退出 | ✅ |

## 与 Tendedero 的关系

交互蓝本是 macOS 上的 [Tendedero](https://github.com/alejandrobujan/tendedero)（MIT）。本项目是它**面向 Windows 的重新实现与扩展**：新增文字剪贴板、双击直贴、拖出为文件、隐私闸门与托盘分发形态。名字 `Clipline = clipboard × clothesline`。

## 许可证

[MIT](./LICENSE) —— 最短、最宽松，和蓝本 Tendedero 一致。这个项目本来就是踩着别人的开源作品学的，没必要再加一层约束。

`Copyright (c) 2026 Clipline contributors`：正式建仓时把它换成你的 GitHub 名字或邮箱即可，其余不用动。
