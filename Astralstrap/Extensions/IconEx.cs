using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bloxstrap.Extensions
{
    public static class IconEx
    {
        public static Icon GetSized(this Icon icon, int width, int height) => new(icon, new System.Drawing.Size(width, height));

        public static ImageSource GetImageSource(this Icon icon, bool handleException = true)
        {
            try
            {
                var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                    icon.Handle,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

                bitmapSource.Freeze();
                return bitmapSource;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("IconEx::GetImageSource", ex);

                try
                {
                    using var bmp = icon.ToBitmap();
                    using var ms = new MemoryStream();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);

                    var bmpImg = new BitmapImage();
                    bmpImg.BeginInit();
                    bmpImg.StreamSource = ms;
                    bmpImg.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImg.EndInit();
                    bmpImg.Freeze();
                    return bmpImg;
                }
                catch
                {
                    return new BitmapImage(new Uri("pack://application:,,,/Astralstrap.png"));
                }
            }
        }
    }
}
