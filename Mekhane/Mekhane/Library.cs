using Pantheon.Common;

namespace Mekhane
{
    /// <summary>
    /// A runnable application. Implement this on the type your host produces
    /// (for example, a wrapper around a web or generic host).
    /// </summary>
    public interface IApplication
    {
        /// <summary>Runs the application and blocks until it stops.</summary>
        public void Run();

        /// <summary>Runs the application asynchronously until it stops or is cancelled.</summary>
        /// <param name="cancellationToken">A token used to request shutdown.</param>
        /// <returns>A task that completes when the application stops.</returns>
        public Task RunAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Builds an application once all configuration is in place.
    /// </summary>
    /// <typeparam name="T">The application type this builder produces.</typeparam>
    public interface IApplicationBuilder<T> where T : class, IApplication
    {
        /// <summary>Creates the application from the current builder state.</summary>
        /// <returns>The built application.</returns>
        public T Build();
    }

    /// <summary>
    /// A self-contained unit of functionality.
    /// </summary>
    /// <typeparam name="TApplication">The application type being composed.</typeparam>
    /// <typeparam name="TBuilder">The builder type that produces the application.</typeparam>
    public interface IModule<TApplication, TBuilder>
      where TApplication : class, IApplication
      where TBuilder : IApplicationBuilder<TApplication>
    {
        /// <summary>
        /// Gets the module types this module depends on.
        /// </summary>
        /// <remarks>
        /// Every type listed here must be registered in the same
        /// <see cref="ModularApplication{TApplication, TBuilder}"/>. If a dependency is
        /// missing, startup fails with a <see cref="PantheonPackageException"/>.
        /// Dependencies are never added automatically; this is deliberate, so that
        /// misconfiguration is reported instead of silently corrected.
        /// Return an empty array if the module has no dependencies.
        /// </remarks>
        public Type[] Dependencies { get; }

        /// <summary>
        /// Called before the application is built. Used to register services,
        /// options, and anything else that must exist before <c>Build()</c>.
        /// </summary>
        /// <param name="builder">The builder that will produce the application.</param>
        public void ConfigureBuilder(TBuilder builder);

        /// <summary>
        /// Called after the application is built and before it runs. Use it for
        /// middleware, endpoints, and other post-build setup.
        /// </summary>
        /// <param name="app">The built application.</param>
        public void ConfigureApplication(TApplication app);
    }

    /// <summary>
    /// Composes a builder and a set of modules into a single runnable application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the application is run, startup proceeds in this order: validation,
    /// each module's <see cref="IModule{TApplication, TBuilder}.ConfigureBuilder"/>,
    /// <see cref="IApplicationBuilder{T}.Build"/>, and then each module's
    /// <see cref="IModule{TApplication, TBuilder}.ConfigureApplication"/>.
    /// Modules are invoked in the order they were registered, in both phases.
    /// </para>
    /// <para>
    /// An instance can be run only once. Calling <see cref="Run"/> or
    /// <see cref="RunAsync"/> a second time throws a <see cref="PantheonPackageException"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TApplication">The application type being composed.</typeparam>
    /// <typeparam name="TBuilder">The builder type that produces the application.</typeparam>
    public sealed class ModularApplication<TApplication, TBuilder>
      where TApplication : class, IApplication
      where TBuilder : IApplicationBuilder<TApplication>
    {
        private readonly TBuilder _builder;
        private TApplication? _application;
        private readonly List<IModule<TApplication, TBuilder>> _modules;

        /// <summary>
        /// Creates a modular application using the specified builder.
        /// </summary>
        /// <param name="builder">The builder the registered modules will configure.</param>
        public ModularApplication(TBuilder builder)
        {
            if (builder is null)
            {
                throw new PantheonPackageException(
                    "The application builder cannot be null. Supply a builder that can construct the application.",
                    "MEKHANE000013");
            }
            _builder = builder;
            _application = null;
            _modules = [];
        }

        private void EnsureApplicationOnlyRunsOnce()
        {
            if (_application is not null)
            {
                throw new PantheonPackageException(
                    "This modular application has already been run. Create a new instance before running it again.",
                    "MEKHANE000001");
            }
        }

        private void EnsureApplicationHasModules()
        {
            if (_modules.Count == 0)
            {
                throw new PantheonPackageException(
                    "The application has no modules. Register at least one module before running it.",
                    "MEKHANE000002");
            }
        }

        private void ValidateDependencies()
        {
            var moduleTypes = _modules.Select(m => m.GetType()).ToHashSet();

            foreach (var module in _modules)
            {
                Type[] dependencies;
                try
                {
                    dependencies = module.Dependencies;
                }
                catch (Exception exception)
                {
                    throw new PantheonPackageException(
                        $"Module '{module.GetType().FullName}' threw while reporting its dependencies.",
                        "MEKHANE000005",
                        exception);
                }

                if (dependencies is null)
                {
                    throw new PantheonPackageException(
                        $"Module '{module.GetType().FullName}' returned a null dependency list.",
                        "MEKHANE000006");
                }

                foreach (var dependency in dependencies)
                {
                    if (dependency is null)
                    {
                        throw new PantheonPackageException(
                            $"Module '{module.GetType().FullName}' declared a null dependency. Remove the null entry or specify a module type.",
                            "MEKHANE000007");
                    }

                    if (!moduleTypes.Contains(dependency))
                    {
                        throw new PantheonPackageException(
                            $"Module '{module.GetType().FullName}' depends on '{dependency.FullName}', but that module is not registered. Register the dependency before running the application.",
                            "MEKHANE000003");
                    }
                }
            }
        }

        private void RunPreRunChecks()
        {
            EnsureApplicationOnlyRunsOnce();
            EnsureApplicationHasModules();
            ValidateDependencies();
        }

        private void RunModulesConfigureBuilder()
        {
            foreach (var module in _modules)
            {
                try
                {
                    module.ConfigureBuilder(_builder);
                }
                catch (Exception exception)
                {
                    throw new PantheonPackageException(
                        $"Module '{module.GetType().FullName}' threw while configuring the application builder.",
                        "MEKHANE000008",
                        exception);
                }
            }
        }

        private void RunModulesConfigureApplication()
        {
            if (_application is null)
            {
                throw new PantheonPackageException(
                    "Cannot configure modules because the application has not been built.",
                    "MEKHANE000004");
            }

            foreach (var module in _modules)
            {
                try
                {
                    module.ConfigureApplication(_application);
                }
                catch (Exception exception)
                {
                    throw new PantheonPackageException(
                        $"Module '{module.GetType().FullName}' threw while configuring the built application.",
                        "MEKHANE000009",
                        exception);
                }
            }
        }

        private void RunBuildPipeline()
        {
            RunModulesConfigureBuilder();
            try
            {
                _application = _builder.Build();
            }
            catch (Exception exception)
            {
                throw new PantheonPackageException(
                    $"Application builder '{_builder.GetType().FullName}' threw while building the application.",
                    "MEKHANE000010",
                    exception);
            }
            RunModulesConfigureApplication();
        }

        /// <summary>
        /// Registers a module with this application.
        /// </summary>
        /// <param name="module">The module to register.</param>
        /// <returns>This application.</returns>
        public ModularApplication<TApplication, TBuilder> AddModule(IModule<TApplication, TBuilder> module)
        {
            if (module is null)
            {
                throw new PantheonPackageException(
                    "The module cannot be null. Supply an implementation of IModule.",
                    "MEKHANE000014");
            }
            _modules.Add(module);
            return this;
        }

        /// <summary>
        /// Validates the configuration, builds the application, and runs it,
        /// blocking until it stops.
        /// </summary>
        /// <exception cref="PantheonPackageException">
        /// The application has already been run, has no modules, or a module's
        /// dependency is not registered.
        /// </exception>
        public void Run()
        {
            RunPreRunChecks();
            RunBuildPipeline();
            try
            {
                _application!.Run();
            }
            catch (Exception exception)
            {
                throw new PantheonPackageException(
                    $"Application '{typeof(TApplication).FullName}' threw while running.",
                    "MEKHANE000011",
                    exception);
            }
        }

        /// <summary>
        /// Validates the configuration, builds the application, and runs it
        /// asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token used to request shutdown.</param>
        /// <returns>A task that completes when the application stops.</returns>
        /// <exception cref="PantheonPackageException">
        /// The application has already been run, has no modules, or a module's
        /// dependency is not registered.
        /// </exception>
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            RunPreRunChecks();
            RunBuildPipeline();
            try
            {
                await _application!.RunAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new PantheonPackageException(
                    $"Application '{typeof(TApplication).FullName}' threw while running asynchronously.",
                    "MEKHANE000012",
                    exception);
            }
        }
    }
}