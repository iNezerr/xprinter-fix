using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GRA.PrintBridge.Poc.Imaging;

/// <summary>Converts a bitmap to 1-bit black-and-white ESC/POS raster data locally.</summary>
public static class RasterConverter
{
    public static RasterImage ToRaster(Bitmap source, int maxWidthPixels, byte blackThreshold = 160)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (maxWidthPixels is <= 0 or > 384)
        {
            throw new ArgumentOutOfRangeException(nameof(maxWidthPixels), "The ZKP5803 profile allows 1 to 384 pixels.");
        }

        var targetWidth = Math.Min(source.Width, maxWidthPixels);
        var targetHeight = Math.Max(1, (int)Math.Round(source.Height * (targetWidth / (double)source.Width)));
        using var normalized = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(normalized))
        {
            graphics.Clear(Color.White);
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.DrawImage(source, new Rectangle(0, 0, targetWidth, targetHeight));
        }

        var widthBytes = (targetWidth + 7) / 8;
        var data = new byte[widthBytes * targetHeight];
        for (var y = 0; y < targetHeight; y++)
        {
            for (var x = 0; x < targetWidth; x++)
            {
                var pixel = normalized.GetPixel(x, y);
                var luminance = ((pixel.R * 299) + (pixel.G * 587) + (pixel.B * 114)) / 1000;
                if (luminance < blackThreshold)
                {
                    var offset = (y * widthBytes) + (x / 8);
                    data[offset] |= (byte)(0x80 >> (x % 8));
                }
            }
        }

        return new RasterImage(widthBytes, targetHeight, data);
    }

    public static Bitmap CreateRasterTestImage(int widthPixels = 384)
    {
        if (widthPixels is <= 0 or > 384)
        {
            throw new ArgumentOutOfRangeException(nameof(widthPixels));
        }

        var bitmap = new Bitmap(widthPixels, 132, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
        using var headingFont = new Font(FontFamily.GenericSansSerif, 22, FontStyle.Bold, GraphicsUnit.Pixel);
        using var bodyFont = new Font(FontFamily.GenericMonospace, 16, FontStyle.Regular, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.Black);
        graphics.DrawString("RASTER TEST", headingFont, brush, new PointF(8, 10));
        graphics.DrawString("1234567890", bodyFont, brush, new PointF(8, 52));
        graphics.DrawString("ABCDEFGHIJKLMNOPQRSTUVWXYZ", bodyFont, brush, new PointF(8, 82));
        return bitmap;
    }
}
