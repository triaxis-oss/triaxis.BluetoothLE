namespace Microsoft.Extensions.DependencyInjection;

using triaxis.BluetoothLE;

/// <summary>
/// triaxis.BluetoothLE dependency injection extension methods
/// </summary>
public static class BluetoothLEDependencyInjectionExtensions
{
    /// <summary>
    /// Add the platform-specific <see cref="IBluetoothLE" /> implementation to the container
    /// </summary>
    public static IServiceCollection AddBluetoothLE(this IServiceCollection services)
    {
        services.AddSingleton<IBluetoothLE, Platform>();
        return services;
    }
}
