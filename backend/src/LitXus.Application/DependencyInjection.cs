using System.Reflection;
using FluentValidation;
using LitXus.Application.Common.Behaviors;
using LitXus.Application.Common.Interfaces;
using LitXus.Application.Modules.Accounting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LitXus.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        services.AddSingleton<ISstCalculator, SstCalculator>();

        return services;
    }
}
