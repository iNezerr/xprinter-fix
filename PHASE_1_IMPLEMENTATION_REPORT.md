# GRA Print Bridge: Phase 1 Implementation Report

**Status:** Implemented locally, awaiting Windows build and physical printer verification.

## Objective delivered

Built a deliberately small Windows console proof of concept for direct printing to the existing `POS-58` installed printer, assumed to be the ZKTeco ZKP5803-compatible 58 mm thermal printer.

The implementation sends printer-ready RAW ESC/POS bytes to the Windows print spooler. It does not use Chrome, `window.print()`, PDF printing, browser print preview, Windows page-layout printing, a web UI, a database, authentication, cloud services, a Chrome extension, a PDF parser, or GRA integration.

## Project created

```text
GRA.PrintBridge.Poc.sln
├── GRA.PrintBridge.Poc/
│   ├── Program.cs
│   ├── Printing/
│   │   ├── PrinterProfile.cs
│   │   ├── RawPrinterService.cs
│   │   └── EscPosBuilder.cs
│   ├── Formatting/
│   │   ├── TextWrapper.cs
│   │   └── ReceiptFormatter.cs
│   ├── Imaging/
│   │   ├── RasterImage.cs
│   │   ├── RasterConverter.cs
│   │   └── QrGenerator.cs
│   └── Models/
│       └── TestReceipt.cs
├── GRA.PrintBridge.Poc.Tests/
│   ├── EscPosBuilderTests.cs
│   └── TextWrapperTests.cs
├── README.md
└── .gitignore
```

The application targets `.NET 8` on Windows. It uses `QRCoder` for locally generating the QR code and `System.Drawing.Common` only for local bitmap generation and conversion.

## Implemented capabilities

### Printer profile

`Printing/PrinterProfile.cs` defines a ZKP5803 profile with:

- installed printer name: `POS-58`
- paper width: 58 mm
- printable width: 48 mm
- resolution: 203 DPI
- printable width: 384 dots
- default native text width: 32 characters per line
- cutter disabled

The printer name and characters-per-line calibration can be changed without source edits:

```powershell
$env:GRA_PRINTER_NAME = "Exact Windows printer name"
$env:GRA_CHARACTERS_PER_LINE = "30"
```

### Windows RAW printing

`Printing/RawPrinterService.cs`:

1. Enumerates installed Windows printers through `EnumPrinters`.
2. Finds `POS-58` case-insensitively.
3. Opens the printer using `OpenPrinter`.
4. Starts a print job with data type `RAW` using `StartDocPrinter`.
5. Writes the supplied ESC/POS bytes using `WritePrinter`.
6. Ends the page and document cleanly.
7. Closes the printer handle even if a job fails.
8. Reports Win32 error codes and their Windows error text through `RawPrinterException`.

This keeps Windows spooler communication separate from ESC/POS command generation.

### ESC/POS command generation

`Printing/EscPosBuilder.cs` contains reusable commands for:

- printer initialization and reset (`ESC @`)
- left, centre, and right alignment (`ESC a`)
- bold on and off (`ESC E`)
- normal, double-width, double-height, and double-width-and-height text (`GS !`)
- plain native text lines
- paper feed (`ESC d`)
- 1-bit raster printing (`GS v 0`)
- native QR commands (`GS ( k`)
- optional full cut (`GS V`)

The active ZKP5803 profile has cutting disabled because the supplied printer has no cutter. Every current job feeds four lines after content.

### Receipt formatting and text wrapping

`Formatting/TextWrapper.cs` provides one reusable wrap function based on `PrinterProfile.CharactersPerLine`. It preserves words where possible and splits a single word only when it is wider than the configured printable line.

`Formatting/ReceiptFormatter.cs` builds the requested hard-coded test receipt. It uses native ESC/POS text for the company header, VAT invoice title, invoice details, customer details, item descriptions, quantities, amounts, totals, E-VAT section, separators, and thank-you message.

The formatter includes normal text, bold text, centred text, left-aligned text, right-aligned text in the text test, double-width text, separators, currency values, long-text wrapping, and controlled feed.

The supplied sample values are retained as stated. The two supplied item amounts do not equal the supplied subtotal, so this proof of concept does not recalculate, amend, or validate the tax values.

### QR code test

`Imaging/QrGenerator.cs` creates a QR code for:

```text
https://example.com/gra-print-test
```

It uses QR error-correction level Q, preserves the QR module grid, and produces a local 1-bit black-and-white ESC/POS raster image. The QR is limited to 240 dots in the receipt to leave clear side margins.

Native ESC/POS QR support is implemented as a reusable helper but is not the default runnable test path yet. The receipt and `qr` command use raster QR because actual native QR firmware support has not been verified on this specific POS-58 installation. This is the safer Phase 1 choice until it is proven physically.

### Raster image test

`Imaging/RasterConverter.cs` creates the required black-and-white raster test with:

```text
RASTER TEST
1234567890
ABCDEFGHIJKLMNOPQRSTUVWXYZ
```

It converts the bitmap locally to thresholded one-bit image data, caps its width at 384 pixels, and sends it directly with ESC/POS raster commands. Windows and Chrome are not used for image conversion or page scaling.

### Console commands and logging

`Program.cs` supports:

```powershell
dotnet run -- text
dotnet run -- receipt
dotnet run -- qr
dotnet run -- raster
dotnet run -- all
dotnet run -- receipt --count 5
```

When run from the repository root, use:

```powershell
dotnet run --project .\GRA.PrintBridge.Poc -- receipt
```

The application logs printer discovery, job opening, byte count, Windows job ID, and clear printer/spooler errors. It exits without sending a print job when `POS-58` is not installed.

## Automated tests added

`GRA.PrintBridge.Poc.Tests` includes tests for:

- wrapping normal receipt descriptions to a fixed width
- splitting a single overlong value without exceeding the configured width
- ESC/POS initialize and bold command bytes
- ESC/POS native QR store and print command bytes

## Verification completed in this workspace

- Reviewed the created project structure and the implementation files.
- Validated the project XML files with `xmllint`.
- Checked that source code does not introduce prohibited browser printing, PDF, HTTP, database, authentication, or Windows page-layout printing code.
- Checked source files for trailing whitespace.

## Verification not completed

The current workspace is macOS-based, has no installed .NET SDK, and has no connection to the physical Windows `POS-58` printer. Therefore, none of these claims has been physically verified yet:

- .NET restore/build/test success
- Windows printer enumeration
- RAW spooler acceptance
- printer darkness or sharpness
- alignment support in the installed driver/firmware
- QR scan success
- raster image result
- five consecutive successful receipts

## Required Windows validation before Phase 2

On the actual Windows printer computer:

```powershell
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet run --project .\GRA.PrintBridge.Poc -- text
dotnet run --project .\GRA.PrintBridge.Poc -- receipt
dotnet run --project .\GRA.PrintBridge.Poc -- qr
dotnet run --project .\GRA.PrintBridge.Poc -- raster
dotnet run --project .\GRA.PrintBridge.Poc -- receipt --count 5
```

Physically confirm all of the following:

1. `POS-58` is detected and each print job is accepted by the spooler.
2. Native text is dark, sharp, readable, and fully within the 48 mm printable width.
3. Bold, centre, left, right, and double-width commands behave as expected.
4. Long descriptions wrap without cut-off or large blank paper sections.
5. The receipt’s QR code scans to the configured test URL.
6. The raster test prints clearly at full width without clipping.
7. Five consecutive receipts print with consistent output, feed, and spacing.

If any right-alignment, QR, or raster behavior differs on the actual firmware, record the exact observed result and adjust only the relevant ESC/POS profile or command path. Do not start Phase 2 until the physical test results meet the Phase 1 definition of done.
