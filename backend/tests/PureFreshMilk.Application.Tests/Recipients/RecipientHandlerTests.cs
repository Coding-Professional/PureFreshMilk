using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.Domain.Common;

namespace PureFreshMilk.Application.Tests.Recipients;

public sealed class RecipientHandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Adding_stores_the_recipient_and_starts_it_today_by_default()
    {
        var app = await TestHousehold.SetUpAsync();

        var recipient = await app.AddRecipientAsync("  Recipient A ", 1000);

        Assert.Equal("Recipient A", recipient.Name);
        Assert.Equal(1000, recipient.ExpectedMl);
        Assert.Equal(TestHousehold.Today, recipient.StartsOn);
        Assert.Null(recipient.ArchivedFrom);
        Assert.Equal(recipient.Id, Assert.Single(app.Database.Recipients).Id.Value);
        Assert.Equal(1, app.Database.SaveCount);
    }

    [Fact]
    public async Task Adding_can_start_the_recipient_on_another_day()
    {
        var app = await TestHousehold.SetUpAsync();

        var recipient = await app.AddRecipientAsync("Recipient A", 500, new DateOnly(2026, 10, 1));

        Assert.Equal(new DateOnly(2026, 10, 1), recipient.StartsOn);
    }

    [Fact]
    public async Task Adding_before_the_household_is_set_up_is_not_found()
    {
        var app = new TestHousehold();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => app.AddRecipientAsync("Recipient A"));

        Assert.Equal(ErrorCodes.HouseholdNotSetUp, exception.Code);
    }

    [Theory]
    [InlineData(null, 1000, "name")]
    [InlineData("  ", 1000, "name")]
    [InlineData("Recipient A", 0, "expectedMl")]
    [InlineData("Recipient A", -250, "expectedMl")]
    [InlineData("Recipient A", 20_001, "expectedMl")]
    public async Task Invalid_input_is_rejected_with_the_field_and_nothing_is_stored(
        string? name, int expectedMl, string field)
    {
        var app = await TestHousehold.SetUpAsync();

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => app.AddRecipient.HandleAsync(new AddRecipientCommand(name, expectedMl, null), Ct));

        Assert.Equal(field, exception.Field);
        Assert.Empty(app.Database.Recipients);
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task A_name_already_used_by_an_active_recipient_is_a_conflict_whatever_its_case()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRecipientAsync("Recipient A");

        var exception = await Assert.ThrowsAsync<ConflictException>(() => app.AddRecipientAsync("  recipient a "));

        Assert.Equal(ErrorCodes.RecipientNameTaken, exception.Code);
        Assert.Single(app.Database.Recipients);
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task The_name_of_an_archived_recipient_can_be_used_again()
    {
        var app = await TestHousehold.SetUpAsync();
        var first = await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));
        await app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(first.Id, null), Ct);

        var second = await app.AddRecipientAsync("Recipient A");

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(2, app.Database.Recipients.Count);
    }

    [Fact]
    [Trait("Invariant", "R-9")]
    public async Task The_list_hides_archived_recipients_unless_asked_for()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRecipientAsync("Recipient B", 500, new DateOnly(2026, 10, 1));
        var gone = await app.AddRecipientAsync("Recipient C", 500, new DateOnly(2026, 10, 1));
        await app.AddRecipientAsync("Recipient A", 1000, new DateOnly(2026, 10, 1));
        await app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(gone.Id, new DateOnly(2026, 10, 20)), Ct);

        var active = await app.ListRecipients.HandleAsync(includeArchived: false, Ct);
        var all = await app.ListRecipients.HandleAsync(includeArchived: true, Ct);

        Assert.Equal(["Recipient A", "Recipient B"], active.Select(recipient => recipient.Name));
        Assert.Equal(["Recipient A", "Recipient B", "Recipient C"], all.Select(recipient => recipient.Name));
    }

    [Fact]
    [Trait("Invariant", "R-9")]
    public async Task A_recipient_archived_from_a_future_day_still_appears_in_the_default_list()
    {
        var app = await TestHousehold.SetUpAsync();
        var recipient = await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));
        await app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(recipient.Id, new DateOnly(2026, 12, 1)), Ct);

        var active = await app.ListRecipients.HandleAsync(includeArchived: false, Ct);

        Assert.Single(active);
    }

    [Fact]
    public async Task The_list_is_ordered_by_name_ignoring_case()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRecipientAsync("charlie");
        await app.AddRecipientAsync("Alice");
        await app.AddRecipientAsync("bob");

        var list = await app.ListRecipients.HandleAsync(includeArchived: false, Ct);

        Assert.Equal(["Alice", "bob", "charlie"], list.Select(recipient => recipient.Name));
    }

    [Fact]
    public async Task The_list_is_empty_for_a_household_with_no_recipients()
    {
        var app = await TestHousehold.SetUpAsync();

        Assert.Empty(await app.ListRecipients.HandleAsync(includeArchived: true, Ct));
    }

    [Fact]
    public async Task Listing_before_the_household_is_set_up_is_not_found()
    {
        var app = new TestHousehold();

        await Assert.ThrowsAsync<NotFoundException>(() => app.ListRecipients.HandleAsync(false, Ct));
    }

    [Fact]
    public async Task Getting_a_recipient_returns_it()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A");

        var found = await app.GetRecipient.HandleAsync(added.Id, Ct);

        Assert.Equal(added, found);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Getting_an_unknown_recipient_is_not_found(bool emptyId)
    {
        var app = await TestHousehold.SetUpAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => app.GetRecipient.HandleAsync(emptyId ? Guid.Empty : Guid.CreateVersion7(), Ct));

        Assert.Equal(ErrorCodes.RecipientNotFound, exception.Code);
    }

    [Fact]
    public async Task Updating_replaces_the_editable_fields()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A", 1000, new DateOnly(2026, 10, 1));
        app.Database.ResetSaveCount();

        var updated = await app.UpdateRecipient.HandleAsync(
            new UpdateRecipientCommand(added.Id, " Recipient Z ", 500, new DateOnly(2026, 9, 15)), Ct);

        Assert.Equal(new RecipientDetails(added.Id, "Recipient Z", 500, new DateOnly(2026, 9, 15), null), updated);
        Assert.Equal(1, app.Database.SaveCount);
    }

    [Fact]
    public async Task A_recipient_can_change_the_case_of_its_own_name()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("recipient a");

        var updated = await app.UpdateRecipient.HandleAsync(
            new UpdateRecipientCommand(added.Id, "Recipient A", 1000, added.StartsOn), Ct);

        Assert.Equal("Recipient A", updated.Name);
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task Renaming_to_the_name_of_another_active_recipient_is_a_conflict_and_changes_nothing()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRecipientAsync("Recipient A");
        var other = await app.AddRecipientAsync("Recipient B", 500);
        app.Database.ResetSaveCount();

        var exception = await Assert.ThrowsAsync<ConflictException>(() => app.UpdateRecipient.HandleAsync(
            new UpdateRecipientCommand(other.Id, "RECIPIENT A", 250, other.StartsOn), Ct));

        Assert.Equal(ErrorCodes.RecipientNameTaken, exception.Code);
        Assert.Equal(other, await app.GetRecipient.HandleAsync(other.Id, Ct));
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    public async Task An_archived_recipient_can_be_renamed_to_a_name_in_use()
    {
        var app = await TestHousehold.SetUpAsync();
        await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));
        var archived = await app.AddRecipientAsync("Recipient B", startsOn: new DateOnly(2026, 10, 1));
        await app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(archived.Id, null), Ct);

        var updated = await app.UpdateRecipient.HandleAsync(
            new UpdateRecipientCommand(archived.Id, "Recipient A", 500, archived.StartsOn), Ct);

        Assert.Equal("Recipient A", updated.Name);
    }

    [Theory]
    [InlineData("", 500, "name")]
    [InlineData("Recipient A", 0, "expectedMl")]
    public async Task An_invalid_update_is_rejected_and_changes_nothing(string name, int expectedMl, string field)
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A", 1000);
        app.Database.ResetSaveCount();

        var exception = await Assert.ThrowsAsync<DomainException>(() => app.UpdateRecipient.HandleAsync(
            new UpdateRecipientCommand(added.Id, name, expectedMl, added.StartsOn), Ct));

        Assert.Equal(field, exception.Field);
        Assert.Equal(added, await app.GetRecipient.HandleAsync(added.Id, Ct));
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    public async Task Updating_an_unknown_recipient_is_not_found()
    {
        var app = await TestHousehold.SetUpAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => app.UpdateRecipient.HandleAsync(
            new UpdateRecipientCommand(Guid.CreateVersion7(), "Recipient A", 500, TestHousehold.Today), Ct));

        Assert.Equal(ErrorCodes.RecipientNotFound, exception.Code);
    }

    [Fact]
    public async Task Archiving_defaults_to_today()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));

        var archived = await app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(added.Id, null), Ct);

        Assert.Equal(TestHousehold.Today, archived.ArchivedFrom);
        Assert.Equal(TestHousehold.Today, Assert.Single(app.Database.Recipients).ArchivedFrom);
    }

    [Fact]
    public async Task Archiving_can_name_the_day()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));

        var archived = await app.ArchiveRecipient.HandleAsync(
            new ArchiveRecipientCommand(added.Id, new DateOnly(2026, 11, 15)), Ct);

        Assert.Equal(new DateOnly(2026, 11, 15), archived.ArchivedFrom);
    }

    [Fact]
    [Trait("Invariant", "R-5")]
    public async Task Archiving_on_or_before_the_start_date_is_rejected()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));
        app.Database.ResetSaveCount();

        var exception = await Assert.ThrowsAsync<DomainException>(() => app.ArchiveRecipient.HandleAsync(
            new ArchiveRecipientCommand(added.Id, new DateOnly(2026, 10, 1)), Ct));

        Assert.Equal("archivedFrom", exception.Field);
        Assert.Null(Assert.Single(app.Database.Recipients).ArchivedFrom);
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    [Trait("Invariant", "R-5")]
    public async Task Archiving_an_archived_recipient_is_a_conflict()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));
        await app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(added.Id, null), Ct);

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(added.Id, new DateOnly(2026, 11, 1)), Ct));

        Assert.Equal(ErrorCodes.RecipientAlreadyArchived, exception.Code);
        Assert.Equal(TestHousehold.Today, Assert.Single(app.Database.Recipients).ArchivedFrom);
    }

    [Fact]
    public async Task Archiving_an_unknown_recipient_is_not_found()
    {
        var app = await TestHousehold.SetUpAsync();

        var exception = await Assert.ThrowsAsync<NotFoundException>(() => app.ArchiveRecipient.HandleAsync(
            new ArchiveRecipientCommand(Guid.CreateVersion7(), null), Ct));

        Assert.Equal(ErrorCodes.RecipientNotFound, exception.Code);
    }

    [Fact]
    public async Task Archiving_never_deletes_the_recipient()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));

        await app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(added.Id, null), Ct);

        Assert.Single(app.Database.Recipients);
        Assert.Equal(added.Id, (await app.GetRecipient.HandleAsync(added.Id, Ct)).Id);
    }

    [Fact]
    public async Task Unarchiving_brings_the_recipient_back()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));
        await app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(added.Id, null), Ct);

        var restored = await app.UnarchiveRecipient.HandleAsync(added.Id, Ct);

        Assert.Null(restored.ArchivedFrom);
        Assert.Single(await app.ListRecipients.HandleAsync(includeArchived: false, Ct));
    }

    [Fact]
    public async Task Unarchiving_a_recipient_who_is_not_archived_does_nothing()
    {
        var app = await TestHousehold.SetUpAsync();
        var added = await app.AddRecipientAsync("Recipient A");
        app.Database.ResetSaveCount();

        var result = await app.UnarchiveRecipient.HandleAsync(added.Id, Ct);

        Assert.Equal(added, result);
        Assert.Equal(0, app.Database.SaveCount);
    }

    [Fact]
    [Trait("Invariant", "R-8")]
    public async Task Unarchiving_into_a_name_that_is_now_taken_is_a_conflict()
    {
        var app = await TestHousehold.SetUpAsync();
        var first = await app.AddRecipientAsync("Recipient A", startsOn: new DateOnly(2026, 10, 1));
        await app.ArchiveRecipient.HandleAsync(new ArchiveRecipientCommand(first.Id, null), Ct);
        await app.AddRecipientAsync("Recipient A");

        var exception = await Assert.ThrowsAsync<ConflictException>(() => app.UnarchiveRecipient.HandleAsync(first.Id, Ct));

        Assert.Equal(ErrorCodes.RecipientNameTaken, exception.Code);
        Assert.Equal(TestHousehold.Today, app.Database.Recipients.Single(recipient => recipient.Id.Value == first.Id).ArchivedFrom);
    }

    [Fact]
    public async Task Unarchiving_an_unknown_recipient_is_not_found()
    {
        var app = await TestHousehold.SetUpAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => app.UnarchiveRecipient.HandleAsync(Guid.CreateVersion7(), Ct));
    }
}
