using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wex.Cards.Application.Cards.Commands;
using Wex.Cards.Application.Cards.Queries;

namespace Wex.Cards.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<CreateCardService>();
        services.AddScoped<GetCardService>();
        return services;
    }
}
