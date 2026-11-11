using Mekhane.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Pantheon.Common;
using Xunit;

namespace Mekhane.DependencyInjection.Tests;

public sealed class DependencyInjectionApplicationBuilderTests
{
    [Fact]
    public void Build_ExposesRegisteredServicesAndEntryPoint()
    {
        var builder = DependencyInjectionApplicationBuilder.CreateBuilder([]);
        builder.Services.AddSingleton<TestService>();
        builder.UseEntryPoint<TestHostedService>();

        using var app = builder.Build();

        Assert.NotNull(app.Services.GetRequiredService<TestService>());
        Assert.Contains(app.Services.GetServices<IHostedService>(), service => service is TestHostedService);
    }

    [Fact]
    public void Constructor_RejectsNullHostBuilder()
    {
        Assert.Throws<PantheonPackageException>(() => new DependencyInjectionApplicationBuilder(null!));
    }

    private sealed class TestService;

    private sealed class TestHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
