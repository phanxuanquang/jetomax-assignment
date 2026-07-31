using ChatApp.Application.Common.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Application;

internal sealed class AssemblyMarker;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

        return services;
    }
}
