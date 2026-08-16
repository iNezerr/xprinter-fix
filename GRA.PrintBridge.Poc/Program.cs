using GRA.PrintBridge.Poc.Formatting;
using GRA.PrintBridge.Poc.Imaging;
using GRA.PrintBridge.Poc.Models;
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
                "receipt" => PrintMany(printerService, installedPrinter, () => formatter.CreateReceipt(TestReceipt.Create()), "GRA receipt test", count),
                "qr" => PrintMany(printerService, installedPrinter, CreateQrTest, "GRA QR raster test", count),
                "raster" => PrintMany(printerService, installedPrinter, CreateRasterTest, "GRA raster test", count),
                "all" => PrintAll(printerService, installedPrinter, formatter),
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

    private static int PrintAll(RawPrinterService service, string printerName, ReceiptFormatter formatter)
    {
        var results = new[]
        {
            PrintMany(service, printerName, formatter.CreateTextTest, "GRA ESC-POS text test", 1),
            PrintMany(service, printerName, () => formatter.CreateReceipt(TestReceipt.Create()), "GRA receipt test", 1),
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
        var printer = new EscPosBuilder().Initialize().SetAlignment(Alignment.Center).SetBold(true).WriteLine("QR TEST").SetBold(false);
        printer.PrintRaster(QrGenerator.GenerateRaster("https://example.com/gra-print-test", maxWidthPixels: 240));
        printer.WriteLine(string.Empty).SetAlignment(Alignment.Left).WriteLine("https://example.com/gra-print-test").FeedLines(4);
        return printer.Build();
    }

    private static byte[] CreateRasterTest()
    {
        using var bitmap = RasterConverter.CreateRasterTestImage();
        var raster = RasterConverter.ToRaster(bitmap, maxWidthPixels: 384);
        var printer = new EscPosBuilder().Initialize().SetAlignment(Alignment.Center).PrintRaster(raster).FeedLines(4);
        return printer.Build();
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
        Console.WriteLine("Usage: dotnet run -- <text|receipt|qr|raster|all> [--count 1-20]");
        Console.WriteLine("Set GRA_PRINTER_NAME or GRA_CHARACTERS_PER_LINE to override the ZKP5803 profile.");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown test command: {command}");
        PrintHelp();
        return 1;
    }
}
