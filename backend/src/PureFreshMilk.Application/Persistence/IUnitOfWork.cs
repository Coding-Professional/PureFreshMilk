namespace PureFreshMilk.Application.Persistence;

/// <summary>
/// Saves everything a use case changed, in one transaction. Implemented by Infrastructure, which also
/// turns a clash with a unique index (two requests racing) into a <c>ConflictException</c>.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
