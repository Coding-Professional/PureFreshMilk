using Microsoft.EntityFrameworkCore;

namespace MilkManager.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the PostgreSQL database. Internal so that only this
/// assembly (repositories, configurations, migrations) touches persistence directly.
/// No entities are mapped yet; see docs/architecture/overview.md for the mapping conventions.
/// </summary>
internal sealed class MilkManagerDbContext(DbContextOptions<MilkManagerDbContext> options)
    : DbContext(options);
