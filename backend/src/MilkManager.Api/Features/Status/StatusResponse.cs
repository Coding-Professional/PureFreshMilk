namespace MilkManager.Api.Features.Status;

/// <summary>Response body for <c>GET /api/v1/status</c>.</summary>
internal sealed record StatusResponse(string Service, string Version, DateTimeOffset ServerTime);
