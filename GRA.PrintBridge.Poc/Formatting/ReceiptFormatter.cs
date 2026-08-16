using GRA.PrintBridge.Poc.Printing;

namespace GRA.PrintBridge.Poc.Formatting;

/// <summary>Composes the remaining native ESC/POS diagnostic text test.</summary>
public sealed class ReceiptFormatter(PrinterProfile profile)
{
    private readonly PrinterProfile _profile = profile;

    public byte[] CreateTextTest()
    {
        var printer = new EscPosBuilder().InitializeForMaximumDarkness();
        printer.SetAlignment(Alignment.Center).SetBold(true).WriteLine("BOLD + CENTRED");
        printer.SetTextSize(TextSize.DoubleWidth).WriteLine("LARGE").ResetTextSize();
        printer.SetAlignment(Alignment.Left).WriteLine("Left-aligned normal text");
        printer.SetAlignment(Alignment.Right).WriteLine("Right-aligned 658.00");
        printer.SetAlignment(Alignment.Left);
        Separator(printer);
        WriteWrapped(printer, "This long line proves that text is wrapped inside the configured 58mm printer width without being cut off.");
        printer.WriteLine("GHS 3,042.00").FeedLines(4);
        if (_profile.SupportsCut)
        {
            printer.CutPaper();
        }

        return printer.Build();
    }

    private void WriteWrapped(EscPosBuilder printer, string value)
    {
        foreach (var line in TextWrapper.Wrap(value, _profile.CharactersPerLine))
        {
            printer.WriteLine(line);
        }
    }

    private void Separator(EscPosBuilder printer) => printer.WriteLine(new string('-', _profile.CharactersPerLine));
}
