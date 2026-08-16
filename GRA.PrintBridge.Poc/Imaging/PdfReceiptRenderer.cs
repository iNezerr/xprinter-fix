using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Docnet.Core;
using Docnet.Core.Converters;
using Docnet.Core.Models;
using GRA.PrintBridge.Poc.Printing;

namespace GRA.PrintBridge.Poc.Imaging;

/// <summary>Renders an original GRA PDF page into a cropped, proportional, one-bit receipt raster.</summary>
public sealed class PdfReceiptRenderer
{
    public const int SourceRenderDpi = 600;
    public const byte CropContentLuminanceThreshold = 245;
    public const int CropPaddingPixels = 24;

    public PdfReceiptRenderResult RenderFirstPage(string pdfPath, PrinterProfile profile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
        ArgumentNullException.ThrowIfNull(profile);
        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException("GRA receipt PDF was not found.", pdfPath);
        }

        using var document = DocLib.Instance.GetDocReader(pdfPath, new PageDimensions(SourceRenderDpi / 72d));
        if (document.GetPageCount() < 1)
        {
            throw new InvalidOperationException("The GRA receipt PDF contains no pages.");
        }

        using var page = document.GetPageReader(0);
        var sourceWidth = page.GetPageWidth();
        var sourceHeight = page.GetPageHeight();
        using var rendered = CreateBitmapFromBgra(page.GetPageWidth(), page.GetPageHeight(), page.GetImage(new NaiveTransparencyRemover(255, 255, 255)));
        var crop = BitmapCropper.FindContentBounds(rendered, CropContentLuminanceThreshold, CropPaddingPixels);
        using var cropped = rendered.Clone(crop, PixelFormat.Format32bppArgb);
        var finalHeight = Math.Max(1, (int)Math.Round(cropped.Height * (profile.WidthDots / (double)cropped.Width)));
        using var resized = ResizeProportionally(cropped, profile.WidthDots, finalHeight);
        var raster = RasterConverter.ToRaster(resized, profile.WidthDots, RasterConverter.MaximumDarknessThreshold);

        return new PdfReceiptRenderResult(
            SourceRenderDpi,
            sourceWidth,
            sourceHeight,
            crop,
            profile.WidthDots,
            finalHeight,
            raster);
    }

    private static Bitmap CreateBitmapFromBgra(int width, int height, byte[] rawBytes)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var bounds = new Rectangle(0, 0, width, height);
        var bitmapData = bitmap.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            if (rawBytes.Length != Math.Abs(bitmapData.Stride) * height)
            {
                throw new InvalidOperationException("PDF renderer returned an unexpected image buffer size.");
            }

            Marshal.Copy(rawBytes, 0, bitmapData.Scan0, rawBytes.Length);
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }
    }

    private static Bitmap ResizeProportionally(Bitmap source, int width, int height)
    {
        var resized = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using var graphics = Graphics.FromImage(resized);
        graphics.Clear(Color.White);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        return resized;
    }
}

public sealed record PdfReceiptRenderResult(
    int SourceDpi,
    int SourceWidthPixels,
    int SourceHeightPixels,
    Rectangle CropBounds,
    int FinalWidthPixels,
    int FinalHeightPixels,
    RasterImage Raster);
