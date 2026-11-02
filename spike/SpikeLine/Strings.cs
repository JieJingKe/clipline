using System.Globalization;
using System.IO;

namespace Clipline.Spike;

internal enum AppLanguage
{
    Zh,
    En,
}

/// <summary>
/// 用户可见文案的唯一来源。持久化在 %LOCALAPPDATA%\CliplineSpike\language.txt，
/// 没有该文件时跟随系统 UI 语言（zh 开头用中文，其余用英文）。
/// 日志和命令行参数不进这张表——它们是给开发者看的，不该被语言开关影响。
/// </summary>
internal static class Lang
{
    private static readonly Dictionary<string, string[]> Table = new()
    {
        ["tray.tip.idle"] = ["Clipline · 剪贴板晾衣绳（spike）", "Clipline · clipboard clothesline (spike)"],
        ["tray.tip.open"] = ["Clipline：已展开（点托盘图标收起）", "Clipline: line is out (click the tray icon to hide it)"],
        ["tray.tip.closed"] = ["Clipline：已收起，{0} 张卡片在绳上", "Clipline: hidden, {0} cards on the line"],
        ["tray.showHide"] = ["显示 / 收起绳子", "Show / hide the line"],
        ["tray.anchor"] = ["显示下拉绳（顶边绳头）", "Show pull cord (top edge)"],
        ["tray.langZh"] = ["中文", "中文"],
        ["tray.langEn"] = ["English", "English"],
        ["tray.quit"] = ["退出 Clipline", "Quit Clipline"],
        ["ctx.copy"] = ["复制", "Copy"],
        ["ctx.remove"] = ["从绳上取下（演示删除）", "Unclip from the line (demo)"],
        ["tip.onboarding"] = ["点一下绳头，或把鼠标顶到屏幕中间这条线", "Click the cord, or push the cursor to the middle of the top edge"],
        ["hint.click"] = ["单击复制剪贴板", "Click to copy to the clipboard"],
        ["hint.doubleClick"] = ["双击直接粘贴到当前光标处", "Double-click to paste at the cursor"],
        ["hint.dragImage"] = ["拖动图片直接快速粘贴", "Drag an image to drop it straight in"],
        ["hint.scroll"] = ["滚轮翻看更早的便签", "Scroll to reach older notes"],
    };

    public static AppLanguage Current { get; private set; } = Load();

    public static event Action? Changed;

    public static string T(string key) => Table[key][(int)Current];

    public static string Format(string key, params object[] args) => string.Format(T(key), args);

    /// <summary>只改这一次运行的语言、不落盘：给 <c>--lang</c> 出图自检用，不该覆盖用户托盘里的选择。</summary>
    public static void Preview(AppLanguage language)
    {
        Current = language;
        Log.Write($"language preview={language} (not persisted)");
    }

    public static void Set(AppLanguage language)
    {
        if (language == Current)
        {
            return;
        }

        Current = language;
        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.WriteAllText(SettingPath, language.ToString());
        }
        catch (Exception error)
        {
            Log.Write($"language save failed: {error.GetType().Name}: {error.Message}");
        }

        Log.Write($"language switched to {language} (persisted at {SettingPath})");
        Changed?.Invoke();
    }

    private static string DataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CliplineSpike");

    private static string SettingPath => Path.Combine(DataDirectory, "language.txt");

    private static AppLanguage Load()
    {
        try
        {
            if (Enum.TryParse<AppLanguage>(File.ReadAllText(SettingPath).Trim(), out var saved))
            {
                Log.Write($"language={saved} (from {SettingPath})");
                return saved;
            }
        }
        catch (Exception)
        {
        }

        var system = new CultureInfo(CultureInfo.CurrentUICulture.Name).TwoLetterISOLanguageName;
        var inferred = system.Equals("zh", StringComparison.OrdinalIgnoreCase) ? AppLanguage.Zh : AppLanguage.En;
        Log.Write($"language={inferred} (inferred from UI culture {system}; no setting file yet)");
        return inferred;
    }
}
