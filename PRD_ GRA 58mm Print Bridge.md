# Product Requirements Document

## Product Name

**GRA Print Bridge**

Working name only. Can be changed later.

## 1. Product Summary

GRA Print Bridge is a small printing system that allows staff to print Ghana Revenue Authority E-VAT invoice PDFs clearly on an existing 58mm thermal printer.

The current GRA PDF is wider than the printer's usable print area. When Chrome prints it directly, Chrome shrinks the entire document. This makes the text very small and faint.

The system will not simply shrink the original PDF.

Instead, it will:

1. Receive the GRA PDF.
2. Read and extract the invoice data.
3. Convert that data into a structured receipt.
4. Rebuild the receipt for a 58mm printer.
5. Print normal text using native thermal printer commands.
6. Print graphical elements separately where needed.
7. Send the final receipt directly to the printer.
8. Fall back to image-based printing if the PDF cannot be parsed correctly.

---

# 2. Background

The company currently uses the Ghana Revenue Authority E-VAT system.

Invoices are generated from the GRA website and printed from the browser.

The existing printer is a 58mm thermal printer.

The GRA PDF itself contains structured invoice data, including:

- customer
- customer TIN
- invoice number
- date
- due date
- currency
- items
- prices
- quantities
- tax information
- invoice total
- E-VAT receipt information

The supplied sample contains four line items.

It also contains tax totals and an invoice total of GHS 3,042.00.

The PDF contains E-VAT fields such as SDC ID, internal data, signature, receipt number, line-item count, and transaction date/time.

It also contains customer and invoice information such as customer name, customer TIN, invoice number, currency, and date.

---

# 3. Problem

The current workflow is:

GRA website  
→ Chrome  
→ Windows printer driver  
→ 58mm printer

Chrome tries to fit the original GRA page into the smaller printable area.

This causes:

- very small text
- faint text
- poor readability
- inconsistent printing
- dependence on Chrome's print settings
- repeated paper-size configuration
- poor user experience
- risk of staff printing unreadable tax receipts

The printer itself is capable of dark printing, as confirmed by its Windows test page.

The problem is therefore mainly the transformation of the GRA document before it reaches the printer.

---

# 4. Product Goal

Create a reliable printing bridge that converts a GRA invoice into a receipt designed specifically for the company's existing 58mm thermal printer.

The final staff experience should be:

**Generate invoice → Click Print → Receive clear receipt**

Staff should not need to manage:

- paper sizes
- scaling
- halftoning
- margins
- printer preferences
- PDF settings
- contrast
- print density
- Chrome print settings

---

# 5. Success Criteria

The product is successful when:

- all important GRA invoice information prints
- text is clearly readable
- text prints dark enough
- line items remain understandable
- amounts remain accurate
- tax information remains accurate
- E-VAT receipt information is preserved
- long descriptions wrap correctly
- no content is cut off
- there is no large blank paper section
- receipt width fits the printer
- printing does not depend on Chrome's rendering
- staff can print with one simple action
- failures are clearly shown
- the original GRA data is never modified

---

# 6. Users

## Primary User

Company staff responsible for issuing GRA VAT invoices.

They should not need technical knowledge.

## Secondary User

Administrator or technical support person.

They can:

- configure printer
- run test prints
- adjust printer profile
- view errors
- review logs
- calibrate receipt output

---

# 7. Core Product Flow

```text
GRA PDF
   ↓
PDF Receiver
   ↓
PDF Parser
   ↓
Invoice Validator
   ↓
Normalized Receipt Data
   ↓
58mm Receipt Composer
   ↓
Print Renderer
   ↓
ESC/POS Generator
   ↓
Local Print Agent
   ↓
Windows Printer
   ↓
58mm Thermal Printer
```

If parsing fails:

```text
GRA PDF
   ↓
Raster Renderer
   ↓
Crop
   ↓
Resize
   ↓
Contrast Enhancement
   ↓
Black/White Conversion
   ↓
ESC/POS Raster
   ↓
Printer
```

---

# 8. Architecture

The system should have four main parts.

## 8.1 Web Application

The web application provides the user interface.

Responsibilities:

- receive invoice PDF
- show receipt preview
- allow printing
- show printer status
- show errors
- communicate with local print agent
- provide calibration screen for administrators

The web application should not directly control Windows printing through the normal browser print dialog.

---

## 8.2 PDF Processing Engine

Responsible for understanding the GRA PDF.

It should:

- read PDF text
- identify fields
- identify sections
- extract invoice values
- extract line items
- extract totals
- extract E-VAT data
- identify graphical elements when necessary
- detect unsupported document formats
- return structured data

---

## 8.3 Receipt Composer

Responsible for converting extracted data into a 58mm-friendly layout.

It should not try to copy the original GRA page pixel-for-pixel.

Instead, it should rebuild it for the printer.

---

## 8.4 Local Print Agent

A lightweight application installed on the Windows computer.

Responsibilities:

- receive print jobs from the web application
- communicate with the printer
- send ESC/POS commands
- send bitmap data
- select the correct Windows printer
- report printer errors
- expose printer status
- store local printer configuration

The print agent should run quietly in the background.

---

# 9. Receipt Data Model

The parser should normalize the GRA PDF into a structure similar to:

```json
{
  "seller": {
    "name": "",
    "tin": ""
  },
  "invoice": {
    "type": "VAT INVOICE",
    "number": "",
    "date": "",
    "due_date": "",
    "currency": ""
  },
  "customer": {
    "name": "",
    "tin": "",
    "phone": ""
  },
  "items": [
    {
      "description": "",
      "unit_price": 0,
      "quantity": 0,
      "amount": 0
    }
  ],
  "remarks": "",
  "taxes": {
    "subtotal": 0,
    "discount": 0,
    "nhil": 0,
    "getfund": 0,
    "cst": 0,
    "tourism": 0,
    "vat": 0,
    "total_tax": 0
  },
  "total": 0,
  "evat": {
    "sdc_id": "",
    "receipt_number": "",
    "internal_data": "",
    "signature": "",
    "mrc": "",
    "date_time": "",
    "line_item_count": 0
  }
}
```

---

# 10. PDF Parser Requirements

The parser must:

### PDF validation

Confirm:

- file is a PDF
- PDF can be opened
- PDF contains at least one page
- PDF is not corrupted

### Document recognition

Check whether the PDF appears to be a supported GRA invoice.

Look for markers such as:

- VAT INVOICE
- INVOICE NO
- CUSTOMER
- INVOICE TOTAL
- EVAT RECEIPT INFORMATION
- SDC ID
- RECEIPT NUMBER

### Field extraction

Extract:

- seller/company name
- seller TIN
- customer name
- customer TIN
- phone
- invoice number
- invoice date
- due date
- currency
- group reference
- remarks

### Item extraction

For every item:

- description
- price
- quantity
- amount

The parser must support:

- one item
- multiple items
- long descriptions
- descriptions wrapping across several lines

### Tax extraction

Extract all tax fields available in the PDF.

Do not assume every tax type will always exist.

### E-VAT information

Extract:

- SDC ID
- receipt number
- internal data
- signature
- MRC
- date and time
- line-item count

### Unknown fields

If GRA introduces a field the parser does not understand:

- do not silently delete it if it can be detected
- record it under an additional-fields structure
- log the unknown field for future support

---

# 11. Data Validation

Before printing, validate the parsed data.

At minimum:

- invoice number must exist
- invoice date must exist
- seller must exist
- invoice total must exist
- E-VAT receipt number should exist where provided
- item count should match parsed items where available
- monetary fields must remain unchanged

The system must never calculate new tax values unless specifically required.

GRA remains the source of truth.

The system's job is formatting and printing, not tax calculation.

---

# 12. Receipt Layout

The receipt must be designed specifically for the printer width.

Suggested structure:

```text
COMPANY NAME
TIN: XXXXX

VAT INVOICE

Invoice #: XXXXX
Date: XXXXX
Customer: XXXXX
TIN: XXXXX

------------------------

ITEM 1
Description wraps here
Qty: 1
Price: 658.00
Amount: 658.00

------------------------

Subtotal       2,535.00
NHIL              63.38
GETFund           63.38
VAT              380.25
Tax Total        507.00

TOTAL          3,042.00

------------------------

EVAT RECEIPT INFORMATION

SDC ID:
XXXXX

Receipt Number:
XXXXX

Internal Data:
XXXXX

Signature:
XXXXX

MRC:
XXXXX

Date & Time:
XXXXX

------------------------
```

Exact layout can change after physical testing.

---

# 13. Text Printing

Where possible, normal receipt text should be printed using native ESC/POS text commands.

Benefits:

- sharper text
- darker output
- smaller print data
- faster printing
- less dependence on image rendering

Supported formatting should include:

- normal text
- bold
- centred
- left aligned
- right aligned
- double width where appropriate
- line breaks
- separators

---

# 14. Long Text Handling

Long strings must wrap automatically.

This includes:

- customer names
- item descriptions
- internal data
- signatures
- receipt numbers
- remarks

No important field may be truncated without clearly indicating it.

The receipt composer should calculate line length based on the active printer profile.

---

# 15. Money Formatting

Amounts should:

- maintain the exact value from GRA
- use two decimal places where appropriate
- use thousand separators
- align consistently
- never be recalculated incorrectly

Example:

```text
Subtotal        2,535.00
Tax               507.00
TOTAL           3,042.00
```

---

# 16. QR Code and Images

If the GRA invoice contains a QR code or another required graphic:

- detect it
- extract it from the PDF where possible
- avoid unnecessary scaling
- convert it to black and white
- preserve its square structure
- print it separately from normal text

QR processing should not use the same aggressive enhancement applied to normal text.

The system should verify the QR remains readable before printing where technically possible.

---

# 17. Raster Fallback Mode

Structured parsing is the preferred mode.

If structured parsing fails, the system should automatically or manually offer raster fallback.

Raster process:

```text
PDF
↓
High-resolution render
↓
Crop page
↓
Remove unnecessary margins
↓
Resize to printer width
↓
Grayscale
↓
Increase contrast
↓
Apply gamma correction
↓
Threshold
↓
Optional text thickening
↓
1-bit bitmap
↓
ESC/POS raster print
```

The raster mode must not use Chrome's normal printing engine.

---

# 18. Image Processing Controls

Administrator calibration may expose:

- contrast
- brightness
- gamma
- threshold
- sharpening
- text weight
- dithering mode

Example:

```text
Contrast       180%
Brightness     95%
Threshold      165
Text Weight    1
Sharpen        1
```

These settings should not be visible to normal staff.

---

# 19. Printer Profiles

The system should support configurable printer profiles.

Example:

```json
{
  "name": "ZKP5803",
  "paper_width_mm": 58,
  "printable_width_mm": 48,
  "dpi": 203,
  "print_width_dots": 384,
  "encoding": "ESC/POS",
  "feed_after_print_mm": 5
}
```

A default profile should be created for the current printer.

Future printers can have separate profiles.

---

# 20. Local Print Agent API

The local print service should expose a small local API.

Example endpoints:

```text
GET /health
GET /printers
GET /printer/status
POST /print
POST /test-print
POST /calibrate
```

Example:

```text
http://127.0.0.1:PORT
```

The service should bind only to localhost by default.

---

# 21. Print Request

Example print request:

```json
{
  "printer": "POS-58",
  "profile": "ZKP5803",
  "mode": "structured",
  "receipt": {}
}
```

The agent should return:

```json
{
  "success": true,
  "job_id": "xxx",
  "message": "Receipt sent to printer"
}
```

---

# 22. Printer Communication

Preferred output:

**RAW ESC/POS**

Avoid:

- browser print rendering
- Windows page scaling
- Chrome PDF rasterization
- normal A4-style print jobs

The application should control:

- text formatting
- bitmap width
- paper feed
- alignment
- receipt ending

---

# 23. Printer Detection

The local agent should:

- list installed printers
- remember selected printer
- detect if selected printer is missing
- detect if printer is offline where Windows exposes this
- allow administrator to change printer

Normal staff should not choose the printer every time.

---

# 24. Test Print

Administrator should have:

**Print Test Receipt**

The test receipt should include:

- normal text
- bold text
- small text
- large text
- numbers
- separators
- a sample QR code
- black blocks
- grey/raster test where needed

This helps diagnose printer quality.

---

# 25. Calibration

Provide an administrator-only calibration page.

Controls:

- printer
- receipt width
- contrast
- threshold
- text weight
- raster mode
- feed amount

Actions:

- preview
- test print
- save profile
- reset defaults

---

# 26. Web Application

The web app should initially provide:

## Print screen

- upload/select GRA PDF
- receipt preview
- printer status
- Print Receipt button
- error state

## Admin screen

- printer configuration
- calibration
- test print
- logs
- system status

No complex dashboard is needed in V1.

---

# 27. GRA Integration

The first release should prove printing using an uploaded GRA PDF.

After printing works correctly, integrate with the GRA workflow.

Potential integration methods:

### Option A: Chrome Extension

Preferred for automatic capture.

The extension can:

- detect the GRA invoice page
- detect receipt generation
- obtain or capture the PDF
- send it to the local/web processing system
- add a Print Receipt button

### Option B: Download Workflow

Staff:

1. Generate receipt.
2. Download PDF.
3. Open Print Bridge.
4. Select PDF.
5. Print.

Useful as an early production fallback.

### Option C: File Watcher

Local agent watches a download directory.

When a new GRA PDF arrives:

- detect file
- process it
- make it available for printing

This may be added later.

---

# 28. Browser Extension

If built, the extension should:

- run only on approved GRA domains
- avoid reading unrelated websites
- detect supported invoice pages
- capture only required invoice information
- communicate securely with the local print agent
- show clear success or failure

Possible button:

**Print with GRA Print Bridge**

---

# 29. Preview

The preview should show the actual 58mm layout, not the original GRA page.

Users should see approximately what will physically print.

Preview should display:

- text wrapping
- items
- totals
- E-VAT information
- QR where available

---

# 30. Error Handling

The system must handle:

### Invalid PDF

Message:

> This file could not be read as a valid PDF.

### Unsupported PDF

> This does not appear to be a supported GRA invoice.

### Parsing failure

> Some invoice information could not be extracted.

Allow:

- retry
- raster fallback
- cancel

### Printer offline

> The receipt is ready, but the printer is currently unavailable.

### Print agent unavailable

> Print service is not running.

### Printer not configured

> No receipt printer has been configured.

### Print failure

Do not mark receipt as successfully printed until the job has at least been accepted by the local print system.

---

# 31. Logging

Store local logs for:

- timestamp
- invoice number
- printer
- print mode
- parsing result
- print job result
- error code

Do not store unnecessary customer data in logs.

Example:

```text
2026-08-16 09:23
Invoice: NS260805-001-000004
Mode: Structured
Printer: POS-58
Status: Sent
```

---

# 32. Reprinting

The system should allow reprinting.

Reprints must not change invoice values.

Optional future feature:

```text
REPRINT
```

can be shown on the receipt if company policy requires it.

Do not add this automatically without approval.

---

# 33. Security

The system handles potentially sensitive financial information.

Requirements:

- process locally where possible
- avoid uploading receipt data to external services unnecessarily
- use localhost for print-agent communication
- restrict local API access
- validate all input files
- do not execute PDF JavaScript
- do not trust filenames
- limit file sizes
- sanitize extracted values
- protect extension permissions
- do not expose printer APIs publicly

---

# 34. Privacy

V1 should not require sending invoices to a cloud server.

Preferred flow:

```text
Browser
↓
Local processing
↓
Local printer
```

If a cloud web application is later used, sensitive invoice contents should remain local where technically possible.

---

# 35. Reliability

The system should continue printing even if:

- the company's cloud dashboard is unavailable
- internet access to the Print Bridge backend is unavailable

As long as:

- GRA invoice has already been obtained
- local print service is running
- printer is connected

---

# 36. Performance

Targets:

PDF parsing:

**< 2 seconds**

Receipt generation:

**< 1 second**

Print job preparation:

**< 2 seconds**

Staff should normally receive print output within a few seconds of clicking Print.

---

# 37. Installation

V1 Windows installation should include:

- local print agent
- required runtime
- printer profile
- local configuration
- optional Chrome extension

Installation should be simple.

Target:

```text
Install
→ Select printer
→ Test print
→ Done
```

---

# 38. Startup Behaviour

The print agent should:

- start automatically with Windows
- run in background
- use minimal memory
- expose a tray icon if useful
- reconnect automatically after restart

---

# 39. Updates

The application should eventually support safe updates.

V1 can use manual updates.

Future versions may support:

- automatic update checks
- signed installers
- version reporting

---

# 40. Offline Behaviour

After the invoice PDF is available, printing should not require an internet connection.

This reduces dependency on external infrastructure.

---

# 41. Supported Operating System

Initial target:

**Windows**

Do not build macOS or Linux support in V1 unless needed.

---

# 42. Supported Printer

Initial profile:

**ZKTeco ZKP5803 / compatible POS-58 ESC/POS printer**

Expected configuration:

- 58mm paper
- approximately 48mm printable width
- approximately 384 horizontal dots
- approximately 203 DPI
- USB
- ESC/POS compatible

Actual printer behaviour must be verified during development.

---

# 43. Functional Requirements

### FR-001
System must accept a GRA PDF.

### FR-002
System must validate that the PDF can be opened.

### FR-003
System must identify supported GRA invoice documents.

### FR-004
System must extract seller information.

### FR-005
System must extract customer information.

### FR-006
System must extract invoice information.

### FR-007
System must extract all line items.

### FR-008
System must extract tax information.

### FR-009
System must extract total amounts.

### FR-010
System must extract E-VAT receipt information.

### FR-011
System must preserve values exactly as supplied by GRA.

### FR-012
System must create a 58mm receipt layout.

### FR-013
System must wrap long text.

### FR-014
System must print through ESC/POS where supported.

### FR-015
System must support raster graphics.

### FR-016
System must support raster fallback.

### FR-017
System must provide receipt preview.

### FR-018
System must report printer availability.

### FR-019
System must provide a test-print function.

### FR-020
System must provide configurable printer profiles.

### FR-021
System must provide calibration.

### FR-022
System must remember configuration.

### FR-023
System must allow receipt reprinting.

### FR-024
System must report parsing failures.

### FR-025
System must report printing failures.

### FR-026
System must store basic local print logs.

### FR-027
System must not require Chrome's normal print dialog.

### FR-028
System must support future GRA browser integration.

---

# 44. Non-Functional Requirements

### NFR-001 Reliability

The same PDF should produce the same receipt layout every time.

### NFR-002 Accuracy

No monetary value may be changed during formatting.

### NFR-003 Readability

Printed receipt text must be clearly readable.

### NFR-004 Security

Local printer API must not be exposed publicly.

### NFR-005 Performance

Processing should normally take less than five seconds.

### NFR-006 Maintainability

GRA parsing logic should be separate from printer logic.

### NFR-007 Extensibility

Other printer profiles should be addable later.

### NFR-008 Observability

Failures should produce useful logs.

---

# 45. Proposed Technical Stack

## Web UI

- React or Next.js
- TypeScript

## PDF Processing

Possible tools:

- PDF.js
- pdf-lib
- dedicated PDF text parser

Selection should be based on actual parsing accuracy against GRA PDFs.

## Local Print Agent

Recommended:

- .NET / C#

Reasons:

- strong Windows integration
- easy printer access
- easy Windows service or tray app
- direct spooler access
- easy installer support

## Printing

- ESC/POS
- Windows RAW print spooler access

## Configuration

V1:

- JSON file

or:

- SQLite

SQLite becomes useful if logs and multiple profiles grow.

---

# 46. Internal Module Structure

```text
gra-print-bridge/
│
├── web/
│   ├── upload
│   ├── preview
│   ├── print
│   └── admin
│
├── parser/
│   ├── pdf-reader
│   ├── gra-detector
│   ├── invoice-parser
│   ├── item-parser
│   └── validator
│
├── receipt-engine/
│   ├── normalizer
│   ├── layout
│   ├── text-wrapper
│   ├── qr
│   └── raster-fallback
│
├── print-agent/
│   ├── api
│   ├── printer-discovery
│   ├── escpos
│   ├── spooler
│   ├── profiles
│   └── status
│
└── tests/
```

---

# 47. Development Order

The system should be built backwards from the printer.

## Phase 1: Raw Printer Proof

Goal:

Prove we can control the printer directly.

Build:

```text
Test Data
→ ESC/POS
→ Printer
```

Test:

- text
- bold
- alignment
- numbers
- long text
- QR
- paper feed

Exit condition:

Receipt text prints dark and clearly.

---

## Phase 2: Receipt Composer

Input:

Hard-coded sample invoice data.

Output:

Full 58mm receipt.

Exit condition:

Physical receipt layout is approved.

---

## Phase 3: GRA PDF Parser

Input:

The provided real GRA PDF.

Parse:

- invoice
- customer
- items
- taxes
- E-VAT information

Exit condition:

Parsed data exactly matches the PDF.

---

## Phase 4: Complete Pipeline

```text
GRA PDF
→ Parser
→ Receipt Data
→ Receipt Composer
→ ESC/POS
→ Printer
```

Exit condition:

The supplied real PDF prints correctly.

---

## Phase 5: Raster Fallback

Build image-based fallback for unsupported documents.

Exit condition:

A document that cannot be parsed can still be printed.

---

## Phase 6: Web Interface

Create:

- PDF upload
- preview
- print
- status
- calibration

---

## Phase 7: GRA Integration

Build browser extension or integration layer.

Goal:

Staff should not manually upload files.

Final flow:

```text
GRA
→ Print Receipt
→ Print Bridge
→ Printer
```

---

# 48. Testing

Testing must use real printed paper, not only screen previews.

## Parser Tests

Test:

- single item
- many items
- long descriptions
- missing phone
- missing optional tax
- different customer names
- different totals
- large amounts
- long E-VAT strings

## Printer Tests

Test:

- normal text
- bold
- small text
- long text
- long receipt
- QR
- numbers
- separator lines
- repeated prints
- printer reconnect

## Failure Tests

Test:

- unplug printer
- turn printer off
- corrupt PDF
- unsupported PDF
- kill print agent
- restart Windows
- close browser
- print twice

---

# 49. Acceptance Criteria

V1 is ready when:

- sample GRA PDF is parsed successfully
- all invoice values match the original PDF
- all four sample line items print correctly
- receipt fits 58mm paper
- no right-side content is cut
- no large empty area appears
- text is clearly darker than current Chrome output
- invoice total is readable
- E-VAT data is readable
- QR is readable where present
- print works without Chrome print preview
- print can be triggered from the application
- printer errors are shown clearly
- restart does not destroy printer configuration
- five consecutive prints succeed correctly

---

# 50. Out of Scope for V1

Do not build:

- invoice creation
- tax calculation
- GRA replacement
- accounting software
- CRM
- customer management
- payment processing
- inventory
- cloud analytics
- mobile apps
- multi-company management
- complex user permissions
- remote printer control

The GRA system remains responsible for creating the official invoice.

---

# 51. Future Features

Possible future additions:

- automatic GRA browser integration
- multiple printers
- multiple branches
- central printer profiles
- remote diagnostics
- automatic software updates
- printer health monitoring
- print history
- role-based access
- receipt templates
- support for 80mm printers
- support for other GRA document types
- cloud management dashboard

---

# 52. Main Technical Risk

The biggest risk is not the web application.

It is whether the ZKP5803 produces sufficiently dark, readable output when we send our own ESC/POS text and raster data directly.

Therefore, the first development task is not the UI.

The first task is:

> Print a complete readable 58mm receipt directly through ESC/POS using the existing printer.

If that works, the core product risk is solved.

---

# 53. First Proof of Concept

Use the real uploaded GRA PDF.

Build this exact pipeline:

```text
NEW RECEIPT.pdf
       ↓
Extract invoice data
       ↓
Convert to receipt model
       ↓
Compose 58mm receipt
       ↓
ESC/POS
       ↓
POS-58 printer
```

No authentication.

No deployment.

No database.

No Chrome extension.

No cloud backend.

The proof of concept succeeds only when the physical receipt is dark, complete, readable, and accurate.

---

# 54. Final V1 User Experience

The final staff experience should eventually be:

1. Staff generates GRA invoice.
2. Staff clicks **Print Receipt**.
3. GRA Print Bridge receives the invoice.
4. Receipt is processed automatically.
5. POS-58 prints the receipt.
6. Staff receives a readable invoice.

Nothing else should be required from the staff member.