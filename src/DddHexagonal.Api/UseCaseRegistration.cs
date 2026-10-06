using DddHexagonal.Application.Ports.In;

namespace DddHexagonal.Api;

internal static class UseCaseRegistration
{
    /// <summary>
    /// Registers every <c>IUseCase&lt;,&gt;</c> implementation of the Application assembly. Adding a use case
    /// needs no change here: it is picked up by convention (one small loop instead of N registration lines).
    /// </summary>
    public static IServiceCollection AddUseCases(this IServiceCollection services)
    {
        var implementations =
            from type in typeof(IUseCase<,>).Assembly.GetTypes()
            where type is { IsClass: true, IsAbstract: false }
            from contract in type.GetInterfaces()
            where contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IUseCase<,>)
            select (contract, type);

        foreach (var (contract, type) in implementations)
        {
            services.AddScoped(contract, type);
        }

        return services;
    }
}
