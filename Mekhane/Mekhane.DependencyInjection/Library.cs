using Mekhane;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Mekhane.DependencyInjection;

/// <summary>
/// A Mekhane application backed by the .NET Generic Host.
/// </summary>
public sealed class DependencyInjectionApplication :
    IApplication,
    IHost,
    IDisposable
{
    /// <summary>
    /// Wraps a built Generic Host.
    /// </summary>
    /// <param name="host">The host to wrap.</param>
    public DependencyInjectionApplication(IHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        Host = host;
    }

    /// <summary>The wrapped Generic Host.</summary>
    public IHost Host { get; }

    /// <inheritdoc />
    public IServiceProvider Services => Host.Services;

    /// <inheritdoc />
    public void Run() => Host.Run();

    /// <inheritdoc />
    public Task RunAsync(CancellationToken cancellationToken = default) =>
        Host.RunAsync(cancellationToken);

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default) =>
        Host.StartAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default) =>
        Host.StopAsync(cancellationToken);

    /// <inheritdoc />
    public void Dispose() => Host.Dispose();

}

/// <summary>
/// Configures and builds a Generic Host-backed Mekhane application.
/// </summary>
public sealed class DependencyInjectionApplicationBuilder :
    IApplicationBuilder<DependencyInjectionApplication>
{
    private readonly HostApplicationBuilder _builder;

    /// <summary>
    /// Creates a builder around a Generic Host application builder.
    /// </summary>
    /// <param name="builder">The host builder to wrap.</param>
    public DependencyInjectionApplicationBuilder(HostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _builder = builder;
    }

    /// <summary>The service collection used to configure the application.</summary>
    public IServiceCollection Services => _builder.Services;

    /// <summary>Registers a hosted service as the application's entrypoint.</summary>
    /// <typeparam name="TEntryPoint">The hosted service to run with the host.</typeparam>
    /// <returns>This builder.</returns>
    public DependencyInjectionApplicationBuilder UseEntryPoint<TEntryPoint>()
        where TEntryPoint : class, IHostedService
    {
        Services.AddHostedService<TEntryPoint>();
        return this;
    }

    /// <summary>Creates a builder using the standard Generic Host defaults.</summary>
    /// <param name="args">Command-line arguments for host configuration.</param>
    /// <returns>A new application builder.</returns>
    public static DependencyInjectionApplicationBuilder CreateBuilder(string[]? args = null) =>
        new(Host.CreateApplicationBuilder(args ?? []));

    /// <inheritdoc />
    public DependencyInjectionApplication Build() => new(_builder.Build());
}
