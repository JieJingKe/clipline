using System.Windows;
using System.Windows.Media.Imaging;

namespace Clipline.Spike;

/// <summary>
/// 观察者模式剪贴板监听：只做「外部复制 → 入绳」，绝不接管任何应用的复制动作。
/// 顺序固定为：自写抑制 → PrivacyGate → 读取 → 去重 → 抛事件。
/// </summary>
internal sealed class ClipboardWatcher
{
    private readonly HashSet<string> _seen = [];
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
        var added = Native.AddClipboardFormatListener(hwnd);
        Log.Write($"R7 clipboard listener attached={added} lastError={System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
    }

    public void Detach(IntPtr hwnd)
    {
        if (hwnd != IntPtr.Zero)
        {
            Native.RemoveClipboardFormatListener(hwnd);
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
            Log.Write($"R7 BLOCKED {rejection}");
            return;
        }

        var item = ReadItem();
        if (item is null)
        {
            Log.Write("R7 update with no capturable text/image, skipped");
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
        Log.Write($"R7 captured {item.Kind} {fingerprint} owner={Native.ClipboardOwnerProcess()} total={Captures}");
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

        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"clipline-spike-clip-{(++_imageSequence)}.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using (var stream = System.IO.File.Create(path))
        {
            encoder.Save(stream);
        }

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
