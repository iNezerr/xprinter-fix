# GRA Print Bridge: Phase 1 Implementation Report

**Status:** PDF-to-raster receipt implementation complete locally. Windows build and physical printer verification remain required.

## Change made

The Phase 1 `receipt` command no longer formats a replacement native ESC/POS invoice. `NEW RECEIPT.pdf` is now the visual source of truth. The command renders the original PDF page and sends the resulting one-bit raster image through the existing RAW Windows spooler service.

## Implemented PDF print pipeline

```text
NEW RECEIPT.pdf
-> Docnet.Core / PDFium render at 600 DPI
-> automatic outer-white-margin crop
-> proportional resize to 384 dots wide
-> grayscale luminance calculation
-> aggressive threshold at 224
-> strict 1-bit black-and-white raster
-> ESC/POS GS v 0 command
-> RAW Windows spooler
-> POS-58 / ZKP5803
```

### PDF rendering

- Library: `Docnet.Core` 2.6.0, using PDFium.
- Runtime: the project configures the library for `win-x64`.
- Source render DPI: 600.
- Input: first page of the selected GRA PDF. The supplied `NEW RECEIPT.pdf` is one page.
- Transparency: the renderer composites transparent regions onto white before crop detection.

### Crop and resize

- Crop method: scans the 600 DPI rendered bitmap and detects pixels with luminance `<= 245`; it then retains a 24-pixel safety border. This removes excessive outer white margin without clipping the PDF content.
- Final raster width: 384 pixels/dots, matching the active ZKP5803 profile.
- Aspect ratio: preserved. The output height is calculated from the crop height and width ratio. Width and height are never independently stretched.
- Resize quality: high-quality bicubic downscaling from the high-DPI source preserves small text as well as the 384-dot printer allows.

### One-bit raster and darkness

- Grayscale: each output pixel is converted to luminance before thresholding.
- Threshold: `224`, intentionally aggressive so grey table headers, anti-aliased text edges, and medium-dark content become black.
- Output: strict one-bit ESC/POS raster data. No grayscale pixels are sent to the printer.
- Printer modes: every job sends `ESC @`, `ESC E 1`, and `ESC G 1`, enabling initialization, bold/emphasized text mode, and double-strike mode before print data.
- Hardware density/heating command: not sent. Manufacturer product material confirms adjustable density but does not provide documented command bytes or a safe maximum value for this model.

## Command behavior

The default source lookup for `receipt` is:

1. An explicitly supplied PDF path: `receipt "C:\path\NEW RECEIPT.pdf"`
2. `GRA_RECEIPT_PDF` environment variable
3. `%USERPROFILE%\Downloads\NEW RECEIPT.pdf`
4. `NEW RECEIPT.pdf` in the current directory
5. `Assets\NEW RECEIPT.pdf` beside the built application

Exact command when the supplied PDF is in the Windows Downloads folder:

```powershell
dotnet run --project .\GRA.PrintBridge.Poc -- receipt
```

## Scope retained

- RAW Windows spooler transport remains in `RawPrinterService`.
- `text`, `qr`, and `raster` remain native/raster diagnostics only.
- No web UI, Chrome/browser printing, Adobe printing, database, authentication, PDF parser, or Phase 2 integration has been added.

## Files changed for the PDF receipt path

- `GRA.PrintBridge.Poc/GRA.PrintBridge.Poc.csproj`: added `Docnet.Core` and Windows x64 PDFium runtime selection.
- `GRA.PrintBridge.Poc/Imaging/PdfReceiptRenderer.cs`: new PDF render, crop, proportional resize, and one-bit raster service.
- `GRA.PrintBridge.Poc/Imaging/BitmapCropper.cs`: new content-boundary detection.
- `GRA.PrintBridge.Poc/Program.cs`: `receipt` now resolves and prints the PDF source.
- `GRA.PrintBridge.Poc/Formatting/ReceiptFormatter.cs`: reduced to the retained diagnostic `text` output.
- `GRA.PrintBridge.Poc/Models/TestReceipt.cs`: removed because custom native receipt composition is no longer the receipt path.

## Verification status

Completed in this workspace:

- Inspected and visually rendered the supplied one-page PDF at 300 DPI.
- Confirmed its GRA logo, company/TIN heading, grey item table header, four line items, tax totals, invoice total, and E-VAT details are all present in the visual source.
- Reviewed the PDFium library API and configured its documented pixel-per-point rendering interface.
- Validated project XML and performed source checks after implementation.

Not possible in this macOS workspace:

- .NET restore, build, or unit-test execution because no .NET SDK is installed.
- Windows PDFium native-runtime loading.
- RAW spooler acceptance and printed output on `POS-58`.
- Physical readability of the final 384-dot raster and five consecutive receipt jobs.

## Required physical check

On the actual Windows printer computer, run the exact receipt command above, compare its output directly to `NEW RECEIPT.pdf`, then run:

```powershell
dotnet run --project .\GRA.PrintBridge.Poc -- receipt --count 5
```

Verify that the physical receipt retains the original visual structure, all required values, the table header, logo, totals, and E-VAT section without clipping or blank excess paper.
