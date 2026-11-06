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
        /// missing, startup fails with an <see cref="InvalidOperationException"/>.
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
    /// <see cref="RunAsync"/> a second time throws an <see cref="InvalidOperationException"/>.
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
            ArgumentNullException.ThrowIfNull(builder);
            _builder = builder;
            _application = null;
            _modules = [];
        }

        private void EnsureApplicationOnlyRunsOnce()
        {
            if (_application is not null)
            {
                throw new InvalidOperationException("The application has already been built and is running, make sure you are not calling Run() or RunAsync() multiple times on the same ModularApplication instance.");
            }
        }

        private void EnsureApplicationHasModules()
        {
            if (_modules.Count == 0)
            {
                throw new InvalidOperationException("The application has no modules. Please add at least one module before running the application.");
            }
        }

        private void ValidateDependencies()
        {
            var moduleTypes = _modules.Select(m => m.GetType()).ToHashSet();

            foreach (var module in _modules)
            {
                foreach (var dependency in module.Dependencies)
                {
                    if (!moduleTypes.Contains(dependency))
                    {
                        throw new InvalidOperationException($"Module {module.GetType().Name} depends on {dependency.Name}, which is not yet registered. A common mistake is to register the orders in th wrong order. Please ensure that all dependencies are registered in the correct order before the module that depends on them.");
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
                module.ConfigureBuilder(_builder);
            }
        }

        private void RunModulesConfigureApplication()
        {
            if (_application is null)
            {
                throw new InvalidOperationException("Cannot configure application because it has not been built yet. Call Build() first.");
            }

            foreach (var module in _modules)
            {
                module.ConfigureApplication(_application);
            }
        }

        private void RunBuildPipeline()
        {
            RunModulesConfigureBuilder();
            _application = _builder.Build();
            RunModulesConfigureApplication();
        }

        /// <summary>
        /// Registers a module with this application.
        /// </summary>
        /// <param name="module">The module to register.</param>
        /// <returns>This application.</returns>
        public ModularApplication<TApplication, TBuilder> AddModule(IModule<TApplication, TBuilder> module)
        {
            ArgumentNullException.ThrowIfNull(module);
            _modules.Add(module);
            return this;
        }

        /// <summary>
        /// Validates the configuration, builds the application, and runs it,
        /// blocking until it stops.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The application has already been run, has no modules, or a module's
        /// dependency is not registered.
        /// </exception>
        public void Run()
        {
            RunPreRunChecks();
            RunBuildPipeline();
            _application!.Run();
        }

        /// <summary>
        /// Validates the configuration, builds the application, and runs it
        /// asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token used to request shutdown.</param>
        /// <returns>A task that completes when the application stops.</returns>
        /// <exception cref="InvalidOperationException">
        /// The application has already been run, has no modules, or a module's
        /// dependency is not registered.
        /// </exception>
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            RunPreRunChecks();
            RunBuildPipeline();
            await _application!.RunAsync(cancellationToken);
        }
    }
}