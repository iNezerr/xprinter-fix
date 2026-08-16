namespace GRA.PrintBridge.Poc.Models;

public sealed record TestReceipt(
    string InvoiceNumber,
    string Date,
    string CustomerName,
    string CustomerType,
    IReadOnlyList<ReceiptItem> Items,
    IReadOnlyList<ReceiptTotal> Totals,
    string SdcId,
    string ReceiptNumber)
{
    public static TestReceipt Create() => new(
        InvoiceNumber: "NS260805-001-000004",
        Date: "5 Aug 2026",
        CustomerName: "KOD HAULAGE",
        CustomerType: "(cash customer)",
        Items:
        [
            new ReceiptItem("CAL ABV 50000 TO 54000 LTS", "1", "658.00", "658.00"),
            new ReceiptItem("TANK CLEANING LES ABOVE 30000 LTS", "1", "800.00", "800.00"),
        ],
        Totals:
        [
            new ReceiptTotal("Subtotal", "2,535.00"),
            new ReceiptTotal("NHIL", "63.38"),
            new ReceiptTotal("GETFund", "63.38"),
            new ReceiptTotal("VAT", "380.25"),
            new ReceiptTotal("Tax Total", "507.00"),
            new ReceiptTotal("TOTAL", "3,042.00"),
        ],
        SdcId: "E002767001",
        ReceiptNumber: "2767001-630A-NS5");
}

public sealed record ReceiptItem(string Description, string Quantity, string Price, string Amount);

public sealed record ReceiptTotal(string Label, string Amount);
