using GRA.PrintBridge.Poc.Imaging;
using GRA.PrintBridge.Poc.Models;
using GRA.PrintBridge.Poc.Printing;

namespace GRA.PrintBridge.Poc.Formatting;

/// <summary>Composes the hard-coded Phase 1 receipt with native ESC/POS text.</summary>
public sealed class ReceiptFormatter(PrinterProfile profile)
{
    private readonly PrinterProfile _profile = profile;

    public byte[] CreateReceipt(TestReceipt receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var printer = new EscPosBuilder().Initialize();
        printer.SetAlignment(Alignment.Center).SetBold(true).SetTextSize(TextSize.DoubleWidth).WriteLine("INDUSTRIAL");
        printer.WriteLine("ENGINEERING").SetTextSize(TextSize.Normal).WriteLine("CONSULTANTS LIMITED");
        printer.SetBold(false).WriteLine(string.Empty).SetBold(true).WriteLine("VAT INVOICE").SetBold(false).WriteLine(string.Empty);
        printer.SetAlignment(Alignment.Left);
        WriteWrapped(printer, $"Invoice #: {receipt.InvoiceNumber}");
        printer.WriteLine($"Date: {receipt.Date}").WriteLine(string.Empty).WriteLine("Customer:");
        printer.SetBold(true).WriteLine(receipt.CustomerName).SetBold(false).WriteLine(receipt.CustomerType);
        Separator(printer);

        foreach (var item in receipt.Items)
        {
            WriteWrapped(printer, item.Description);
            printer.WriteLine($"Qty: {item.Quantity}");
            WriteAmountRow(printer, "Price:", item.Price);
            WriteAmountRow(printer, "Amount:", item.Amount);
            printer.WriteLine(string.Empty);
        }

        Separator(printer);
        foreach (var total in receipt.Totals)
        {
            printer.SetBold(total.Label == "TOTAL");
            WriteAmountRow(printer, total.Label, total.Amount);
            printer.SetBold(false);
        }

        Separator(printer);
        printer.SetAlignment(Alignment.Center).SetBold(true).WriteLine("EVAT RECEIPT INFORMATION").SetBold(false).SetAlignment(Alignment.Left);
        printer.WriteLine(string.Empty).WriteLine("SDC ID:").WriteLine(receipt.SdcId).WriteLine(string.Empty);
        printer.WriteLine("Receipt Number:");
        WriteWrapped(printer, receipt.ReceiptNumber);
        Separator(printer);
        printer.SetAlignment(Alignment.Center);
        printer.PrintRaster(QrGenerator.GenerateRaster("https://example.com/gra-print-test", maxWidthPixels: 240));
        printer.WriteLine(string.Empty).WriteLine("Thank you").FeedLines(4);
        if (_profile.SupportsCut)
        {
            printer.CutPaper();
        }

        return printer.Build();
    }

    public byte[] CreateTextTest()
    {
        var printer = new EscPosBuilder().Initialize();
        printer.SetAlignment(Alignment.Center).SetBold(true).WriteLine("BOLD + CENTRED").SetBold(false);
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

    private void WriteAmountRow(EscPosBuilder printer, string label, string amount)
    {
        var available = _profile.CharactersPerLine - amount.Length - 1;
        var safeLabel = label.Length > available ? label[..available] : label;
        printer.WriteLine($"{safeLabel.PadRight(available)} {amount}");
    }

    private void Separator(EscPosBuilder printer) => printer.WriteLine(new string('-', _profile.CharactersPerLine));
}
