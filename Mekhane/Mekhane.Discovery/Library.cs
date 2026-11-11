using System.Reflection;
using Mekhane;
using Pantheon.Common;

namespace Mekhane.Discovery;

/// <summary>
/// Discovers and creates Mekhane modules from assemblies.
/// </summary>
public static class ModuleDiscovery
{
    /// <summary>
    /// A function that governs what types are considered module types.
    /// </summary>
    /// <typeparam name="TApplication"></typeparam>
    /// <typeparam name="TBuilder"></typeparam>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool IsModuleType<TApplication, TBuilder>(Type type)
        where TApplication : class, IApplication
        where TBuilder : IApplicationBuilder<TApplication>
    {
        return type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && typeof(IModule<TApplication, TBuilder>).IsAssignableFrom(type);
    }

    /// <summary>
    /// Finds concrete module types in the specified assemblies and creates their instances.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="moduleFactory">
    /// An optional factory for creating discovered module types. When omitted, each module
    /// must have a public parameterless constructor.
    /// </param>
    /// <returns>The discovered module instances in assembly and type enumeration order.</returns>
    /// <exception cref="PantheonPackageException">An assembly or the assembly collection is null.</exception>
    /// <exception cref="PantheonPackageException">An assembly cannot be scanned or a module cannot be created.</exception>
    public static IReadOnlyList<IModule<TApplication, TBuilder>> DiscoverModules<TApplication, TBuilder>(
        IEnumerable<Assembly> assemblies,
        Func<Type, IModule<TApplication, TBuilder>>? moduleFactory = null)
        where TApplication : class, IApplication
        where TBuilder : IApplicationBuilder<TApplication>
    {
        if (assemblies is null)
        {
            throw new PantheonPackageException(
                "The assembly collection cannot be null. Supply one or more assemblies to scan.",
                "MEKHANE000110");
        }

        var discoveredTypes = new HashSet<Type>();
        var modules = new List<IModule<TApplication, TBuilder>>();

        foreach (var assembly in assemblies)
        {
            if (assembly is null)
            {
                throw new PantheonPackageException(
                    "The assembly collection contains a null entry. Remove it or provide an assembly to scan.",
                    "MEKHANE000111");
            }

            foreach (var moduleType in GetLoadableTypes(assembly))
            {
                if (!IsModuleType<TApplication, TBuilder>(moduleType) || !discoveredTypes.Add(moduleType))
                {
                    continue;
                }

                modules.Add(CreateModule<TApplication, TBuilder>(moduleType, moduleFactory));
            }
        }

        return modules;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var loadErrors = exception.LoaderExceptions
                .Where(error => error is not null)
                .Select(error => error!.Message);
            throw new PantheonPackageException(
                $"Could not load all types from assembly '{assembly.FullName}'. Loader errors: {string.Join(" | ", loadErrors)}",
                "MEKHANE000101");
        }
    }

    private static IModule<TApplication, TBuilder> CreateModule<TApplication, TBuilder>(
        Type moduleType,
        Func<Type, IModule<TApplication, TBuilder>>? moduleFactory)
        where TApplication : class, IApplication
        where TBuilder : IApplicationBuilder<TApplication>
    {
        object? module;
        try
        {
            module = moduleFactory is null
                ? Activator.CreateInstance(moduleType)
                : moduleFactory(moduleType);
        }
        catch (Exception exception)
        {
            throw new PantheonPackageException(
                $"Could not create module '{moduleType.FullName}'. Provide a factory when the module requires constructor dependencies, and inspect the inner exception for details from its constructor or factory.",
                "MEKHANE000102",
                exception);
        }

        return module as IModule<TApplication, TBuilder>
            ?? throw new PantheonPackageException(
                $"The module factory did not return an instance of '{moduleType.FullName}'. Return an object implementing the requested module interface.",
                "MEKHANE000103");
    }
}

/// <summary>
/// Orders modules so that each module's dependencies precede it.
/// </summary>
public static class ModuleDependencySorter
{
    /// <summary>
    /// Sorts module instances into dependency order.
    /// </summary>
    /// <param name="modules">The modules to sort.</param>
    /// <returns>Modules in dependency order, preserving input order where possible.</returns>
    /// <exception cref="PantheonPackageException">The module collection or an entry is null.</exception>
    /// <exception cref="PantheonPackageException">
    /// A module type is duplicated, a dependency is missing, or dependencies contain a cycle.
    /// </exception>
    public static IReadOnlyList<IModule<TApplication, TBuilder>> SortModules<TApplication, TBuilder>(
        IEnumerable<IModule<TApplication, TBuilder>> modules)
        where TApplication : class, IApplication
        where TBuilder : IApplicationBuilder<TApplication>
    {
        if (modules is null)
        {
            throw new PantheonPackageException(
                "The module collection cannot be null. Supply the modules to order.",
                "MEKHANE000112");
        }

        var moduleList = modules.ToList();
        var modulesByType = new Dictionary<Type, IModule<TApplication, TBuilder>>();
        var moduleOrder = new Dictionary<Type, int>();

        for (var index = 0; index < moduleList.Count; index++)
        {
            var module = moduleList[index]
                ?? throw new PantheonPackageException(
                    "The module collection contains a null entry. Remove it or provide a module instance.",
                    "MEKHANE000113");
            var moduleType = module.GetType();

            if (!modulesByType.TryAdd(moduleType, module))
            {
                throw new PantheonPackageException(
                    $"Module type '{moduleType.FullName}' was provided more than once. Register each module type only once.",
                    "MEKHANE000104");
            }

            moduleOrder.Add(moduleType, index);
        }

        var dependentModules = modulesByType.Keys.ToDictionary(
            moduleType => moduleType,
            _ => new List<Type>());
        var dependencyCounts = modulesByType.Keys.ToDictionary(moduleType => moduleType, _ => 0);

        foreach (var module in moduleList)
        {
            var moduleType = module.GetType();
            Type[]? dependencies;
            try
            {
                dependencies = module.Dependencies;
            }
            catch (Exception exception)
            {
                throw new PantheonPackageException(
                    $"Module '{moduleType.FullName}' threw while reporting its dependencies.",
                    "MEKHANE000105",
                    exception);
            }

            if (dependencies is null)
            {
                throw new PantheonPackageException(
                    $"Module '{moduleType.FullName}' returned a null dependency list. Return an empty array when the module has no dependencies.",
                    "MEKHANE000106");
            }

            foreach (var dependencyType in dependencies.Distinct())
            {
                if (dependencyType is null)
                {
                    throw new PantheonPackageException(
                        $"Module '{moduleType.FullName}' contains a null dependency. Remove the null entry or specify a module type.",
                        "MEKHANE000107");
                }

                if (!modulesByType.ContainsKey(dependencyType))
                {
                    throw new PantheonPackageException(
                        $"Module '{moduleType.FullName}' depends on '{dependencyType.FullName}', which was not discovered. Include that module in the collection before sorting.",
                        "MEKHANE000108");
                }

                dependentModules[dependencyType].Add(moduleType);
                dependencyCounts[moduleType]++;
            }
        }

        var readyModules = new PriorityQueue<Type, int>();
        foreach (var (moduleType, index) in moduleOrder)
        {
            if (dependencyCounts[moduleType] == 0)
            {
                readyModules.Enqueue(moduleType, index);
            }
        }

        var sortedModules = new List<IModule<TApplication, TBuilder>>(moduleList.Count);

        while (readyModules.TryDequeue(out var readyModuleType, out _))
        {
            sortedModules.Add(modulesByType[readyModuleType]);

            foreach (var dependentModuleType in dependentModules[readyModuleType])
            {
                dependencyCounts[dependentModuleType]--;
                if (dependencyCounts[dependentModuleType] == 0)
                {
                    readyModules.Enqueue(dependentModuleType, moduleOrder[dependentModuleType]);
                }
            }
        }

        if (sortedModules.Count != moduleList.Count)
        {
            var cycleModules = moduleOrder.Keys
                .Where(moduleType => dependencyCounts[moduleType] > 0)
                .Select(moduleType => moduleType.FullName);
            throw new PantheonPackageException(
                $"Module dependencies contain a cycle involving: {string.Join(", ", cycleModules)}. Remove a dependency edge to make the modules acyclic.",
                "MEKHANE000109");
        }

        return sortedModules;
    }
}

/// <summary>
/// Registers discovered modules with a modular application.
/// </summary>
public static class ModuleRegistration
{
    /// <summary>
    /// Discovers modules in the specified assemblies, orders them by dependency, and registers them.
    /// </summary>
    /// <param name="application">The application to configure.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="moduleFactory">
    /// An optional factory for creating discovered module types. When omitted, each module
    /// must have a public parameterless constructor.
    /// </param>
    /// <returns>The configured application.</returns>
    public static ModularApplication<TApplication, TBuilder> AddModulesFromAssemblies<TApplication, TBuilder>(
        this ModularApplication<TApplication, TBuilder> application,
        IEnumerable<Assembly> assemblies,
        Func<Type, IModule<TApplication, TBuilder>>? moduleFactory = null)
        where TApplication : class, IApplication
        where TBuilder : IApplicationBuilder<TApplication>
    {
        if (application is null)
        {
            throw new PantheonPackageException(
                "The modular application cannot be null. Supply an application to configure.",
                "MEKHANE000114");
        }
        var discoveredModules = ModuleDiscovery.DiscoverModules<TApplication, TBuilder>(assemblies, moduleFactory);
        var sortedModules = ModuleDependencySorter.SortModules(discoveredModules);

        foreach (var module in sortedModules)
        {
            application.AddModule(module);
        }

        return application;
    }
}
