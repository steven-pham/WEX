using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Wex.Cards.Infrastructure.Persistence;

internal sealed class CardsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CardsDbContext>
{
    public CardsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CardsDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=wexcards;Username=wex;Password=wex_dev_password",
            npgsql => npgsql.MigrationsAssembly(typeof(CardsDbContext).Assembly.FullName));
        return new CardsDbContext(optionsBuilder.Options);
    }
}
