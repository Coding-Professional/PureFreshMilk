using Microsoft.EntityFrameworkCore;
using Npgsql;
using PureFreshMilk.Application.Common;
using PureFreshMilk.Application.Persistence;
using PureFreshMilk.Infrastructure.Persistence.Configurations;

namespace PureFreshMilk.Infrastructure.Persistence;

internal sealed class UnitOfWork(PureFreshMilkDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (ToConflict(exception) is { } conflict)
        {
            // Two requests raced past the friendly check in the use case; the unique index decided.
            throw conflict;
        }
    }

    private static ConflictException? ToConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } violation
            ? violation.ConstraintName switch
            {
                RecipientConfiguration.ActiveNameIndexName => new ConflictException(
                    ErrorCodes.RecipientNameTaken, "Another recipient who is not archived already has that name."),
                MilkRateConfiguration.EffectiveFromIndexName => new ConflictException(
                    ErrorCodes.RateDateTaken, "A price already starts on that date."),
                _ => null,
            }
            : null;
}
