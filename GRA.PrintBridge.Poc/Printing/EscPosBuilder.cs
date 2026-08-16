using System.Text;
using GRA.PrintBridge.Poc.Imaging;

namespace GRA.PrintBridge.Poc.Printing;

/// <summary>Produces ESC/POS commands without any knowledge of the Windows spooler.</summary>
public sealed class EscPosBuilder
{
    private readonly List<byte> _bytes = [];
    // Phase 1 receipts contain ASCII-only invoice fields. This avoids an implicit code-page dependency.
    private readonly Encoding _encoding = Encoding.ASCII;

    public EscPosBuilder Initialize()
    {
        _bytes.AddRange([0x1B, 0x40]);
        return this;
    }

    public EscPosBuilder SetAlignment(Alignment alignment)
    {
        _bytes.AddRange([0x1B, 0x61, (byte)alignment]);
        return this;
    }

    public EscPosBuilder SetBold(bool enabled)
    {
        _bytes.AddRange([0x1B, 0x45, enabled ? (byte)1 : (byte)0]);
        return this;
    }

    public EscPosBuilder SetTextSize(TextSize size)
    {
        _bytes.AddRange([0x1D, 0x21, (byte)size]);
        return this;
    }

    public EscPosBuilder ResetTextSize() => SetTextSize(TextSize.Normal);

    public EscPosBuilder WriteLine(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _bytes.AddRange(_encoding.GetBytes(value.Replace('\r', ' ').Replace('\n', ' ')));
        _bytes.Add(0x0A);
        return this;
    }

    public EscPosBuilder FeedLines(byte lineCount)
    {
        _bytes.AddRange([0x1B, 0x64, lineCount]);
        return this;
    }

    public EscPosBuilder PrintRaster(RasterImage image)
    {
        ArgumentNullException.ThrowIfNull(image);
        _bytes.AddRange([0x1D, 0x76, 0x30, 0x00]);
        _bytes.Add((byte)(image.WidthBytes & 0xFF));
        _bytes.Add((byte)(image.WidthBytes >> 8));
        _bytes.Add((byte)(image.HeightPixels & 0xFF));
        _bytes.Add((byte)(image.HeightPixels >> 8));
        _bytes.AddRange(image.Data);
        return this;
    }

    /// <summary>Uses the ESC/POS native QR feature. Use raster QR if the printer firmware does not support this command set.</summary>
    public EscPosBuilder PrintNativeQr(string value, byte moduleSize = 5, byte errorCorrection = 48)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var content = Encoding.UTF8.GetBytes(value);
        if (content.Length > 7092)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "QR payload is too large for ESC/POS.");
        }

        _bytes.AddRange([0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00]);
        _bytes.AddRange([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, moduleSize]);
        _bytes.AddRange([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, errorCorrection]);
        var payloadLength = content.Length + 3;
        _bytes.AddRange([0x1D, 0x28, 0x6B, (byte)(payloadLength & 0xFF), (byte)(payloadLength >> 8), 0x31, 0x50, 0x30]);
        _bytes.AddRange(content);
        _bytes.AddRange([0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30]);
        return this;
    }

    public EscPosBuilder CutPaper()
    {
        _bytes.AddRange([0x1D, 0x56, 0x00]);
        return this;
    }

    public byte[] Build() => _bytes.ToArray();
}

public enum Alignment : byte
{
    Left = 0,
    Center = 1,
    Right = 2,
}

public enum TextSize : byte
{
    Normal = 0x00,
    DoubleWidth = 0x10,
    DoubleHeight = 0x01,
    DoubleWidthAndHeight = 0x11,
}
