using Microsoft.EntityFrameworkCore;
using Wex.Cards.Domain.Entities;

namespace Wex.Cards.Infrastructure.Persistence;

public class CardsDbContext(DbContextOptions<CardsDbContext> options) : DbContext(options)
{
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CardsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
