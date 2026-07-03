using FluentAssertions;
using LitXus.Application.Common.Exceptions;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Commands.CreateGLEntry;
using LitXus.Application.Modules.Accounting.Commands.PostGLEntry;
using LitXus.Application.Modules.Accounting.Commands.UpdateGLEntry;
using LitXus.Application.Modules.Accounting.Commands.VoidGLEntry;
using LitXus.Domain.Modules.Accounting.Entities;
using LitXus.Domain.Modules.Accounting.Enums;
using LitXus.Infrastructure.Persistence;
using Moq;

namespace LitXus.UnitTests.Application;

public class GLEntryHandlerTests
{
    private static Account NewAccount(AccountType type, string code) => Account.Create(code, $"{code} Account", type, null);

    [Fact]
    public async Task CreateGLEntryCommandHandler_WithValidLines_PersistsDraftEntry()
    {
        await using var db = TestDbContextFactory.Create();
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        db.Accounts.AddRange(cash, revenue);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateGLEntryCommandHandler(db);
        var command = new CreateGLEntryCommand(
            DateOnly.FromDateTime(DateTime.UtcNow), "Test entry",
            [
                new CreateGLEntryLineInput(cash.Id, 150m, 0m, null),
                new CreateGLEntryLineInput(revenue.Id, 0m, 150m, null),
            ]);

        var dto = await handler.Handle(command, CancellationToken.None);

        dto.Status.Should().Be(nameof(GLEntryStatus.Draft));
        var persisted = await db.GLEntries.FindAsync([dto.Id], CancellationToken.None);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateGLEntryCommandHandler_WithUnknownAccountId_ThrowsNotFoundException()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = new CreateGLEntryCommandHandler(db);
        var command = new CreateGLEntryCommand(
            DateOnly.FromDateTime(DateTime.UtcNow), "Bad entry",
            [new CreateGLEntryLineInput(Guid.NewGuid(), 100m, 0m, null)]);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task PostGLEntryCommandHandler_WhenBalanced_AssignsEntryNumberAndAuditLogs()
    {
        await using var db = TestDbContextFactory.Create();
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        db.Accounts.AddRange(cash, revenue);
        var entry = GLEntry.CreateDraft(DateOnly.FromDateTime(DateTime.UtcNow), "To post",
            [GLEntryLine.Create(cash, 100m, 0m, null), GLEntryLine.Create(revenue, 0m, 100m, null)]);
        db.GLEntries.Add(entry);
        await db.SaveChangesAsync(CancellationToken.None);

        var numberGenerator = new Mock<INumberSequenceGenerator>();
        numberGenerator.Setup(n => n.NextGLEntryNumberAsync(It.IsAny<CancellationToken>())).ReturnsAsync("JE-2026-000099");
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.SetupGet(c => c.UserId).Returns(Guid.NewGuid());
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.SetupGet(d => d.UtcNow).Returns(DateTime.UtcNow);
        var auditLogger = new Mock<IAuditLogger>();

        var handler = new PostGLEntryCommandHandler(db, numberGenerator.Object, currentUser.Object, dateTimeProvider.Object, auditLogger.Object);
        var dto = await handler.Handle(new PostGLEntryCommand(entry.Id), CancellationToken.None);

        dto.Status.Should().Be(nameof(GLEntryStatus.Posted));
        dto.EntryNumber.Should().Be("JE-2026-000099");
        auditLogger.Verify(a => a.LogAsync(
            nameof(GLEntry), entry.Id.ToString(), "Approve",
            It.IsAny<object>(), It.IsAny<object>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VoidGLEntryCommandHandler_WhenPosted_VoidsAndAuditLogsWithReason()
    {
        await using var db = TestDbContextFactory.Create();
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        db.Accounts.AddRange(cash, revenue);
        var entry = GLEntry.CreateDraft(DateOnly.FromDateTime(DateTime.UtcNow), "To void",
            [GLEntryLine.Create(cash, 100m, 0m, null), GLEntryLine.Create(revenue, 0m, 100m, null)]);
        entry.Post("JE-2026-000100", Guid.NewGuid(), DateTime.UtcNow);
        db.GLEntries.Add(entry);
        await db.SaveChangesAsync(CancellationToken.None);

        var auditLogger = new Mock<IAuditLogger>();
        var handler = new VoidGLEntryCommandHandler(db, auditLogger.Object);

        var dto = await handler.Handle(new VoidGLEntryCommand(entry.Id, "Duplicate entry"), CancellationToken.None);

        dto.Status.Should().Be(nameof(GLEntryStatus.Voided));
        auditLogger.Verify(a => a.LogAsync(
            nameof(GLEntry), entry.Id.ToString(), "Void",
            It.IsAny<object>(), It.IsAny<object>(), "Duplicate entry", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGLEntryCommandHandler_WhenDraft_ReplacesLinesWithoutConcurrencyException()
    {
        // Regression test for the EF Core Added-vs-Modified misinference bug: new GLEntryLines carry
        // a client-generated GUID (Guid.CreateVersion7()), so when attached only via the already-tracked
        // parent's navigation collection, EF's change tracker used to infer Modified instead of Added and
        // generate UPDATEs against non-existent rows (DbUpdateConcurrencyException). The fix was an
        // explicit db.GLEntryLines.AddRange(...) in the handler — this test exercises that exact path
        // end-to-end against a real (InMemory) DbContext rather than mocking it away.
        await using var db = TestDbContextFactory.Create();
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        db.Accounts.AddRange(cash, revenue);
        var entry = GLEntry.CreateDraft(DateOnly.FromDateTime(DateTime.UtcNow), "Original",
            [GLEntryLine.Create(cash, 100m, 0m, null), GLEntryLine.Create(revenue, 0m, 100m, null)]);
        db.GLEntries.Add(entry);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateGLEntryCommandHandler(db);
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var command = new UpdateGLEntryCommand(entry.Id, newDate, "Updated description",
            [
                new CreateGLEntryLineInput(cash.Id, 250m, 0m, null),
                new CreateGLEntryLineInput(revenue.Id, 0m, 250m, null),
            ]);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().NotThrowAsync();
        var dto = await act();
        dto.Description.Should().Be("Updated description");
        dto.EntryDate.Should().Be(newDate);
        dto.Lines.Sum(l => l.DebitAmount).Should().Be(250m);
    }

    [Fact]
    public async Task UpdateGLEntryCommandHandler_WhenNotDraft_ThrowsBusinessRuleException()
    {
        await using var db = TestDbContextFactory.Create();
        var cash = NewAccount(AccountType.Asset, "1010");
        var revenue = NewAccount(AccountType.Revenue, "4010");
        db.Accounts.AddRange(cash, revenue);
        var entry = GLEntry.CreateDraft(DateOnly.FromDateTime(DateTime.UtcNow), "Posted already",
            [GLEntryLine.Create(cash, 100m, 0m, null), GLEntryLine.Create(revenue, 0m, 100m, null)]);
        entry.Post("JE-2026-000101", Guid.NewGuid(), DateTime.UtcNow);
        db.GLEntries.Add(entry);
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateGLEntryCommandHandler(db);
        var command = new UpdateGLEntryCommand(entry.Id, entry.EntryDate, "Should fail",
            [
                new CreateGLEntryLineInput(cash.Id, 100m, 0m, null),
                new CreateGLEntryLineInput(revenue.Id, 0m, 100m, null),
            ]);

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }
}
