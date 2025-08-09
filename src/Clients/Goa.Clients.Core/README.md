# Goa.Clients.Core

Base functionality and abstractions for all Goa AWS service clients. This package provides the foundational components that other Goa client libraries build upon.

## Installation

```bash
dotnet add package Goa.Clients.Core
```

## Overview

Goa.Clients.Core provides:
- Base AWS client abstractions and implementations
- HTTP client factory integration
- Error handling patterns using ErrorOr
- Common AWS authentication and request signing
- Shared utilities for all AWS service clients

## Usage

This package is typically not used directly, but rather as a dependency of other Goa client packages. It provides the base classes and services that AWS service clients inherit from.

```csharp
using Goa.Clients.Core;
using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Example: Building a custom AWS service client
public class MyCustomAwsClient : AwsServiceClient<MyAwsServiceConfiguration>
{
    public MyCustomAwsClient(IHttpClientFactory httpClientFactory, ILogger<MyCustomAwsClient> logger, MyAwsServiceConfiguration configuration)
        : base(httpClientFactory, logger, configuration)
    {
    }

    // Your custom AWS service implementation
}

// Register static AWS credentials (optional)
services.AddStaticCredentials("your-access-key", "your-secret-key");
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).