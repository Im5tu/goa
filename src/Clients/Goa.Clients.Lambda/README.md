# Goa.Clients.Lambda

Lambda client for function invocation in high-performance AWS Lambda functions. This package provides a lightweight, AOT-ready Lambda client optimized for minimal cold start times.

## Installation

```bash
dotnet add package Goa.Clients.Lambda
```

## Features

- Native AOT support for faster Lambda cold starts
- Minimal dependencies and memory allocations
- Built-in error handling with ErrorOr pattern
- Support for synchronous and asynchronous invocations
- Support for dry-run validation
- Type-safe payload handling with JSON serialization

## Usage

### Basic Setup

```csharp
using Goa.Clients.Lambda;
using Microsoft.Extensions.DependencyInjection;

// Register Lambda client
services.AddLambda();

// Or with custom configuration
services.AddLambda(config =>
{
    config.ServiceUrl = "http://localhost:3001"; // For LocalStack
    config.Region = "us-west-2";
    config.LogLevel = LogLevel.Debug;
});
```

### Synchronous Invocation

```csharp
using System.Text.Json;
using ErrorOr;
using Goa.Clients.Lambda;
using Goa.Clients.Lambda.Operations.Invoke;

public class WorkflowService
{
    private readonly ILambdaClient _lambda;
    
    public WorkflowService(ILambdaClient lambda)
    {
        _lambda = lambda;
    }
    
    public async Task<ErrorOr<ProcessResult>> ProcessDataAsync(ProcessRequest request)
    {
        var invokeRequest = new InvokeRequest
        {
            FunctionName = "data-processor",
            Payload = JsonSerializer.Serialize(request)
        };
        
        var result = await _lambda.InvokeSynchronousAsync(invokeRequest);
        
        if (result.IsError)
            return result.FirstError;
            
        // Deserialize the response payload
        var response = result.Value;
        if (response.Payload != null)
        {
            var processResult = JsonSerializer.Deserialize<ProcessResult>(response.Payload);
            return processResult;
        }
        
        return ErrorOr.Error.Failure("EmptyResponse", "No payload returned from function");
    }
}
```

### Asynchronous Invocation

```csharp
using Goa.Clients.Lambda.Operations.InvokeAsync;

public async Task<ErrorOr<Success>> TriggerBackgroundTaskAsync(BackgroundTask task)
{
    var invokeRequest = new InvokeAsyncRequest
    {
        FunctionName = "background-processor",
        Payload = JsonSerializer.Serialize(task)
    };
    
    var result = await _lambda.InvokeAsynchronousAsync(invokeRequest);
        
    if (result.IsError)
    {
        Console.WriteLine($"Failed to invoke function: {result.FirstError}");
        return result.FirstError;
    }
    
    return ErrorOr.Success;
}
```

### Synchronous with Event Invocation Type

```csharp
using Goa.Clients.Lambda.Models;

public async Task<ErrorOr<Success>> TriggerEventAsync(EventData eventData)
{
    var invokeRequest = new InvokeRequest
    {
        FunctionName = "event-processor",
        InvocationType = InvocationType.Event,
        Payload = JsonSerializer.Serialize(eventData)
    };
    
    var result = await _lambda.InvokeSynchronousAsync(invokeRequest);
    return result.IsError ? result.FirstError : ErrorOr.Success;
}
```

### Dry Run Validation

```csharp
using Goa.Clients.Lambda.Operations.InvokeDryRun;

public async Task<ErrorOr<bool>> ValidateInvocationAsync(string functionName)
{
    var dryRunRequest = new InvokeDryRunRequest
    {
        FunctionName = functionName,
        Payload = "{\"test\": true}"
    };
    
    var result = await _lambda.InvokeDryRunAsync(dryRunRequest);
    
    if (result.IsError)
    {
        Console.WriteLine($"Validation failed: {result.FirstError}");
        return false;
    }
    
    return true; // Validation successful
}
```

### Advanced Options

```csharp
public async Task<ErrorOr<string>> InvokeWithAdvancedOptionsAsync()
{
    var invokeRequest = new InvokeRequest
    {
        FunctionName = "my-function",
        Qualifier = "PROD", // Version or alias
        LogType = LogType.Tail, // Include logs in response
        ClientContext = "eyJjdXN0b20iOiJkYXRhIn0=", // Base64 encoded context
        Payload = "{\"input\": \"data\"}"
    };
    
    var result = await _lambda.InvokeSynchronousAsync(invokeRequest);
    
    if (result.IsError)
        return result.FirstError;
    
    var response = result.Value;
    
    // Check for function errors
    if (response.FunctionError != null)
    {
        return ErrorOr.Error.Failure("FunctionError", $"Function returned error: {response.FunctionError}");
    }
    
    // Access logs if requested
    if (!string.IsNullOrEmpty(response.LogResult))
    {
        var logs = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(response.LogResult));
        Console.WriteLine($"Function logs: {logs}");
    }
    
    return response.Payload ?? "";
}
```

### Error Handling

```csharp
var result = await _lambda.InvokeSynchronousAsync(invokeRequest);

if (result.IsError)
{
    // Handle client errors (network, auth, etc.)
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Client Error: {error.Description}");
    }
    return;
}

var response = result.Value;

// Check for function execution errors
if (response.FunctionError != null)
{
    Console.WriteLine($"Function Error Type: {response.FunctionError}");
    Console.WriteLine($"Function Error Payload: {response.Payload}");
}

// Check HTTP status
if (response.StatusCode != 200 && response.StatusCode != 202)
{
    Console.WriteLine($"Unexpected status code: {response.StatusCode}");
}
```

## Available Operations

- **InvokeSynchronousAsync**: Invoke function and wait for response
- **InvokeAsynchronousAsync**: Fire-and-forget asynchronous invocation
- **InvokeDryRunAsync**: Validate invocation without executing the function

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).