using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace PureFreshMilk.Api.Tests;

/// <summary>Small helpers for reading what the API sends back and for the household every data test needs.</summary>
internal static class ApiJson
{
    public static async Task<JsonElement> ReadAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    /// <summary>Asserts a Problem Details response and returns its body.</summary>
    public static async Task<JsonElement> ReadProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.ReadAsync();
        Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
        return problem;
    }

    /// <summary>The validation messages for one field of a 400 response.</summary>
    public static string[] ErrorsFor(this JsonElement problem, string field) =>
        [.. problem.GetProperty("errors").GetProperty(field).EnumerateArray().Select(message => message.GetString()!)];

    public static async Task<HttpResponseMessage> SetUpHouseholdAsync(this HttpClient client, string timeZoneId = "Asia/Kolkata") =>
        await client.PutAsJsonAsync(
            "/api/v1/household", new { name = "Example household", timeZoneId }, TestContext.Current.CancellationToken);

    public static async Task<JsonElement> AddRecipientAsync(
        this HttpClient client, string name, int expectedMl = 1000, string? startsOn = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/recipients", new { name, expectedMl, startsOn }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync();
    }

    public static async Task<JsonElement> AddRateAsync(this HttpClient client, string effectiveFrom, long pricePerLitrePaise)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/milk-rates", new { effectiveFrom, pricePerLitrePaise }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadAsync();
    }
}
