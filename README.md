# GRA Print Bridge, Phase 1 POC

This Windows-only console application prints the actual GRA invoice PDF through the existing `POS-58` / ZKTeco ZKP5803 printer. The receipt path is:

```text
GRA PDF -> PDFium render -> crop -> proportional resize -> grayscale luminance -> 1-bit raster -> ESC/POS -> RAW Windows spooler -> POS-58
```

It does not use Chrome, Adobe, a browser, `window.print()`, PDF page-layout printing, or the Windows print dialog.

## Receipt behavior

`receipt` does not rebuild the invoice with native receipt text. It renders the original page, preserving the GRA logo, headings, TIN, information rows, grey table header, item table, totals, E-VAT section, and spacing as one visual document.

- PDF renderer: `Docnet.Core` 2.6.0, a PDFium renderer.
- Source rendering resolution: 600 DPI.
- Crop: scans the rendered page for luminance of 245 or darker, then retains a 24-pixel safety border around the visible content.
- Resize: scales the cropped image proportionally to exactly 384 dots wide. It never independently scales width and height.
- Raster: calculates grayscale luminance and thresholds at 224, turning dark and medium-dark pixels into black. The print stream contains only 1-bit black or white pixels.
- Printer setup: every job initializes the printer then enables ESC/POS bold/emphasized (`ESC E 1`) and double-strike (`ESC G 1`). No undocumented heating/density byte is sent.

## Setup on the Windows printer computer

1. Install the .NET 8 SDK.
2. Confirm that the printer is installed as `POS-58`.
3. Put the supplied `NEW RECEIPT.pdf` in your Windows Downloads folder.
4. Restore and build:

   ```powershell
   dotnet restore
   dotnet build -c Release
   dotnet test -c Release
   ```

If the PDF is stored elsewhere, either set an environment variable or pass the path after `receipt`:

```powershell
$env:GRA_RECEIPT_PDF = "C:\Invoices\NEW RECEIPT.pdf"
dotnet run --project .\GRA.PrintBridge.Poc -- receipt "C:\Invoices\NEW RECEIPT.pdf"
```

If Windows has a different printer name:

```powershell
$env:GRA_PRINTER_NAME = "Exact Windows printer name"
```

## Commands

Print the supplied PDF from the Windows Downloads folder:

```powershell
dotnet run --project .\GRA.PrintBridge.Poc -- receipt
```

Run diagnostic commands:

```powershell
dotnet run --project .\GRA.PrintBridge.Poc -- text
dotnet run --project .\GRA.PrintBridge.Poc -- qr
dotnet run --project .\GRA.PrintBridge.Poc -- raster
dotnet run --project .\GRA.PrintBridge.Poc -- all
```

For five consecutive PDF receipts:

```powershell
dotnet run --project .\GRA.PrintBridge.Poc -- receipt --count 5
```

## Project structure

```text
GRA.PrintBridge.Poc/
├── Program.cs                         Commands and PDF discovery
├── Printing/
│   ├── RawPrinterService.cs           Windows RAW spooler transport
│   ├── EscPosBuilder.cs               ESC/POS setup and raster commands
│   └── PrinterProfile.cs              384-dot ZKP5803 profile
├── Formatting/
│   ├── ReceiptFormatter.cs            Native text diagnostic only
│   └── TextWrapper.cs                 Diagnostic text wrapping
└── Imaging/
    ├── PdfReceiptRenderer.cs          PDFium render, crop, resize, 1-bit result
    ├── BitmapCropper.cs               Outer whitespace crop detection
    ├── RasterConverter.cs             Grayscale luminance to 1-bit ESC/POS data
    ├── QrGenerator.cs                 QR diagnostic raster
    └── RasterImage.cs                 ESC/POS raster representation
```

## Physical validation before Phase 2

Compare the original PDF directly with the thermal output and verify the complete original layout is recognisable, including the logo, table structure, all item and tax values, E-VAT details, and totals. Confirm small text remains readable, no content is clipped, the output is dark, and the QR diagnostic scans. Do not begin Phase 2 until five consecutive `receipt` jobs print correctly.
