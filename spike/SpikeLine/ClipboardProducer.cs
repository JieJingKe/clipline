using System.Runtime.InteropServices;
using System.Text;

namespace Clipline.Spike;

/// <summary>
/// 辅助进程用的裸 Win32 剪贴板写入器。用 Win32 而不是 WPF Clipboard，
/// 是为了能带上「Clipboard Viewer Ignore」这类自定义隐私格式并控制其字节值。
/// </summary>
internal static class ClipboardProducer
{
    private const uint GMEM_MOVEABLE = 0x0002;
    private const uint CF_UNICODETEXT = 13;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    public static int WriteText(string text, bool withIgnore, bool historyOptOut)
    {
        // OpenClipboard 的参数必须是本进程创建的窗口；无窗口进程传 NULL（绑定到当前 task）。
        for (var attempt = 1; attempt <= 8; attempt++)
        {
            if (Native.OpenClipboard(IntPtr.Zero))
            {
                return WriteAfterOpen(text, withIgnore, historyOptOut);
            }

            var error = Marshal.GetLastWin32Error();
            Thread.Sleep(150);
            Log.Write($"helper OpenClipboard attempt {attempt} failed err={error}");
        }

        return 2;
    }

    private static int WriteAfterOpen(string text, bool withIgnore, bool historyOptOut)
    {
        try
        {
            EmptyClipboard();
            if (WriteBlob(CF_UNICODETEXT, Encoding.Unicode.GetBytes(text + "\0")) == IntPtr.Zero)
            {
                return 3;
            }

            if (withIgnore && WriteRegistered("Clipboard Viewer Ignore", [1]) == IntPtr.Zero)
            {
                return 4;
            }

            if (historyOptOut && WriteRegistered("CanIncludeInClipboardHistory", [0]) == IntPtr.Zero)
            {
                return 5;
            }

            return 0;
        }
        finally
        {
            Native.CloseClipboard();
        }
    }

    private static IntPtr WriteRegistered(string format, byte[] payload)
    {
        var id = Native.RegisterClipboardFormatW(format);
        return id == 0 ? IntPtr.Zero : WriteBlob(id, payload);
    }

    private static IntPtr WriteBlob(uint format, byte[] payload)
    {
        var block = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)payload.Length);
        if (block == IntPtr.Zero)
        {
            Console.Error.WriteLine($"GlobalAlloc failed err={Marshal.GetLastWin32Error()}");
            return IntPtr.Zero;
        }

        var address = GlobalLock(block);
        Marshal.Copy(payload, 0, address, payload.Length);
        GlobalUnlock(block);

        // SetClipboardData 成功后内存归剪贴板链所有，不能再 GlobalFree。
        return SetClipboardData(format, block);
    }
}
