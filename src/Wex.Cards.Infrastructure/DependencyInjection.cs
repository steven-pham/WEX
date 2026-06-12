using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wex.Cards.Application.Ports;
using Wex.Cards.Infrastructure.Persistence;
using Wex.Cards.Infrastructure.Persistence.Repositories;

namespace Wex.Cards.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<CardsDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(CardsDbContext).Assembly.FullName)));

        services.AddScoped<ICardRepository, CardRepository>();

        return services;
    }
}
