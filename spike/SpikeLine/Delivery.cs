using System.Windows;
using System.Windows.Media.Imaging;

namespace Clipline.Spike;

internal static class Delivery
{
    public static DataObject BuildDataObject(SpikeItem item)
    {
        var data = new DataObject();
        if (item.Kind == SpikeKind.Text)
        {
            data.SetData(DataFormats.UnicodeText, item.Display);
        }
        else
        {
            data.SetData(DataFormats.Bitmap, LoadBitmap(item.FilePath!));
            if (item.FilePath is not null && System.IO.File.Exists(item.FilePath))
            {
                data.SetData(DataFormats.FileDrop, new[] { item.FilePath });
            }
        }

        return data;
    }

    public static void Copy(SpikeItem item)
    {
        var data = BuildDataObject(item);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                Clipboard.SetDataObject(data, true);
                Log.Write($"copy ok item={item.Label} ({item.Preview}) attempt={attempt}");
                return;
            }
            catch (Exception error)
            {
                Log.Write($"copy attempt {attempt} failed: {error.GetType().Name}: {error.Message}");
                System.Threading.Thread.Sleep(50);
            }
        }

        Log.Write($"copy FAILED item={item.Label} after 5 attempts");
    }

    public static void PasteIntoForeground(SpikeItem item)
    {
        Copy(item);
        var foreground = Native.GetForegroundWindow();
        Log.Write($"paste target is still {Native.DescribeWindow(foreground)}");
        Native.SendCtrlV();
    }

    internal static BitmapSource LoadBitmap(string path, int decodeWidth = 480)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        bitmap.DecodePixelWidth = decodeWidth;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
