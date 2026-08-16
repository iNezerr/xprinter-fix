using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GRA.PrintBridge.Poc.Printing;

/// <summary>Sends printer-ready RAW ESC/POS bytes through the Windows print spooler.</summary>
public sealed class RawPrinterService
{
    public IReadOnlyList<string> GetInstalledPrinterNames()
    {
        EnsureWindows();
        EnumPrinters(PrinterEnumLocal | PrinterEnumConnections, IntPtr.Zero, 4, IntPtr.Zero, 0, out var requiredBytes, out var returned);
        if (requiredBytes == 0)
        {
            return Array.Empty<string>();
        }

        var buffer = Marshal.AllocHGlobal(checked((int)requiredBytes));
        try
        {
            if (!EnumPrinters(PrinterEnumLocal | PrinterEnumConnections, IntPtr.Zero, 4, buffer, requiredBytes, out _, out returned))
            {
                throw NativeFailure("Could not enumerate installed printers");
            }

            var printers = new List<string>(checked((int)returned));
            var itemSize = Marshal.SizeOf<PrinterInfo4>();
            for (var index = 0; index < returned; index++)
            {
                var itemPointer = IntPtr.Add(buffer, checked((int)index * itemSize));
                var printer = Marshal.PtrToStructure<PrinterInfo4>(itemPointer);
                if (!string.IsNullOrWhiteSpace(printer.PrinterName))
                {
                    printers.Add(printer.PrinterName);
                }
            }

            return printers;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public string? FindInstalledPrinter(string printerName) => GetInstalledPrinterNames()
        .FirstOrDefault(installed => string.Equals(installed, printerName, StringComparison.OrdinalIgnoreCase));

    public RawPrintResult Send(string printerName, byte[] data, string documentName = "GRA Print Bridge test")
    {
        EnsureWindows();
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            throw new ArgumentException("RAW print data cannot be empty.", nameof(data));
        }

        if (!OpenPrinter(printerName, out var printerHandle, IntPtr.Zero))
        {
            throw NativeFailure($"Could not open printer {printerName}");
        }

        var documentStarted = false;
        var pageStarted = false;
        try
        {
            var documentInfo = new DocInfo1
            {
                DocumentName = documentName,
                OutputFile = null,
                DataType = "RAW",
            };

            var jobId = StartDocPrinter(printerHandle, 1, ref documentInfo);
            if (jobId == 0)
            {
                throw NativeFailure("Could not start the RAW print job");
            }

            documentStarted = true;
            if (!StartPagePrinter(printerHandle))
            {
                throw NativeFailure("Could not start the RAW print page");
            }

            pageStarted = true;
            if (!WritePrinter(printerHandle, data, checked((uint)data.Length), out var written) || written != (uint)data.Length)
            {
                throw NativeFailure($"Could not write all RAW print bytes ({written} of {data.Length})");
            }

            if (!EndPagePrinter(printerHandle))
            {
                throw NativeFailure("Could not finish the RAW print page");
            }

            pageStarted = false;
            if (!EndDocPrinter(printerHandle))
            {
                throw NativeFailure("Could not finish the RAW print job");
            }

            documentStarted = false;
            return new RawPrintResult(jobId, written);
        }
        finally
        {
            if (pageStarted)
            {
                EndPagePrinter(printerHandle);
            }

            if (documentStarted)
            {
                EndDocPrinter(printerHandle);
            }

            ClosePrinter(printerHandle);
        }
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("RAW Windows spooler printing can only run on Windows.");
        }
    }

    private static RawPrinterException NativeFailure(string context)
    {
        var errorCode = Marshal.GetLastWin32Error();
        return new RawPrinterException(context, errorCode, new Win32Exception(errorCode));
    }

    private const uint PrinterEnumLocal = 0x00000002;
    private const uint PrinterEnumConnections = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PrinterInfo4
    {
        public string? PrinterName;
        public string? ServerName;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DocInfo1
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? DocumentName;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? OutputFile;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string? DataType;
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenPrinter(string printerName, out IntPtr printerHandle, IntPtr defaults);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClosePrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumPrinters(uint flags, IntPtr name, uint level, IntPtr buffer, uint bufferSize, out uint needed, out uint returned);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint StartDocPrinter(IntPtr printerHandle, uint level, ref DocInfo1 documentInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndDocPrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool StartPagePrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndPagePrinter(IntPtr printerHandle);

    [DllImport("winspool.drv", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WritePrinter(IntPtr printerHandle, byte[] bytes, uint count, out uint written);
}

public sealed class RawPrinterException(string message, int errorCode, Exception innerException)
    : Exception(message, innerException)
{
    public int ErrorCode { get; } = errorCode;
}

public sealed record RawPrintResult(uint JobId, uint BytesWritten);
