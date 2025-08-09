# Goa.Core

Core utilities and shared functionality for the Goa framework. This package provides foundational components that are shared across all Goa libraries.

## Installation

```bash
dotnet add package Goa.Core
```

## Features

- Native AOT support for faster Lambda cold starts
- Core utilities and helper methods
- Shared abstractions and interfaces
- Lightweight dependency injection extensions
- Common error handling patterns
- Minimal dependencies for optimal performance

## Overview

Goa.Core provides the foundational building blocks for the entire Goa framework, including:

- Core abstractions used by AWS service clients
- Shared utility methods and extensions
- Common interfaces for dependency injection
- Base error handling and result patterns
- Performance-optimized implementations

## Usage

This package is typically used as a dependency by other Goa packages rather than directly in application code. However, it provides useful utilities that can be leveraged:

```csharp
using Goa.Core;
using Microsoft.Extensions.DependencyInjection;

// Register core services
services.AddGoaCore();

// Use core utilities
var result = GoaUtilities.ValidateAwsArn(arnString);
```

### Dependency Injection Extensions

```csharp
using Goa.Core.Extensions;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Add core logging and configuration
services.AddGoaLogging();
services.AddGoaConfiguration();
```

### Error Handling Patterns

```csharp
using Goa.Core.Results;

public async Task<Result<User>> GetUserAsync(string id)
{
    if (string.IsNullOrEmpty(id))
    {
        return Result.Failure<User>("User ID is required");
    }
    
    try
    {
        var user = await _repository.GetAsync(id);
        return Result.Success(user);
    }
    catch (Exception ex)
    {
        return Result.Failure<User>($"Failed to get user: {ex.Message}");
    }
}
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).