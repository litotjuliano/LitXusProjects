using FluentAssertions;
using LitXus.Application.Common.Exceptions;
using LitXus.Application.Modules.Accounting.Services;

namespace LitXus.UnitTests.Application;

public class BankStatementCsvParserTests
{
    [Fact]
    public void Parse_WithValidRows_ReturnsParsedLines()
    {
        var csv = "Date,Description,Amount\n2026-07-01,Rent payment,-1200.00\n2026-07-03,Customer payment,5000.00\n";

        var result = BankStatementCsvParser.Parse(csv);

        result.Should().HaveCount(2);
        result[0].TransactionDate.Should().Be(new DateOnly(2026, 7, 1));
        result[0].Description.Should().Be("Rent payment");
        result[0].Amount.Should().Be(-1200.00m);
        result[1].Amount.Should().Be(5000.00m);
    }

    [Fact]
    public void Parse_WithQuotedDescriptionContainingComma_PreservesWholeField()
    {
        var csv = "Date,Description,Amount\n2026-07-01,\"Payment, ref 12345\",100.00\n";

        var result = BankStatementCsvParser.Parse(csv);

        result.Should().ContainSingle();
        result[0].Description.Should().Be("Payment, ref 12345");
    }

    [Fact]
    public void Parse_WithWrongHeader_ThrowsValidationException()
    {
        var csv = "TransactionDate,Description,Amount\n2026-07-01,Rent,100.00\n";

        var act = () => BankStatementCsvParser.Parse(csv);

        act.Should().Throw<ValidationException>().Where(e => e.Errors.ContainsKey("Header"));
    }

    [Fact]
    public void Parse_WithMalformedRow_RejectsEntireImportAndListsAllBadRows()
    {
        var csv = "Date,Description,Amount\n2026-07-01,Valid row,100.00\nnot-a-date,Bad row,50.00\n";

        var act = () => BankStatementCsvParser.Parse(csv);

        act.Should().Throw<ValidationException>().Where(e => e.Errors.ContainsKey("Row 3"));
    }

    [Fact]
    public void Parse_WithEmptyFile_ThrowsValidationException()
    {
        var act = () => BankStatementCsvParser.Parse("");

        act.Should().Throw<ValidationException>();
    }
}
