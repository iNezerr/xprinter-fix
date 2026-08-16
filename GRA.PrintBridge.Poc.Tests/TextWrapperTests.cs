using GRA.PrintBridge.Poc.Formatting;
using Xunit;

namespace GRA.PrintBridge.Poc.Tests;

public sealed class TextWrapperTests
{
    [Fact]
    public void Wrap_keeps_words_inside_configured_width()
    {
        var lines = TextWrapper.Wrap("TANK CLEANING LES ABOVE 30000 LTS", 16);

        Assert.Equal(["TANK CLEANING", "LES ABOVE 30000", "LTS"], lines);
        Assert.All(lines, line => Assert.True(line.Length <= 16));
    }

    [Fact]
    public void Wrap_splits_a_word_that_is_wider_than_the_receipt()
    {
        var lines = TextWrapper.Wrap("NS260805-001-000004", 8);

        Assert.Equal(["NS260805", "-001-000", "004"], lines);
        Assert.All(lines, line => Assert.True(line.Length <= 8));
    }
}
