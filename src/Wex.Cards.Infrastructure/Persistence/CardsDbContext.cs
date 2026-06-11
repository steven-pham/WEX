using Microsoft.EntityFrameworkCore;

namespace Wex.Cards.Infrastructure.Persistence;

public class CardsDbContext(DbContextOptions<CardsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CardsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
