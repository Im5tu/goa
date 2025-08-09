# Goa.Functions.Core

Core runtime and bootstrapping functionality for high-performance AWS Lambda functions. This package provides the foundational components for building Lambda functions with the Goa framework.

## Installation

```bash
dotnet add package Goa.Functions.Core
```

## Features

- Native AOT support for faster Lambda cold starts
- Built-in dependency injection with Microsoft.Extensions.DependencyInjection
- Configuration management with environment variables
- Structured logging support
- Lambda runtime abstraction and bootstrapping
- Minimal overhead and optimized performance

## Usage

### Basic Lambda Function

```csharp
using Goa.Functions.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Configure services
var builder = Host.CreateDefaultBuilder(args)
    .UseLambdaLifecycle()
    .WithServices(services =>
    {
        services.AddScoped<IMyService, MyService>();
        services.AddLogging();
    });

// Build and run as Lambda function
var function = new MyLambdaFunction(/* inject dependencies */);
var runnable = new Runnable(builder);
await runnable.RunAsync();

public class MyLambdaFunction : ILambdaFunction<string, string>
{
    private readonly IMyService _service;
    private readonly ILogger<MyLambdaFunction> _logger;
    
    public MyLambdaFunction(IMyService service, ILogger<MyLambdaFunction> logger)
    {
        _service = service;
        _logger = logger;
    }
    
    public async Task<string> InvokeAsync(string request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing request: {Request}", request);
        
        var result = await _service.ProcessAsync(request);
        
        _logger.LogInformation("Processing complete");
        
        return result;
    }
}
```

### Using LambdaBootstrapService

```csharp
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddScoped<IDataProcessor, DataProcessor>();
                services.AddHostedService<LambdaBootstrapService<string, string>>();
            })
            .Build();
            
        await host.RunAsync();
    }
}
```

### Custom Runtime Configuration

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .UseLambdaLifecycle()
    .WithServices(services =>
    {
        // Add your services
        services.AddScoped<IRepository, Repository>();
    })
    .WithConfiguration((context, config) =>
    {
        // Add custom configuration sources
        config.AddEnvironmentVariables();
    });
```

## Key Interfaces

- `IRunnable` - Interface for running Lambda functions
- `ILambdaFunction<TRequest, TResponse>` - Base interface for Lambda function handlers
- `ILambdaBuilder` - Builder interface for configuring Lambda functions
- `ILambdaRuntimeClient` - Interface for Lambda runtime communication

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).