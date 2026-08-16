using GRA.PrintBridge.Poc.Printing;
using Xunit;

namespace GRA.PrintBridge.Poc.Tests;

public sealed class EscPosBuilderTests
{
    [Fact]
    public void Initialize_and_bold_emit_expected_esc_pos_commands()
    {
        var bytes = new EscPosBuilder().Initialize().SetBold(true).WriteLine("TEST").SetBold(false).Build();

        Assert.Equal(new byte[] { 0x1B, 0x40, 0x1B, 0x45, 0x01 }, bytes[..5]);
        Assert.Equal(new byte[] { 0x1B, 0x45, 0x00 }, bytes[^3..]);
    }

    [Fact]
    public void Native_qr_includes_store_and_print_commands()
    {
        var bytes = new EscPosBuilder().PrintNativeQr("https://example.com/gra-print-test").Build();

        Assert.True(ContainsSequence(bytes, new byte[] { 0x31, 0x50, 0x30 }));
        Assert.Equal(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 }, bytes[^8..]);
    }

    [Fact]
    public void Double_strike_emits_esc_g_on_and_off_commands()
    {
        var bytes = new EscPosBuilder().SetDoubleStrike(true).WriteLine("DENSE").SetDoubleStrike(false).Build();

        Assert.Equal(new byte[] { 0x1B, 0x47, 0x01 }, bytes[..3]);
        Assert.Equal(new byte[] { 0x1B, 0x47, 0x00 }, bytes[^3..]);
    }

    [Fact]
    public void Maximum_darkness_initialization_resets_then_enables_bold_and_double_strike()
    {
        var bytes = new EscPosBuilder().InitializeForMaximumDarkness().Build();

        Assert.Equal(new byte[] { 0x1B, 0x40, 0x1B, 0x45, 0x01, 0x1B, 0x47, 0x01 }, bytes);
    }

    private static bool ContainsSequence(byte[] source, byte[] expected)
    {
        for (var index = 0; index <= source.Length - expected.Length; index++)
        {
            if (source.AsSpan(index, expected.Length).SequenceEqual(expected))
            {
                return true;
            }
        }

        return false;
    }
}
