using PureFreshMilk.Application.Deliveries;

namespace PureFreshMilk.Api.Features.Deliveries;

internal sealed record DayRowResponse(Guid RecipientId, string Name, int ExpectedMl, string Status, int DeliveredMl)
{
    public static DayRowResponse From(DayRow row) => new(row.RecipientId, row.Name, row.ExpectedMl, row.Status, row.DeliveredMl);
}

internal sealed record DayTotalsResponse(int ExpectedMl, int DeliveredMl, int Delivered, int Missed, int NotRecorded)
{
    public static DayTotalsResponse From(DayTotals totals) => new(
        totals.ExpectedMl, totals.DeliveredMl, totals.Delivered, totals.Missed, totals.NotRecorded);
}

internal sealed record DaySheetResponse(DateOnly Date, bool IsToday, IReadOnlyList<DayRowResponse> Rows, DayTotalsResponse Totals)
{
    public static DaySheetResponse From(DaySheet sheet) => new(sheet.Date, sheet.IsToday,
        [.. sheet.Rows.Select(DayRowResponse.From)], DayTotalsResponse.From(sheet.Totals));
}

internal sealed record DaySummaryResponse(DateOnly Date, DayTotalsResponse Totals)
{
    public static DaySummaryResponse From(DaySummary summary) => new(summary.Date, DayTotalsResponse.From(summary.Totals));
}
