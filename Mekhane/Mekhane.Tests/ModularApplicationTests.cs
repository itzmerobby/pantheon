using Xunit;
using Pantheon.Common;

namespace Mekhane.Tests;

public sealed class ModularApplicationTests
{
    [Fact]
    public async Task RunAsync_InvokesModulesInOrderAndForwardsCancellationToken()
    {
        var events = new List<string>();
        using var cancellationSource = new CancellationTokenSource();
        var builder = new TestApplicationBuilder(events);
        var application = new ModularApplication<TestApplication, TestApplicationBuilder>(builder)
            .AddModule(new TestModule(events, "first"))
            .AddModule(new TestModule(events, "second"));

        await application.RunAsync(cancellationSource.Token);

        Assert.Equal(
            [
                "first:configure-builder",
                "second:configure-builder",
                "build",
                "first:configure-application",
                "second:configure-application",
                "run"
            ],
            events);
        Assert.Equal(cancellationSource.Token, builder.Application!.CancellationToken);
    }

    [Fact]
    public void Run_RejectsAnApplicationWithoutModulesBeforeBuilding()
    {
        var builder = new TestApplicationBuilder([]);
        var application = new ModularApplication<TestApplication, TestApplicationBuilder>(builder);

        var exception = Assert.Throws<PantheonPackageException>(() => application.Run());

        Assert.Equal("MEKHANE000002", exception.Code);
        Assert.Null(exception.InnerException);
        Assert.Equal(0, builder.BuildCount);
    }

    [Fact]
    public void Run_RejectsMissingDependenciesBeforeConfiguringBuilder()
    {
        var events = new List<string>();
        var builder = new TestApplicationBuilder(events);
        var application = new ModularApplication<TestApplication, TestApplicationBuilder>(builder)
            .AddModule(new DependentModule(events));

        var exception = Assert.Throws<PantheonPackageException>(() => application.Run());

        Assert.Equal("MEKHANE000003", exception.Code);
        Assert.Empty(events);
        Assert.Equal(0, builder.BuildCount);
    }

    [Fact]
    public void Run_RejectsRunningTheSameApplicationTwice()
    {
        var builder = new TestApplicationBuilder([]);
        var application = new ModularApplication<TestApplication, TestApplicationBuilder>(builder)
            .AddModule(new TestModule([], "module"));

        application.Run();
        var exception = Assert.Throws<PantheonPackageException>(() => application.Run());

        Assert.Equal("MEKHANE000001", exception.Code);
        Assert.Equal(1, builder.BuildCount);
    }

    [Fact]
    public void Run_PreservesExceptionsThrownByUserModuleCallbacks()
    {
        var originalException = new InvalidOperationException("Module configuration failed.");
        var builder = new TestApplicationBuilder([]);
        var application = new ModularApplication<TestApplication, TestApplicationBuilder>(builder)
            .AddModule(new ThrowingModule(originalException));

        var exception = Assert.Throws<PantheonPackageException>(() => application.Run());

        Assert.Equal("MEKHANE000008", exception.Code);
        Assert.Same(originalException, exception.InnerException);
    }

    private sealed class TestApplication(List<string> events) : IApplication
    {
        public CancellationToken CancellationToken { get; private set; }

        public void Run() => events.Add("run");

        public Task RunAsync(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            events.Add("run");
            return Task.CompletedTask;
        }
    }

    private sealed class TestApplicationBuilder(List<string> events) : IApplicationBuilder<TestApplication>
    {
        public int BuildCount { get; private set; }

        public TestApplication? Application { get; private set; }

        public TestApplication Build()
        {
            BuildCount++;
            events.Add("build");
            Application = new TestApplication(events);
            return Application;
        }
    }

    private sealed class TestModule(List<string> events, string name) :
        IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [];

        public void ConfigureBuilder(TestApplicationBuilder builder) =>
            events.Add($"{name}:configure-builder");

        public void ConfigureApplication(TestApplication app) =>
            events.Add($"{name}:configure-application");
    }

    private sealed class RequiredModule(List<string> events) :
        IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [];

        public void ConfigureBuilder(TestApplicationBuilder builder) => events.Add("required:configure-builder");

        public void ConfigureApplication(TestApplication app) => events.Add("required:configure-application");
    }

    private sealed class DependentModule(List<string> events) :
        IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [typeof(RequiredModule)];

        public void ConfigureBuilder(TestApplicationBuilder builder) => events.Add("dependent:configure-builder");

        public void ConfigureApplication(TestApplication app) => events.Add("dependent:configure-application");
    }

    private sealed class ThrowingModule(Exception exception) :
        IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [];

        public void ConfigureBuilder(TestApplicationBuilder builder) => throw exception;

        public void ConfigureApplication(TestApplication app)
        {
        }
    }
}
