using FluentAssertions;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Exceptions;

namespace LitXus.UnitTests.Domain;

public class BankStatementLineTests
{
    [Fact]
    public void Match_WhenNotYetReconciled_SetsReconciledAndMatchedLine()
    {
        var line = BankStatementLine.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), "Deposit", 100m);
        var glEntryLineId = Guid.NewGuid();

        line.Match(glEntryLineId);

        line.IsReconciled.Should().BeTrue();
        line.MatchedGLEntryLineId.Should().Be(glEntryLineId);
    }

    [Fact]
    public void Match_WhenAlreadyReconciled_ThrowsStatementLineAlreadyMatchedException()
    {
        var line = BankStatementLine.Create(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), "Deposit", 100m);
        line.Match(Guid.NewGuid());

        var act = () => line.Match(Guid.NewGuid());

        act.Should().Throw<StatementLineAlreadyMatchedException>();
    }
}
