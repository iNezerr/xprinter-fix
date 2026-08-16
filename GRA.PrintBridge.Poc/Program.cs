using GRA.PrintBridge.Poc.Formatting;
using GRA.PrintBridge.Poc.Imaging;
using GRA.PrintBridge.Poc.Printing;

namespace GRA.PrintBridge.Poc;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                Console.Error.WriteLine("GRA Print Bridge POC must run on Windows because it uses the Windows print spooler.");
                return 2;
            }

            var command = args.FirstOrDefault()?.ToLowerInvariant() ?? "help";
            if (command is "help" or "--help" or "-h")
            {
                return PrintHelp();
            }

            if (command is not ("text" or "receipt" or "qr" or "raster" or "all"))
            {
                return UnknownCommand(command);
            }

            var count = ReadCount(args);
            var receiptPdfPath = command is "receipt" or "all" ? ResolveReceiptPdfPath(args) : null;
            var profile = PrinterProfile.FromEnvironment();
            var printerService = new RawPrinterService();
            var installedPrinter = printerService.FindInstalledPrinter(profile.PrinterName);
            if (installedPrinter is null)
            {
                Console.Error.WriteLine($"Printer {profile.PrinterName} was not found.");
                Console.Error.WriteLine("Installed printers: " + string.Join(", ", printerService.GetInstalledPrinterNames()));
                return 3;
            }

            Console.WriteLine($"Printer found: {installedPrinter}");
            Console.WriteLine($"Profile: {profile.Name}, {profile.WidthDots} dots, {profile.CharactersPerLine} characters per line");
            var formatter = new ReceiptFormatter(profile);

            return command switch
            {
                "text" => PrintMany(printerService, installedPrinter, formatter.CreateTextTest, "GRA ESC-POS text test", count),
                "receipt" => PrintMany(printerService, installedPrinter, () => CreatePdfReceipt(profile, receiptPdfPath!), "GRA PDF receipt", count),
                "qr" => PrintMany(printerService, installedPrinter, CreateQrTest, "GRA QR raster test", count),
                "raster" => PrintMany(printerService, installedPrinter, CreateRasterTest, "GRA raster test", count),
                "all" => PrintAll(printerService, installedPrinter, formatter, profile, receiptPdfPath!),
                _ => throw new InvalidOperationException("Validated test command did not match a print action."),
            };
        }
        catch (RawPrinterException exception)
        {
            Console.Error.WriteLine("Failed to send RAW print job.");
            Console.Error.WriteLine($"Windows error: {exception.ErrorCode} ({exception.InnerException?.Message})");
            return 4;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Print bridge error: {exception.Message}");
            return 1;
        }
    }

    private static int PrintAll(RawPrinterService service, string printerName, ReceiptFormatter formatter, PrinterProfile profile, string receiptPdfPath)
    {
        var results = new[]
        {
            PrintMany(service, printerName, formatter.CreateTextTest, "GRA ESC-POS text test", 1),
            PrintMany(service, printerName, () => CreatePdfReceipt(profile, receiptPdfPath), "GRA PDF receipt", 1),
            PrintMany(service, printerName, CreateQrTest, "GRA QR raster test", 1),
            PrintMany(service, printerName, CreateRasterTest, "GRA raster test", 1),
        };
        return results.Any(result => result != 0) ? 1 : 0;
    }

    private static int PrintMany(RawPrinterService service, string printerName, Func<byte[]> createJob, string jobName, int count)
    {
        for (var index = 1; index <= count; index++)
        {
            var data = createJob();
            Console.WriteLine($"Opening printer... ({index}/{count})");
            Console.WriteLine($"Sending {data.Length} bytes...");
            var result = service.Send(printerName, data, count == 1 ? jobName : $"{jobName} {index} of {count}");
            Console.WriteLine($"Print job accepted. Windows job ID: {result.JobId}.");
        }

        return 0;
    }

    private static byte[] CreateQrTest()
    {
        var printer = new EscPosBuilder().InitializeForMaximumDarkness().SetAlignment(Alignment.Center).WriteLine("QR TEST");
        printer.PrintRaster(QrGenerator.GenerateRaster("https://example.com/gra-print-test", maxWidthPixels: 240));
        printer.WriteLine(string.Empty).SetAlignment(Alignment.Left).WriteLine("https://example.com/gra-print-test").FeedLines(4);
        return printer.Build();
    }

    private static byte[] CreateRasterTest()
    {
        using var bitmap = RasterConverter.CreateRasterTestImage();
        var raster = RasterConverter.ToRaster(bitmap, maxWidthPixels: 384);
        var printer = new EscPosBuilder().InitializeForMaximumDarkness().SetAlignment(Alignment.Center).PrintRaster(raster).FeedLines(4);
        return printer.Build();
    }

    private static byte[] CreatePdfReceipt(PrinterProfile profile, string pdfPath)
    {
        var renderedReceipt = new PdfReceiptRenderer().RenderFirstPage(pdfPath, profile);
        Console.WriteLine($"Rendered PDF at {renderedReceipt.SourceDpi} DPI: {renderedReceipt.SourceWidthPixels} x {renderedReceipt.SourceHeightPixels} pixels.");
        Console.WriteLine($"Cropped to {renderedReceipt.CropBounds.Width} x {renderedReceipt.CropBounds.Height} pixels, then scaled proportionally to {renderedReceipt.FinalWidthPixels} x {renderedReceipt.FinalHeightPixels} pixels.");
        Console.WriteLine($"Converted to 1-bit raster using threshold {RasterConverter.MaximumDarknessThreshold}.");

        return new EscPosBuilder()
            .InitializeForMaximumDarkness()
            .SetAlignment(Alignment.Left)
            .PrintRaster(renderedReceipt.Raster)
            .FeedLines(4)
            .Build();
    }

    private static string ResolveReceiptPdfPath(string[] args)
    {
        var explicitPath = args.Length > 1 && !string.Equals(args[1], "--count", StringComparison.OrdinalIgnoreCase)
            ? args[1]
            : null;
        var candidates = new[]
        {
            explicitPath,
            Environment.GetEnvironmentVariable("GRA_RECEIPT_PDF"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "NEW RECEIPT.pdf"),
            Path.Combine(Environment.CurrentDirectory, "NEW RECEIPT.pdf"),
            Path.Combine(AppContext.BaseDirectory, "Assets", "NEW RECEIPT.pdf"),
        };

        var foundPath = candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path));
        if (foundPath is not null)
        {
            return Path.GetFullPath(foundPath);
        }

        throw new FileNotFoundException(
            "NEW RECEIPT.pdf was not found. Put it in your Windows Downloads folder, set GRA_RECEIPT_PDF, or pass its path after the receipt command.");
    }

    private static int ReadCount(string[] args)
    {
        var countIndex = Array.FindIndex(args, arg => string.Equals(arg, "--count", StringComparison.OrdinalIgnoreCase));
        if (countIndex < 0)
        {
            return 1;
        }

        if (countIndex == args.Length - 1 || !int.TryParse(args[countIndex + 1], out var count) || count is < 1 or > 20)
        {
            throw new ArgumentException("--count must be a whole number from 1 to 20.");
        }

        return count;
    }

    private static int PrintHelp()
    {
        Console.WriteLine("Usage: dotnet run -- <text|receipt [pdf-path]|qr|raster|all> [--count 1-20]");
        Console.WriteLine("receipt resolves NEW RECEIPT.pdf from Downloads by default. Set GRA_RECEIPT_PDF to override it.");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown test command: {command}");
        PrintHelp();
        return 1;
    }
}
