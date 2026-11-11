using Mekhane.Discovery;
using Pantheon.Common;
using Xunit;

namespace Mekhane.Discovery.Tests;

public sealed class ModuleDiscoveryTests
{
    [Fact]
    public void DiscoverModules_FindsConcreteModulesAndDeduplicatesAssemblies()
    {
        var assembly = typeof(FirstModule).Assembly;

        var modules = ModuleDiscovery.DiscoverModules<TestApplication, TestApplicationBuilder>(
            [assembly, assembly]);

        Assert.Equal(6, modules.Count);
        Assert.Contains(modules, module => module is FirstModule);
        Assert.Contains(modules, module => module is SecondModule);
        Assert.Contains(modules, module => module is IndependentModule);
    }

    [Fact]
    public void DiscoverModules_UsesTheProvidedFactory()
    {
        var factoryCalls = 0;

        var modules = ModuleDiscovery.DiscoverModules<TestApplication, TestApplicationBuilder>(
            [typeof(FirstModule).Assembly],
            type =>
            {
                factoryCalls++;
                return (IModule<TestApplication, TestApplicationBuilder>)Activator.CreateInstance(type)!;
            });

        Assert.Equal(modules.Count, factoryCalls);
        Assert.NotEmpty(modules);
    }

    [Fact]
    public void SortModules_PutsDependenciesBeforeDependents()
    {
        var modules = ModuleDependencySorter.SortModules<TestApplication, TestApplicationBuilder>(
            [new SecondModule(), new FirstModule(), new IndependentModule()]);

        Assert.Equal(
            [typeof(FirstModule), typeof(SecondModule), typeof(IndependentModule)],
            modules.Select(module => module.GetType()));
    }

    [Fact]
    public void SortModules_RejectsMissingDependencies()
    {
        var exception = Assert.Throws<PantheonPackageException>(
            () => ModuleDependencySorter.SortModules<TestApplication, TestApplicationBuilder>(
                [new MissingDependencyModule()]));

        Assert.Equal("MEKHANE000108", exception.Code);
    }

    [Fact]
    public void SortModules_RejectsDuplicateModuleTypes()
    {
        var exception = Assert.Throws<PantheonPackageException>(
            () => ModuleDependencySorter.SortModules<TestApplication, TestApplicationBuilder>(
                [new FirstModule(), new FirstModule()]));

        Assert.Equal("MEKHANE000104", exception.Code);
    }

    [Fact]
    public void SortModules_RejectsDependencyCycles()
    {
        var exception = Assert.Throws<PantheonPackageException>(
            () => ModuleDependencySorter.SortModules<TestApplication, TestApplicationBuilder>(
                [new CyclicFirstModule(), new CyclicSecondModule()]));

        Assert.Equal("MEKHANE000109", exception.Code);
    }

    public sealed class TestApplication : IApplication
    {
        public void Run()
        {
        }

        public Task RunAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class TestApplicationBuilder : IApplicationBuilder<TestApplication>
    {
        public TestApplication Build() => new();
    }

    public sealed class FirstModule : IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [];

        public void ConfigureBuilder(TestApplicationBuilder builder)
        {
        }

        public void ConfigureApplication(TestApplication app)
        {
        }
    }

    public sealed class SecondModule : IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [typeof(FirstModule)];

        public void ConfigureBuilder(TestApplicationBuilder builder)
        {
        }

        public void ConfigureApplication(TestApplication app)
        {
        }
    }

    public sealed class IndependentModule : IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [];

        public void ConfigureBuilder(TestApplicationBuilder builder)
        {
        }

        public void ConfigureApplication(TestApplication app)
        {
        }
    }

    public sealed class MissingDependencyModule : IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [typeof(FirstModule)];

        public void ConfigureBuilder(TestApplicationBuilder builder)
        {
        }

        public void ConfigureApplication(TestApplication app)
        {
        }
    }

    public sealed class CyclicFirstModule : IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [typeof(CyclicSecondModule)];

        public void ConfigureBuilder(TestApplicationBuilder builder)
        {
        }

        public void ConfigureApplication(TestApplication app)
        {
        }
    }

    public sealed class CyclicSecondModule : IModule<TestApplication, TestApplicationBuilder>
    {
        public Type[] Dependencies => [typeof(CyclicFirstModule)];

        public void ConfigureBuilder(TestApplicationBuilder builder)
        {
        }

        public void ConfigureApplication(TestApplication app)
        {
        }
    }
}
