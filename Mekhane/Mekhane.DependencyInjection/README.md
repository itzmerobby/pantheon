# Mekhane.DependencyInjection

Generic Host and dependency injection integration for Mekhane applications.

```csharp
using Mekhane.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = DependencyInjectionApplicationBuilder.CreateBuilder(args);
builder.UseEntryPoint<Worker>();

using var app = builder.Build();
await app.RunAsync();

public sealed class Worker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
```

`UseEntryPoint<T>()` is optional. When omitted, the Generic Host runs any hosted
services registered through `Services`.
