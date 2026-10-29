namespace PureFreshMilk.Application.Status;

public sealed record SystemStatus(string Version, DateTimeOffset ServerTime);
