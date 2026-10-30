using PureFreshMilk.Application.Households;
using PureFreshMilk.Application.Rates;
using PureFreshMilk.Application.Recipients;
using PureFreshMilk.TestSupport;

namespace PureFreshMilk.Application.Tests;

/// <summary>
/// A household's worth of handlers wired to in-memory persistence. The clock stands at 06:00 UTC on
/// 30 October 2026, which is 11:30 on the same day in Asia/Kolkata.
/// </summary>
internal sealed class TestHousehold
{
    public static readonly DateTimeOffset Now = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = new(2026, 10, 30);

    public TestHousehold()
    {
        Database = new InMemoryDatabase();
        Clock = new FixedTimeProvider(Now);

        var households = new InMemoryHouseholdRepository(Database);
        var recipients = new InMemoryRecipientRepository(Database);
        var rates = new InMemoryMilkRateRepository(Database);

        GetHousehold = new GetHouseholdHandler(households, Clock);
        SetUpHousehold = new SetUpHouseholdHandler(households, households, Database, Clock);

        ListRecipients = new ListRecipientsHandler(households, recipients, Clock);
        GetRecipient = new GetRecipientHandler(households, recipients);
        AddRecipient = new AddRecipientHandler(households, recipients, Database, Clock);
        UpdateRecipient = new UpdateRecipientHandler(households, recipients, Database, Clock);
        ArchiveRecipient = new ArchiveRecipientHandler(households, recipients, Database, Clock);
        UnarchiveRecipient = new UnarchiveRecipientHandler(households, recipients, Database, Clock);

        ListRates = new ListMilkRatesHandler(households, rates);
        GetRateInEffect = new GetRateInEffectHandler(households, rates, Clock);
        AddRate = new AddMilkRateHandler(households, rates, Database, Clock);
        UpdateRate = new UpdateMilkRateHandler(households, rates, Database, Clock);
        DeleteRate = new DeleteMilkRateHandler(households, rates, Database);
    }

    public InMemoryDatabase Database { get; }

    public FixedTimeProvider Clock { get; }

    public GetHouseholdHandler GetHousehold { get; }

    public SetUpHouseholdHandler SetUpHousehold { get; }

    public ListRecipientsHandler ListRecipients { get; }

    public GetRecipientHandler GetRecipient { get; }

    public AddRecipientHandler AddRecipient { get; }

    public UpdateRecipientHandler UpdateRecipient { get; }

    public ArchiveRecipientHandler ArchiveRecipient { get; }

    public UnarchiveRecipientHandler UnarchiveRecipient { get; }

    public ListMilkRatesHandler ListRates { get; }

    public GetRateInEffectHandler GetRateInEffect { get; }

    public AddMilkRateHandler AddRate { get; }

    public UpdateMilkRateHandler UpdateRate { get; }

    public DeleteMilkRateHandler DeleteRate { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A household that has been set up, as the first screen of the app does.</summary>
    public static async Task<TestHousehold> SetUpAsync(string timeZoneId = "Asia/Kolkata")
    {
        var household = new TestHousehold();
        await household.SetUpHousehold.HandleAsync(new SetUpHouseholdCommand("Example household", timeZoneId), Ct);
        household.Database.ResetSaveCount();
        return household;
    }

    public async Task<RecipientDetails> AddRecipientAsync(
        string name, int expectedMl = 1000, DateOnly? startsOn = null) =>
        await AddRecipient.HandleAsync(new AddRecipientCommand(name, expectedMl, startsOn), Ct);

    public async Task<MilkRateDetails> AddRateAsync(DateOnly effectiveFrom, long pricePerLitrePaise) =>
        await AddRate.HandleAsync(new AddMilkRateCommand(effectiveFrom, pricePerLitrePaise), Ct);
}
