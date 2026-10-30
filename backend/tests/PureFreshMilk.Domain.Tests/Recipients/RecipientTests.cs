using System.Globalization;
using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Households;
using PureFreshMilk.Domain.Recipients;

namespace PureFreshMilk.Domain.Tests.Recipients;

public sealed class RecipientTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);
    private static readonly HouseholdId Household = HouseholdId.New(Now);
    private static readonly DateOnly Start = new(2026, 10, 1);

    private static Recipient NewRecipient(string name = "Recipient A", int millilitres = 1000) =>
        Recipient.Add(Household, name, Quantity.FromMillilitres(millilitres), Start, Now);

    [Fact]
    public void Add_stores_the_details_and_starts_active()
    {
        var recipient = Recipient.Add(Household, "  Recipient A ", Quantity.FromMillilitres(1000), Start, Now);

        Assert.NotEqual(Guid.Empty, recipient.Id.Value);
        Assert.Equal(Household, recipient.HouseholdId);
        Assert.Equal("Recipient A", recipient.Name);
        Assert.Equal(1000, recipient.ExpectedQuantity.Millilitres);
        Assert.Equal(Start, recipient.StartsOn);
        Assert.Null(recipient.ArchivedFrom);
        Assert.Equal(Now, recipient.CreatedAt);
        Assert.Equal(Now, recipient.UpdatedAt);
    }

    [Theory]
    [Trait("Invariant", "R-1")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Add_rejects_a_missing_name(string? name)
    {
        var exception = Assert.Throws<DomainException>(
            () => Recipient.Add(Household, name, Quantity.FromMillilitres(1000), Start, Now));

        Assert.Equal("name", exception.Field);
    }

    [Fact]
    [Trait("Invariant", "R-1")]
    public void Name_can_have_80_characters_but_not_81()
    {
        Assert.Equal(80, NewRecipient(new string('a', 80)).Name.Length);

        var exception = Assert.Throws<DomainException>(() => NewRecipient(new string('a', 81)));
        Assert.Equal("name", exception.Field);
    }

    [Theory]
    [Trait("Invariant", "R-2")]
    [InlineData(1)]
    [InlineData(250)]
    [InlineData(20_000)]
    public void The_usual_quantity_can_be_between_1_and_20000_ml(int millilitres)
    {
        Assert.Equal(millilitres, NewRecipient(millilitres: millilitres).ExpectedQuantity.Millilitres);
    }

    [Theory]
    [Trait("Invariant", "R-2")]
    [InlineData(0)]
    [InlineData(20_001)]
    [InlineData(int.MaxValue)]
    public void The_usual_quantity_outside_1_to_20000_ml_is_rejected(int millilitres)
    {
        var exception = Assert.Throws<DomainException>(() => NewRecipient(millilitres: millilitres));

        Assert.Equal("expectedMl", exception.Field);
    }

    [Fact]
    [Trait("Invariant", "R-3")]
    public void The_start_date_is_required()
    {
        var exception = Assert.Throws<DomainException>(
            () => Recipient.Add(Household, "Recipient A", Quantity.FromMillilitres(1000), default, Now));

        Assert.Equal("startsOn", exception.Field);
    }

    [Theory]
    [Trait("Invariant", "R-4")]
    [InlineData("2026-09-30", false)]
    [InlineData("2026-10-01", true)]
    [InlineData("2026-10-14", true)]
    [InlineData("2026-10-15", false)]
    [InlineData("2026-10-16", false)]
    public void A_recipient_is_active_from_the_start_date_until_the_day_before_archiving(string date, bool expected)
    {
        var recipient = NewRecipient();
        recipient.Archive(new DateOnly(2026, 10, 15), Now);

        Assert.Equal(expected, recipient.IsActiveOn(DateOnly.Parse(date, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [Trait("Invariant", "R-4")]
    [InlineData("2026-09-30", false)]
    [InlineData("2026-10-01", true)]
    [InlineData("2035-01-01", true)]
    public void A_recipient_who_is_not_archived_stays_active_after_the_start_date(string date, bool expected)
    {
        Assert.Equal(expected, NewRecipient().IsActiveOn(DateOnly.Parse(date, CultureInfo.InvariantCulture)));
    }

    [Fact]
    [Trait("Invariant", "R-5")]
    public void Archive_records_the_first_day_without_deliveries()
    {
        var recipient = NewRecipient();
        var later = Now.AddHours(1);

        recipient.Archive(new DateOnly(2026, 10, 31), later);

        Assert.Equal(new DateOnly(2026, 10, 31), recipient.ArchivedFrom);
        Assert.Equal(later, recipient.UpdatedAt);
    }

    [Theory]
    [Trait("Invariant", "R-5")]
    [InlineData("2026-10-01")]
    [InlineData("2026-09-01")]
    public void Archive_needs_a_date_after_the_start_date(string from)
    {
        var recipient = NewRecipient();

        var exception = Assert.Throws<DomainException>(
            () => recipient.Archive(DateOnly.Parse(from, CultureInfo.InvariantCulture), Now));

        Assert.Equal("archivedFrom", exception.Field);
        Assert.Null(recipient.ArchivedFrom);
    }

    [Fact]
    [Trait("Invariant", "R-5")]
    public void Archiving_an_archived_recipient_is_rejected()
    {
        var recipient = NewRecipient();
        recipient.Archive(new DateOnly(2026, 10, 15), Now);

        Assert.Throws<DomainException>(() => recipient.Archive(new DateOnly(2026, 10, 20), Now));
        Assert.Equal(new DateOnly(2026, 10, 15), recipient.ArchivedFrom);
    }

    [Fact]
    [Trait("Invariant", "R-5")]
    public void Unarchive_clears_the_archive_date()
    {
        var recipient = NewRecipient();
        recipient.Archive(new DateOnly(2026, 10, 15), Now);
        var later = Now.AddHours(1);

        recipient.Unarchive(later);

        Assert.Null(recipient.ArchivedFrom);
        Assert.Equal(later, recipient.UpdatedAt);
        Assert.True(recipient.IsActiveOn(new DateOnly(2026, 10, 20)));
    }

    [Fact]
    [Trait("Invariant", "R-5")]
    public void Unarchive_does_nothing_when_the_recipient_is_not_archived()
    {
        var recipient = NewRecipient();

        recipient.Unarchive(Now.AddHours(1));

        Assert.Null(recipient.ArchivedFrom);
        Assert.Equal(Now, recipient.UpdatedAt);
    }

    [Fact]
    public void An_archive_date_in_the_future_still_counts_as_active_today()
    {
        var recipient = NewRecipient();
        recipient.Archive(new DateOnly(2026, 12, 1), Now);

        Assert.False(recipient.IsArchivedOn(new DateOnly(2026, 11, 30)));
        Assert.True(recipient.IsArchivedOn(new DateOnly(2026, 12, 1)));
        Assert.True(recipient.IsArchivedOn(new DateOnly(2027, 1, 1)));
        Assert.False(NewRecipient().IsArchivedOn(new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void Update_changes_the_editable_fields()
    {
        var recipient = NewRecipient();
        var later = Now.AddHours(1);

        recipient.Update("Recipient B", Quantity.FromMillilitres(500), new DateOnly(2026, 9, 1), later);

        Assert.Equal("Recipient B", recipient.Name);
        Assert.Equal(500, recipient.ExpectedQuantity.Millilitres);
        Assert.Equal(new DateOnly(2026, 9, 1), recipient.StartsOn);
        Assert.Equal(later, recipient.UpdatedAt);
    }

    [Fact]
    public void A_rejected_update_leaves_the_recipient_unchanged()
    {
        var recipient = NewRecipient();

        Assert.Throws<DomainException>(
            () => recipient.Update("Recipient B", Quantity.FromMillilitres(0), Start, Now.AddHours(1)));

        Assert.Equal("Recipient A", recipient.Name);
        Assert.Equal(1000, recipient.ExpectedQuantity.Millilitres);
        Assert.Equal(Now, recipient.UpdatedAt);
    }

    [Fact]
    public void The_start_date_cannot_be_moved_to_or_after_the_archive_date()
    {
        var recipient = NewRecipient();
        recipient.Archive(new DateOnly(2026, 10, 15), Now);

        var exception = Assert.Throws<DomainException>(
            () => recipient.Update("Recipient A", Quantity.FromMillilitres(1000), new DateOnly(2026, 10, 15), Now));

        Assert.Equal("startsOn", exception.Field);
        Assert.Equal(Start, recipient.StartsOn);
    }
}
