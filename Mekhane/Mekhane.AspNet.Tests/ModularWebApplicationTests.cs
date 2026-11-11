using Mekhane.AspNet;
using Microsoft.Extensions.DependencyInjection;
using Pantheon.Common;
using Xunit;

namespace Mekhane.AspNet.Tests;

public sealed class ModularWebApplicationTests
{
    [Fact]
    public void CreateBuilderAndBuild_ExposeConfiguredServicesOnTheApplication()
    {
        var builder = ModularWebApplication.CreateBuilder([]);
        builder.Services.AddSingleton<TestService>();

        using var app = builder.Build();

        Assert.IsType<TestService>(app.Services.GetRequiredService<TestService>());
        Assert.Same(app.Application.Services, app.Services);
    }

    [Fact]
    public void CreateSlimBuilder_ProducesAWorkingWrappedApplication()
    {
        var builder = ModularWebApplication.CreateSlimBuilder([]);

        using var app = builder.Build();

        Assert.NotNull(app.Environment);
        Assert.NotNull(app.Configuration);
    }

    [Fact]
    public void Constructors_RejectNullApplicationsAndBuilders()
    {
        Assert.Throws<PantheonPackageException>(() => new ModularWebApplication(null!));
        Assert.Throws<PantheonPackageException>(() => new ModularWebApplicationBuilder(null!));
    }

    private sealed class TestService;
}
