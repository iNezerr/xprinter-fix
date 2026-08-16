using System.Drawing;
using GRA.PrintBridge.Poc.Imaging;
using Xunit;

namespace GRA.PrintBridge.Poc.Tests;

public sealed class BitmapCropperTests
{
    [Fact]
    public void Find_content_bounds_removes_outer_white_margin_and_keeps_padding()
    {
        using var bitmap = new Bitmap(10, 10);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);
            graphics.FillRectangle(Brushes.Black, 3, 4, 2, 1);
        }

        var bounds = BitmapCropper.FindContentBounds(bitmap, contentLuminanceThreshold: 245, paddingPixels: 1);

        Assert.Equal(new Rectangle(2, 3, 4, 3), bounds);
    }
}
