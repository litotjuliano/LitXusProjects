using System.Globalization;
using System.Text;
using FluentValidation.Results;
using LitXus.Application.Common.Exceptions;

namespace LitXus.Application.Modules.Accounting.Services;

/// <summary>
/// Hand-rolled parser for the bank statement CSV import format (no CSV library — 3 fixed columns,
/// same reasoning as the report CSV export). Header row required: Date,Description,Amount — Date
/// as yyyy-MM-dd, Amount signed decimal (+deposit/-withdrawal, matching BankStatementLines.Amount's
/// schema comment). Validation is all-or-nothing: every row is checked before any is accepted, so a
/// malformed row never causes a partial import.
/// </summary>
public static class BankStatementCsvParser
{
    private static readonly string[] ExpectedHeader = ["date", "description", "amount"];

    public record ParsedLine(DateOnly TransactionDate, string Description, decimal Amount);

    public static IReadOnlyList<ParsedLine> Parse(string csvContent)
    {
        var lines = csvContent.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            throw new ValidationException([new ValidationFailure("File", "The CSV file is empty.")]);
        }

        var header = SplitCsvLine(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToArray();
        if (!header.SequenceEqual(ExpectedHeader))
        {
            throw new ValidationException([new ValidationFailure("Header", "The CSV header must be exactly: Date,Description,Amount")]);
        }

        var results = new List<ParsedLine>();
        var errors = new List<ValidationFailure>();

        for (var i = 1; i < lines.Length; i++)
        {
            var rowNumber = i + 1; // 1-based, matches what a user sees opening the file in a spreadsheet
            var fields = SplitCsvLine(lines[i]);
            if (fields.Count != 3)
            {
                errors.Add(new ValidationFailure($"Row {rowNumber}", "Expected exactly 3 columns (Date, Description, Amount)."));
                continue;
            }

            var dateStr = fields[0].Trim();
            var description = fields[1].Trim();
            var amountStr = fields[2].Trim();

            if (!DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                errors.Add(new ValidationFailure($"Row {rowNumber}", $"Invalid date '{dateStr}' — expected yyyy-MM-dd."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                errors.Add(new ValidationFailure($"Row {rowNumber}", "Description cannot be empty."));
                continue;
            }

            if (!decimal.TryParse(amountStr, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount))
            {
                errors.Add(new ValidationFailure($"Row {rowNumber}", $"Invalid amount '{amountStr}'."));
                continue;
            }

            results.Add(new ParsedLine(date, description, amount));
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return results;
    }

    /// <summary>Splits one CSV line on commas, honoring RFC4180-style double-quoted fields
    /// (so a Description containing a comma, e.g. "Payment, ref 12345", survives intact).</summary>
    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }
}
