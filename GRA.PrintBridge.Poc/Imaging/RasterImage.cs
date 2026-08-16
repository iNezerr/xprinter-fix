namespace GRA.PrintBridge.Poc.Imaging;

/// <summary>One-bit, row-major image data in the ESC/POS GS v 0 representation.</summary>
public sealed record RasterImage(int WidthBytes, int HeightPixels, byte[] Data)
{
    public int WidthPixels => WidthBytes * 8;
}
