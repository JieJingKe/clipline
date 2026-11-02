using System.Windows;
using System.Windows.Media.Imaging;

namespace Clipline.Spike;

/// <summary>
/// 观察者模式剪贴板监听：只做「外部复制 → 入绳」，绝不接管任何应用的复制动作。
/// 顺序固定为：自写抑制 → PrivacyGate → 读取 → 去重 → 抛事件。
/// </summary>
internal sealed class ClipboardWatcher
{
    private static readonly string ImagePrefix =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "clipline-spike-clip-");

    private readonly HashSet<string> _seen = [];
    private readonly List<string> _imageFiles = [];
    private int _imageSequence;
    private uint _lastSequence;

    public ClipboardWatcher()
    {
        MaxItems = 12;
    }

    public int MaxItems { get; }

    public int Captures { get; private set; }

    public int Rejections { get; private set; }

    public int SelfWrites { get; private set; }

    public int Duplicates { get; private set; }

    public int StaleSkips { get; private set; }

    public string? LastRejection { get; private set; }

    public event Action<SpikeItem>? Captured;

    public void Attach(IntPtr hwnd)
    {
        CleanStaleImages();
        var added = Native.AddClipboardFormatListener(hwnd);
        Log.Write($"R7 clipboard listener attached={added} lastError={System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
    }

    public void Detach(IntPtr hwnd)
    {
        if (hwnd != IntPtr.Zero)
        {
            Native.RemoveClipboardFormatListener(hwnd);
        }

        CleanImages();
    }

    /// <summary>图片卡片落在 %TEMP%，属于"退出即清"的一部分；启动时顺手收掉上次异常退出留下的。</summary>
    private void CleanStaleImages()
    {
        try
        {
            foreach (var path in System.IO.Directory.EnumerateFiles(
                System.IO.Path.GetTempPath(),
                "clipline-spike-clip-*.png"))
            {
                TryDelete(path);
            }
        }
        catch (Exception error)
        {
            Log.Write($"R7 stale image cleanup failed: {error.GetType().Name}: {error.Message}");
        }
    }

    private void CleanImages()
    {
        foreach (var path in _imageFiles)
        {
            TryDelete(path);
        }

        _imageFiles.Clear();
    }

    private static void TryDelete(string path)
    {
        try
        {
            System.IO.File.Delete(path);
        }
        catch (Exception)
        {
        }
    }

    public void OnClipboardUpdate()
    {
        // WM_CLIPBOARDUPDATE 会在 UI 线程忙时排队，一次批量复制可能只对应一次真实内容变化。
        // 用序列号去重，否则会把 N 次通知读成同一份内容（实测：9 次写入塌成 1 次捕获 + 8 次重复）。
        var sequence = Native.GetClipboardSequenceNumber();
        if (sequence == _lastSequence)
        {
            StaleSkips++;
            return;
        }

        _lastSequence = sequence;
        Capture("update");
    }

    /// <summary>
    /// 启动时把剪贴板里已有的内容当成第一张卡片（用户要求：打开软件就能看到当前内容，
    /// 而不是只有启动后新复制的才入绳）。隐私闸门、去重、序列号标记与常规捕获完全同路。
    /// </summary>
    public void CaptureCurrent()
    {
        // 先记住当前序列号：这次读到的内容已经入绳，紧接着若收到同序号通知不应再读一遍。
        _lastSequence = Native.GetClipboardSequenceNumber();
        Capture("startup");
    }

    private void Capture(string source)
    {
        var startup = source == "startup";

        if (Native.ClipboardOwnedByThisProcess())
        {
            SelfWrites++;
            Log.Write($"R7 self-write ignored (owner={Native.ClipboardOwnerProcess()}) captures={Captures}");
            return;
        }

        var rejection = PrivacyGate.Reject();
        if (rejection is not null)
        {
            Rejections++;
            LastRejection = rejection;
            Log.Write($"R7 BLOCKED {rejection} source={source}");
            return;
        }

        var item = ReadItem();
        if (item is null)
        {
            Log.Write(startup
                ? "R7 startup clipboard has no capturable text/image"
                : "R7 update with no capturable text/image, skipped");
            return;
        }

        var fingerprint = Fingerprint(item);
        if (!_seen.Add(fingerprint))
        {
            Duplicates++;
            Log.Write($"R7 duplicate skipped {fingerprint}");
            return;
        }

        Captures++;
        item.Label = $"C{Captures}";
        Log.Write($"R7 {(startup ? "startup " : string.Empty)}captured {item.Kind} {fingerprint} owner={Native.ClipboardOwnerProcess()} total={Captures}");
        Captured?.Invoke(item);
    }

    private SpikeItem? ReadItem()
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var image = ReadImage();
                if (image is not null)
                {
                    return image;
                }

                var text = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return new SpikeItem
                    {
                        Kind = SpikeKind.Text,
                        Label = $"C-{text.Length}",
                        Text = text,
                        IsMonospace = text.Contains('\n') || text.Contains('{') || text.Contains("=>"),
                    };
                }

                return null;
            }
            catch (Exception error)
            {
                Log.Write($"R7 read attempt {attempt} failed: {error.GetType().Name}: {error.Message}");
                System.Threading.Thread.Sleep(40);
            }
        }

        return null;
    }

    private SpikeItem? ReadImage()
    {
        if (!Clipboard.ContainsImage())
        {
            return null;
        }

        var source = Clipboard.GetImage();
        if (source is null || source.PixelWidth < 2 || source.PixelHeight < 2)
        {
            return null;
        }

        var path = $"{ImagePrefix}{++_imageSequence}.png";
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using (var stream = System.IO.File.Create(path))
        {
            encoder.Save(stream);
        }

        // 去重命中时这张卡不会入绳，但文件已经写出来了，同样要记进清理清单。
        _imageFiles.Add(path);

        return new SpikeItem
        {
            Kind = SpikeKind.Image,
            Label = $"CI-{_imageSequence}",
            FilePath = path,
        };
    }

    private static string Fingerprint(SpikeItem item) => item.Kind == SpikeKind.Text
        ? $"text[{System.HashCode.Combine(item.Text)}+len{item.Text?.Length}]"
        : $"image[{new System.IO.FileInfo(item.FilePath!).Length}]";
}
