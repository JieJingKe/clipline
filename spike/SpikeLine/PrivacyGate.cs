namespace Clipline.Spike;

/// <summary>
/// R7：复制内容进入历史前的四重判定。命中任意一条即拒绝入绳。
/// 格式名来自密码管理器/Windows 凭据生态的实际约定（见 DESIGN §3）。
/// </summary>
internal static class PrivacyGate
{
    private static readonly string[] IgnoreFormats =
    [
        "Clipboard Viewer Ignore",
        "ExcludeClipboardContentFromMonitorProcessing",
    ];

    private static readonly string[] OptOutFormats =
    [
        "CanIncludeInClipboardHistory",
        "CanUploadToCloudClipboard",
    ];

    private static readonly string[] SensitiveProcesses =
    [
        "1password",
        "onepassword",
        "bitwarden",
        "keepass",
        "keepassxc",
        "lastpass",
        "dashlane",
        "protonpass",
        "roboform",
    ];

    public static string? Reject()
    {
        foreach (var format in IgnoreFormats)
        {
            if (Native.IsFormatPresent(format))
            {
                return $"ignore format present: {format}";
            }
        }

        foreach (var format in OptOutFormats)
        {
            // IsClipboardFormatAvailable 不需要打开剪贴板；能读到值时按值判定，
            // 读不到（剪贴板被占用 / 全局内存锁定失败）时 fail-closed，宁可不入绳。
            if (!Native.IsFormatPresent(format))
            {
                continue;
            }

            var flag = Native.ReadFormatFlag(format);
            if (flag != true)
            {
                return flag is null
                    ? $"{format} present but unreadable → fail closed"
                    : $"opt-out flag {format}=0";
            }
        }

        var foreground = Native.ForegroundProcessName();
        if (foreground.Length > 0 && SensitiveProcesses.Any(name =>
            foreground.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            foreground.Contains(name, StringComparison.OrdinalIgnoreCase)))
        {
            return $"sensitive foreground process: {foreground}";
        }

        return null;
    }
}
