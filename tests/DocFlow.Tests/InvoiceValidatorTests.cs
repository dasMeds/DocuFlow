using DocFlow.Core.Models;
using DocFlow.Core.Validation;
using Xunit;

namespace DocFlow.Tests;

/// <summary>
/// Unit tests verifying financial validation rules independently of UI or worker infrastructure.
/// </summary>
public class InvoiceValidatorTests
{
    [Fact]
    public void Validate_ReturnsTrue_WhenTotalsMatch()
    {
        var invoice = new ExtractedInvoice
        {
            InvoiceNumber = "INV-1001",
            VendorName = "Acme Corp",
            Subtotal = 100.00m,
            TaxAmount = 12.00m,
            TotalAmount = 112.00m
        };

        var (isValid, issue) = InvoiceValidator.Validate(invoice);
        Assert.True(isValid);
        Assert.Null(issue);
    }

    [Fact]
    public void Validate_ReturnsFalse_WhenTotalMismatch()
    {
        var invoice = new ExtractedInvoice
        {
            InvoiceNumber = "INV-1002",
            VendorName = "Acme Corp",
            Subtotal = 100.00m,
            TaxAmount = 12.00m,
            TotalAmount = 120.00m
        };

        var (isValid, issue) = InvoiceValidator.Validate(invoice);
        Assert.False(isValid);
        Assert.Contains("Total mismatch", issue);
    }
}
