# Mekhane.Discovery

Assembly discovery and dependency ordering for Mekhane modules.

```shell
dotnet add package Mekhane.Discovery
```

Discover modules in one or more assemblies, sort them by their declared
dependencies, and register them with a modular application:

```csharp
using System.Reflection;
using Mekhane;
using Mekhane.Discovery;
using Microsoft.Extensions.DependencyInjection;

var application = new ModularApplication<MyApplication, MyApplicationBuilder>(builder);
application.AddModulesFromAssemblies([Assembly.GetExecutingAssembly()]);
```

By default, discovered modules need a public parameterless constructor. Pass a
factory to create modules that require dependencies:

```csharp
application.AddModulesFromAssemblies(
    [Assembly.GetExecutingAssembly()],
    moduleType => (IModule<MyApplication, MyApplicationBuilder>)ActivatorUtilities.CreateInstance(
        services,
        moduleType));
```

The stages are also available individually through
`ModuleDiscovery.DiscoverModules`, `ModuleDependencySorter.SortModules`, and
`ModuleRegistration.RegisterModules`. Package-detected discovery and dependency
configuration failures throw `PantheonPackageException` from the
`Pantheon.Common` package; inspect its `Code` property for stable identification.
Exceptions thrown by user-provided module constructors, factories, or dependency
properties are available as `InnerException`.
