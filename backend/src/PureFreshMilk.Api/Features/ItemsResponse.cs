namespace PureFreshMilk.Api.Features;

/// <summary>
/// The body of every list endpoint: an object with an <c>items</c> array, so a field can be added later
/// without breaking clients.
/// </summary>
internal sealed record ItemsResponse<T>(IReadOnlyList<T> Items);
