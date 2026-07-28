using ChatApp.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Application;

/// <summary>Marks this assembly for MediatR/FluentValidation assembly scanning.</summary>
internal sealed class AssemblyMarker;

/// <summary>Registers everything this layer needs: the mediator, its two pipeline behaviors, and every FluentValidation validator (§4.1, §10). Ports (<c>Abstractions/</c>) are not registered here — they are implemented and wired by whichever outer layer provides them.</summary>
public static class DependencyInjection
{
    /// <summary>Adds MediatR (scanning this assembly, with <see cref="ValidationBehavior{TRequest,TResponse}"/> then <see cref="LoggingBehavior{TRequest,TResponse}"/> as open behaviors) and every validator in this assembly.</summary>
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
