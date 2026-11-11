using Mekhane;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pantheon.Common;

namespace Mekhane.AspNet;

/// <summary>
/// A forwarding wrapper for <see cref="WebApplication"/> that also implements
/// Mekhane's <see cref="IApplication"/> abstraction.
/// </summary>
public sealed class ModularWebApplication :
    IApplication,
    IHost,
    IApplicationBuilder,
    IEndpointRouteBuilder,
    IDisposable,
    IAsyncDisposable
{
    public ModularWebApplication(WebApplication application)
    {
        if (application is null)
        {
            throw new PantheonPackageException(
                "The ASP.NET Core application cannot be null. Supply a built WebApplication.",
                "MEKHANE000015");
        }
        Application = application;
    }

    /// <summary>The wrapped ASP.NET Core application.</summary>
    public WebApplication Application { get; }

    public IServiceProvider Services => Application.Services;

    public IConfiguration Configuration => Application.Configuration;

    public IHostEnvironment Environment => Application.Environment;

    public IHostApplicationLifetime Lifetime => Application.Lifetime;

    public ILogger Logger => Application.Logger;

    public ICollection<string> Urls => Application.Urls;

    IServiceProvider IApplicationBuilder.ApplicationServices
    {
        get => ((IApplicationBuilder)Application).ApplicationServices;
        set => ((IApplicationBuilder)Application).ApplicationServices = value;
    }

    IFeatureCollection IApplicationBuilder.ServerFeatures =>
        ((IApplicationBuilder)Application).ServerFeatures;

    IDictionary<string, object?> IApplicationBuilder.Properties =>
        ((IApplicationBuilder)Application).Properties;

    ICollection<EndpointDataSource> IEndpointRouteBuilder.DataSources =>
        ((IEndpointRouteBuilder)Application).DataSources;

    IServiceProvider IEndpointRouteBuilder.ServiceProvider =>
        ((IEndpointRouteBuilder)Application).ServiceProvider;

    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) =>
        ((IApplicationBuilder)Application).Use(middleware);

    public IApplicationBuilder New() =>
        ((IApplicationBuilder)Application).New();

    RequestDelegate IApplicationBuilder.Build() =>
        ((IApplicationBuilder)Application).Build();

    IApplicationBuilder IEndpointRouteBuilder.CreateApplicationBuilder() =>
        ((IEndpointRouteBuilder)Application).CreateApplicationBuilder();

    public void Run() => Application.Run();

    public void Run(string? url) => Application.Run(url);

    public Task RunAsync(CancellationToken cancellationToken = default) =>
        ((IHost)Application).RunAsync(cancellationToken);

    public Task RunAsync(string? url) => Application.RunAsync(url);

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        Application.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default) =>
        Application.StopAsync(cancellationToken);

    public void Dispose() => ((IDisposable)Application).Dispose();

    public ValueTask DisposeAsync() => Application.DisposeAsync();

    /// <summary>Creates a standard ASP.NET Core application.</summary>
    public static ModularWebApplication Create(string[]? args = null) =>
        new(WebApplication.Create(args ?? []));

    /// <summary>Creates a standard ASP.NET Core application builder.</summary>
    public static ModularWebApplicationBuilder CreateBuilder(string[]? args = null) =>
        ModularWebApplicationBuilder.Create(args);

    /// <summary>Creates a standard ASP.NET Core application builder.</summary>
    public static ModularWebApplicationBuilder CreateBuilder(WebApplicationOptions options) =>
        ModularWebApplicationBuilder.Create(options);

    /// <summary>Creates a slim ASP.NET Core application builder.</summary>
    public static ModularWebApplicationBuilder CreateSlimBuilder(string[]? args = null) =>
        ModularWebApplicationBuilder.CreateSlim(args);

    /// <summary>Creates a slim ASP.NET Core application builder.</summary>
    public static ModularWebApplicationBuilder CreateSlimBuilder(WebApplicationOptions options) =>
        ModularWebApplicationBuilder.CreateSlim(options);

    /// <summary>Creates an empty ASP.NET Core application builder.</summary>
    public static ModularWebApplicationBuilder CreateEmptyBuilder(WebApplicationOptions options) =>
        ModularWebApplicationBuilder.CreateEmpty(options);
}

/// <summary>
/// A forwarding wrapper for <see cref="WebApplicationBuilder"/> that implements
/// Mekhane's <see cref="IApplicationBuilder{T}"/> abstraction.
/// </summary>
public sealed class ModularWebApplicationBuilder :
    IApplicationBuilder<ModularWebApplication>,
    IHostApplicationBuilder
{
    public ModularWebApplicationBuilder(WebApplicationBuilder builder)
    {
        if (builder is null)
        {
            throw new PantheonPackageException(
                "The ASP.NET Core application builder cannot be null. Supply a WebApplicationBuilder.",
                "MEKHANE000016");
        }
        Builder = builder;
    }

    /// <summary>The wrapped ASP.NET Core application builder.</summary>
    public WebApplicationBuilder Builder { get; }

    public IHostEnvironment Environment => Builder.Environment;

    public ConfigurationManager Configuration => Builder.Configuration;

    IConfigurationManager IHostApplicationBuilder.Configuration => Builder.Configuration;

    public IServiceCollection Services => Builder.Services;

    public ILoggingBuilder Logging => Builder.Logging;

    public IMetricsBuilder Metrics => Builder.Metrics;

    public ConfigureHostBuilder Host => Builder.Host;

    public ConfigureWebHostBuilder WebHost => Builder.WebHost;

    IDictionary<object, object> IHostApplicationBuilder.Properties =>
        ((IHostApplicationBuilder)Builder).Properties;

    public void ConfigureContainer<TContainerBuilder>(
        IServiceProviderFactory<TContainerBuilder> factory,
        Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull =>
        ((IHostApplicationBuilder)Builder).ConfigureContainer(factory, configure);

    /// <summary>Creates a wrapper around a standard ASP.NET Core builder.</summary>
    public static ModularWebApplicationBuilder Create(string[]? args = null) =>
        new(WebApplication.CreateBuilder(args ?? []));

    /// <summary>Creates a wrapper using standard ASP.NET Core application options.</summary>
    public static ModularWebApplicationBuilder Create(WebApplicationOptions options) =>
        new(WebApplication.CreateBuilder(options));

    /// <summary>Creates a wrapper around a slim ASP.NET Core builder.</summary>
    public static ModularWebApplicationBuilder CreateSlim(string[]? args = null) =>
        new(WebApplication.CreateSlimBuilder(args ?? []));

    /// <summary>Creates a wrapper using options for a slim ASP.NET Core builder.</summary>
    public static ModularWebApplicationBuilder CreateSlim(WebApplicationOptions options) =>
        new(WebApplication.CreateSlimBuilder(options));

    /// <summary>Creates a wrapper around an empty ASP.NET Core builder.</summary>
    public static ModularWebApplicationBuilder CreateEmpty(WebApplicationOptions options) =>
        new(WebApplication.CreateEmptyBuilder(options));

    public ModularWebApplication Build() => new(Builder.Build());
}
