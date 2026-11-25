using DocFlow.Infrastructure.Parsing;
using Xunit;

namespace DocFlow.Tests;

/// <summary>
/// Confirms invoice fields are parsed consistently from processor text output.
/// </summary>
public sealed class InvoiceFieldExtractorTests
{
    [Fact]
    public void Extract_ParsesLabeledFieldsAndDecimalAmounts()
    {
        const string text = """
            Invoice Number: INV-2040
            Vendor: Northwind Services
            Issue Date: 2026-03-16
            Subtotal: $1,200.50
            Tax: $96.04
            Total: $1,296.54
            """;

        var invoice = new InvoiceFieldExtractor().Extract(text);

        Assert.Equal("INV-2040", invoice.InvoiceNumber);
        Assert.Equal("Northwind Services", invoice.VendorName);
        Assert.Equal(new DateOnly(2026, 3, 16), invoice.IssueDate);
        Assert.Equal(1200.50m, invoice.Subtotal);
        Assert.Equal(96.04m, invoice.TaxAmount);
        Assert.Equal(1296.54m, invoice.TotalAmount);
    }

    [Fact]
    public void Extract_DoesNotInventMissingInvoiceData()
    {
        var invoice = new InvoiceFieldExtractor().Extract("unstructured text");

        Assert.Empty(invoice.InvoiceNumber);
        Assert.Empty(invoice.VendorName);
        Assert.Null(invoice.IssueDate);
        Assert.Equal(0m, invoice.TotalAmount);
    }
}
