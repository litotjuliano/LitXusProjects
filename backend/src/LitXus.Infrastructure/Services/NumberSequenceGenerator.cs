using LitXus.Application.Common.Interfaces;
using LitXus.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace LitXus.Infrastructure.Services;

/// <summary>
/// Uses a SQL Server SEQUENCE object (created in the migration) rather than MAX(number)+1,
/// which is unsafe under concurrent posting — see docs/phase-1-accounting/Business_Rules.md.
/// Executed via raw ADO.NET rather than Database.SqlQuery&lt;T&gt;, since EF Core wraps SqlQuery
/// in a derived table and SQL Server rejects "NEXT VALUE FOR" inside subqueries/derived tables.
/// </summary>
public class NumberSequenceGenerator(AppDbContext db) : INumberSequenceGenerator
{
    public async Task<string> NextGLEntryNumberAsync(CancellationToken cancellationToken = default)
    {
        var connection = (SqlConnection)db.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR dbo.GLEntryNumberSeq";

            var next = (long)(await command.ExecuteScalarAsync(cancellationToken))!;
            return $"JE-{DateTime.UtcNow.Year}-{next:D6}";
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
            }
        }
    }
}
