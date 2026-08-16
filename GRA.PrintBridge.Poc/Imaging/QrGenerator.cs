using System.Drawing;
using QRCoder;

namespace GRA.PrintBridge.Poc.Imaging;

/// <summary>Creates a high-contrast local QR bitmap for printers without reliable native QR firmware.</summary>
public static class QrGenerator
{
    public static RasterImage GenerateRaster(string payload, int maxWidthPixels)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCode(data);
        var pixelsPerModule = Math.Min(8, maxWidthPixels / data.ModuleMatrix.Count);
        if (pixelsPerModule < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxWidthPixels), "QR code modules cannot fit at the requested width.");
        }

        using var bitmap = qrCode.GetGraphic(pixelsPerModule, darkColor: Color.Black, lightColor: Color.White, drawQuietZones: true);
        return RasterConverter.ToRaster(bitmap, maxWidthPixels, blackThreshold: 128);
    }
}
