namespace PureFreshMilk.TestSupport;

/// <summary>A clock that stands still until a test moves it.</summary>
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    private DateTimeOffset utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => utcNow;

    public void Set(DateTimeOffset value) => utcNow = value;

    public void Advance(TimeSpan by) => utcNow += by;
}
