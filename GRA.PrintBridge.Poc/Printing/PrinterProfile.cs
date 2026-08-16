namespace GRA.PrintBridge.Poc.Printing;

/// <summary>Physical and layout capabilities for a receipt printer.</summary>
public sealed record PrinterProfile(
    string Name,
    string PrinterName,
    int PaperWidthMm,
    int PrintableWidthMm,
    int Dpi,
    int WidthDots,
    int CharactersPerLine,
    bool SupportsCut)
{
    public static PrinterProfile Zkp5803 { get; } = new(
        Name: "ZKP5803",
        PrinterName: "POS-58",
        PaperWidthMm: 58,
        PrintableWidthMm: 48,
        Dpi: 203,
        WidthDots: 384,
        CharactersPerLine: 32,
        SupportsCut: false);

    public static PrinterProfile FromEnvironment()
    {
        var profile = Zkp5803;
        var printerName = Environment.GetEnvironmentVariable("GRA_PRINTER_NAME");
        var characters = Environment.GetEnvironmentVariable("GRA_CHARACTERS_PER_LINE");

        if (!string.IsNullOrWhiteSpace(printerName))
        {
            profile = profile with { PrinterName = printerName.Trim() };
        }

        if (!string.IsNullOrWhiteSpace(characters)
            && int.TryParse(characters, out var parsedCharacters)
            && parsedCharacters is >= 20 and <= 48)
        {
            profile = profile with { CharactersPerLine = parsedCharacters };
        }

        return profile;
    }
}
