namespace PureFreshMilk.Application.Deliveries;

public sealed record DayRow(Guid RecipientId, string Name, int ExpectedMl, string Status, int DeliveredMl);

public sealed record DayTotals(int ExpectedMl, int DeliveredMl, int Delivered, int Missed, int NotRecorded);

public sealed record DaySheet(DateOnly Date, bool IsToday, IReadOnlyList<DayRow> Rows, DayTotals Totals);

public sealed record DaySummary(DateOnly Date, DayTotals Totals);
