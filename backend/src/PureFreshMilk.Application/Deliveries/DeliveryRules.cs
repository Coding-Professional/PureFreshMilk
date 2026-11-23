using PureFreshMilk.Application.Common;
using PureFreshMilk.Domain.Common;
using PureFreshMilk.Domain.Deliveries;
using PureFreshMilk.Domain.Households;

namespace PureFreshMilk.Application.Deliveries;

internal static class DeliveryRules
{
    public static DateOnly ResolveDate(string value, Household household, DateTimeOffset now)
    {
        var today = household.Today(now);
        if (value.Equals("today", StringComparison.OrdinalIgnoreCase))
        {
            return today;
        }

        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
        {
            throw new DomainException("Use a date in YYYY-MM-DD format or 'today'.", "date");
        }

        if (date > today)
        {
            throw new FutureDateException("date");
        }

        return date;
    }

    public static DeliveryStatus ParseStatus(string? status) => status switch
    {
        "delivered" => DeliveryStatus.Delivered,
        "missed" => DeliveryStatus.Missed,
        _ => throw new DomainException("Status must be delivered or missed.", "status"),
    };
}
