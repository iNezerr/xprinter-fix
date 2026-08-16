# GRA Print Bridge, Phase 1 POC

This is a Windows-only console proof of concept for the installed `POS-58` ZKTeco ZKP5803-compatible thermal printer. It sends printer-ready RAW ESC/POS bytes through the Windows spooler. It does not use Chrome, PDFs, a normal Windows page-layout print path, a web app, or a GRA integration.

## Project structure

```text
GRA.PrintBridge.Poc.sln
├── GRA.PrintBridge.Poc/
│   ├── Program.cs                         Console commands and clear logging
│   ├── Printing/
│   │   ├── PrinterProfile.cs              ZKP5803 physical/layout configuration
│   │   ├── RawPrinterService.cs           Windows RAW spooler implementation
│   │   └── EscPosBuilder.cs               Native ESC/POS command builder
│   ├── Formatting/
│   │   ├── TextWrapper.cs                 Reusable width-aware wrapping
│   │   └── ReceiptFormatter.cs            Hard-coded Phase 1 receipt layout
│   ├── Imaging/
│   │   ├── RasterImage.cs                 ESC/POS 1-bit raster representation
│   │   ├── RasterConverter.cs             Local bitmap to 1-bit conversion
│   │   └── QrGenerator.cs                 High-contrast QR raster generation
│   └── Models/TestReceipt.cs              Test invoice data
└── GRA.PrintBridge.Poc.Tests/             Wrapper and command-byte unit tests
```

## Setup on the Windows printer computer

1. Confirm that the printer is powered on, connected by USB, and appears in **Settings > Bluetooth & devices > Printers & scanners** as `POS-58`.
2. Install the .NET 8 SDK if it is not already installed.
3. Open PowerShell in this project folder and restore/build:

   ```powershell
   dotnet restore
   dotnet build -c Release
   dotnet test -c Release
   ```

4. If Windows uses a different installed name, set it only for the current PowerShell session:

   ```powershell
   $env:GRA_PRINTER_NAME = "Exact Windows printer name"
   ```

5. The ZKP5803 default profile is 384 dots wide and 32 native Font A characters per line. If physical output shows a character edge clipping, calibrate the profile without editing source:

   ```powershell
   $env:GRA_CHARACTERS_PER_LINE = "30"
   ```

## Run tests

Use `--` to separate `dotnet run` options from application arguments. Recent SDKs may also accept `dotnet run receipt`, but the forms below work consistently.

```powershell
dotnet run --project .\GRA.PrintBridge.Poc -- text
dotnet run --project .\GRA.PrintBridge.Poc -- receipt
dotnet run --project .\GRA.PrintBridge.Poc -- qr
dotnet run --project .\GRA.PrintBridge.Poc -- raster
dotnet run --project .\GRA.PrintBridge.Poc -- all
```

The commands map to the requested short forms when the current directory is `GRA.PrintBridge.Poc`:

```powershell
dotnet run -- text
dotnet run -- receipt
dotnet run -- qr
dotnet run -- raster
dotnet run -- all
```

For the five-consecutive-receipt physical reliability test:

```powershell
dotnet run --project .\GRA.PrintBridge.Poc -- receipt --count 5
```

Expected console flow:

```text
Printer found: POS-58
Opening printer... (1/1)
Sending 1452 bytes...
Print job accepted. Windows job ID: 12.
```

## Print behavior and calibration

- Every print job resets the printer, then enables native ESC/POS emphasized/bold (`ESC E 1`) and double-strike (`ESC G 1`) before receipt text is sent. `receipt` therefore prints all native receipt text using both darkness modes by default, while retaining the existing double-width heading only.
- Native QR support is implemented in `EscPosBuilder.PrintNativeQr`. The runnable `qr` and `receipt` tests intentionally use the dependable 1-bit raster QR path because clone firmware support for native `GS ( k` QR commands is not yet confirmed for this specific POS-58 installation.
- Raster images are created locally, capped at 384 pixels, aggressively thresholded at luminance 224 into pure 1-bit black/white, and sent with `GS v 0` raster commands. Neither Windows nor Chrome transforms them.
- The profile has `SupportsCut = false`; no cutter command is sent. All jobs feed four lines after content.
- ZKTeco confirms this model has adjustable print density, but its available official material does not document a safe ESC/POS heating/density command or maximum value. No guessed hardware density byte is sent.
- The values in the requested receipt sample are kept as supplied. Its two shown line-item amounts do not arithmetically equal the supplied subtotal, so this POC deliberately does not calculate or validate invoice totals.

## Physical checks required before Phase 2

Phase 1 is not physically complete until these checks are performed on the actual `POS-58` printer:

1. `text`: check density, bold, centre/left/right alignment, double width, separator, currency, and long-line wrapping.
2. `receipt`: verify every receipt line is inside the printable width, legible, dark, and has no blank paper area or clipped content.
3. `qr`: scan the printed code and confirm it resolves to `https://example.com/gra-print-test`.
4. `raster`: confirm the three supplied raster text lines are sharp and complete.
5. Run `receipt --count 5` and verify all five receipts succeed without skipped lines, spooler failures, or different spacing.

Do not begin PDF parsing, web UI, GRA integration, authentication, or database work until those physical checks pass. If native right alignment or native QR is not supported by the installed driver/firmware, record that observed limitation and keep the raster QR fallback.
