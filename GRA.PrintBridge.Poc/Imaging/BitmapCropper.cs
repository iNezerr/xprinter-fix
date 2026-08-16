using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace GRA.PrintBridge.Poc.Imaging;

/// <summary>Finds the visible receipt bounds while treating transparent pixels as white.</summary>
public static class BitmapCropper
{
    public static Rectangle FindContentBounds(Bitmap bitmap, byte contentLuminanceThreshold, int paddingPixels)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (paddingPixels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(paddingPixels));
        }

        using var normalized = EnsureArgb(bitmap);
        var bounds = new Rectangle(0, 0, normalized.Width, normalized.Height);
        var bitmapData = normalized.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var rowBytes = Math.Abs(bitmapData.Stride);
            var pixels = new byte[rowBytes * normalized.Height];
            Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);
            var left = normalized.Width;
            var top = normalized.Height;
            var right = -1;
            var bottom = -1;

            for (var y = 0; y < normalized.Height; y++)
            {
                var rowOffset = y * rowBytes;
                for (var x = 0; x < normalized.Width; x++)
                {
                    var pixelOffset = rowOffset + (x * 4);
                    var blue = pixels[pixelOffset];
                    var green = pixels[pixelOffset + 1];
                    var red = pixels[pixelOffset + 2];
                    var alpha = pixels[pixelOffset + 3];
                    var luminance = CompositeOnWhiteLuminance(red, green, blue, alpha);
                    if (luminance > contentLuminanceThreshold)
                    {
                        continue;
                    }

                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }

            if (right < left || bottom < top)
            {
                throw new InvalidOperationException("The rendered PDF contains no visible content to print.");
            }

            return Rectangle.FromLTRB(
                Math.Max(0, left - paddingPixels),
                Math.Max(0, top - paddingPixels),
                Math.Min(normalized.Width, right + paddingPixels + 1),
                Math.Min(normalized.Height, bottom + paddingPixels + 1));
        }
        finally
        {
            normalized.UnlockBits(bitmapData);
        }
    }

    private static Bitmap EnsureArgb(Bitmap source)
    {
        if (source.PixelFormat == PixelFormat.Format32bppArgb)
        {
            return (Bitmap)source.Clone();
        }

        var copy = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(copy);
        graphics.Clear(Color.White);
        graphics.DrawImageUnscaled(source, 0, 0);
        return copy;
    }

    private static int CompositeOnWhiteLuminance(byte red, byte green, byte blue, byte alpha)
    {
        var compositedRed = ((red * alpha) + (255 * (255 - alpha))) / 255;
        var compositedGreen = ((green * alpha) + (255 * (255 - alpha))) / 255;
        var compositedBlue = ((blue * alpha) + (255 * (255 - alpha))) / 255;
        return ((compositedRed * 299) + (compositedGreen * 587) + (compositedBlue * 114)) / 1000;
    }
}
