# Goa

A high-performance, minimal-overhead .NET framework for building AWS Lambda functions with native AOT support. Goa provides lightweight clients for AWS services and streamlined function development patterns.

## Why Goa?

The AWS SDK for .NET comes with significant overhead that impacts Lambda function performance. Goa addresses these key issues:

- **Modern .NET**: Built with HttpClientFactory, native AOT support, dependency injection, and source generators
- **Performance**: Reduces memory allocations and cold start times through minimal dependencies
- **Security**: Goa only provides only essential operations, preventing accidental misuse

## Features

- **Native AOT Ready**: All libraries support native ahead-of-time compilation for faster cold starts
- **Minimal Dependencies**: Lean implementations with only essential dependencies
- **Type-Safe**: Full C# type safety with source generators for enhanced development experience
- **Multi-Target**: Supports .NET 9.0 and .NET 10.0

## Getting Started

The fastest way to get started is using the Goa project templates:

### 1. Install the Templates

```bash
dotnet new install Goa.Templates
```

### 2. Create a New Project

Create an API Gateway Lambda function:
```bash
dotnet new goa.apigw -n "MyFirstGoaProject"
cd MyFirstGoaProject
```

Or create a DynamoDB stream processing function:
```bash
dotnet new goa.dynamodb -n "MyDynamoFunction"
cd MyDynamoFunction
```

### 3. Build and Deploy

```bash
# Build the project
dotnet build

# Deploy using your preferred method (AWS SAM, CDK, etc.)
```

## Components

| Name | Description | NuGet | Docs |
|------|-------------|-------|------|
| **Goa.Core** | Core utilities and extensions | [![NuGet](https://img.shields.io/nuget/v/Goa.Core.svg)](https://nuget.org/packages/Goa.Core) | [📖](src/Goa.Core/README.md) |
| **Goa.Functions.Core** | Core runtime and bootstrapping functionality | [![NuGet](https://img.shields.io/nuget/v/Goa.Functions.Core.svg)](https://nuget.org/packages/Goa.Functions.Core) | [📖](src/Functions/Goa.Functions.Core/README.md) |
| **Goa.Functions.ApiGateway** | API Gateway integration with V1/V2 payload support | [![NuGet](https://img.shields.io/nuget/v/Goa.Functions.ApiGateway.svg)](https://nuget.org/packages/Goa.Functions.ApiGateway) | [📖](src/Functions/Goa.Functions.ApiGateway/README.md) |
| **Goa.Functions.ApiGateway.Authorizer** | Custom authorizer support for API Gateway | [![NuGet](https://img.shields.io/nuget/v/Goa.Functions.ApiGateway.Authorizer.svg)](https://nuget.org/packages/Goa.Functions.ApiGateway.Authorizer) | [📖](src/Functions/Goa.Functions.ApiGateway.Authorizer/README.md) |
| **Goa.Functions.Dynamo** | DynamoDB stream processing | [![NuGet](https://img.shields.io/nuget/v/Goa.Functions.Dynamo.svg)](https://nuget.org/packages/Goa.Functions.Dynamo) | [📖](src/Functions/Goa.Functions.Dynamo/README.md) |
| **Goa.Functions.EventBridge** | EventBridge event processing | [![NuGet](https://img.shields.io/nuget/v/Goa.Functions.EventBridge.svg)](https://nuget.org/packages/Goa.Functions.EventBridge) | [📖](src/Functions/Goa.Functions.EventBridge/README.md) |
| **Goa.Functions.Kinesis** | Kinesis stream processing | [![NuGet](https://img.shields.io/nuget/v/Goa.Functions.Kinesis.svg)](https://nuget.org/packages/Goa.Functions.Kinesis) | [📖](src/Functions/Goa.Functions.Kinesis/README.md) |
| **Goa.Functions.S3** | S3 event processing | [![NuGet](https://img.shields.io/nuget/v/Goa.Functions.S3.svg)](https://nuget.org/packages/Goa.Functions.S3) | [📖](src/Functions/Goa.Functions.S3/README.md) |
| **Goa.Functions.Sqs** | SQS message processing | [![NuGet](https://img.shields.io/nuget/v/Goa.Functions.Sqs.svg)](https://nuget.org/packages/Goa.Functions.Sqs) | [📖](src/Functions/Goa.Functions.Sqs/README.md) |
| **Goa.Clients.Core** | Base functionality for all AWS clients | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.Core.svg)](https://nuget.org/packages/Goa.Clients.Core) | [📖](src/Clients/Goa.Clients.Core/README.md) |
| **Goa.Clients.Dynamo** | DynamoDB client with source generator support | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.Dynamo.svg)](https://nuget.org/packages/Goa.Clients.Dynamo) | [📖](src/Clients/Goa.Clients.Dynamo/README.md) |
| **Goa.Clients.Dynamo.Generator** | Source generator for DynamoDB models | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.Dynamo.Generator.svg)](https://nuget.org/packages/Goa.Clients.Dynamo.Generator) | [📖](src/Clients/Goa.Clients.Dynamo.Generator/README.md) |
| **Goa.Clients.EventBridge** | EventBridge client for event routing | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.EventBridge.svg)](https://nuget.org/packages/Goa.Clients.EventBridge) | [📖](src/Clients/Goa.Clients.EventBridge/README.md) |
| **Goa.Clients.Kinesis** | Kinesis client for stream operations | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.Kinesis.svg)](https://nuget.org/packages/Goa.Clients.Kinesis) | [📖](src/Clients/Goa.Clients.Kinesis/README.md) |
| **Goa.Clients.Lambda** | Lambda client for function invocation | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.Lambda.svg)](https://nuget.org/packages/Goa.Clients.Lambda) | [📖](src/Clients/Goa.Clients.Lambda/README.md) |
| **Goa.Clients.ParameterStore** | Systems Manager Parameter Store client | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.ParameterStore.svg)](https://nuget.org/packages/Goa.Clients.ParameterStore) | [📖](src/Clients/Goa.Clients.ParameterStore/README.md) |
| **Goa.Clients.S3** | S3 client for object storage operations | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.S3.svg)](https://nuget.org/packages/Goa.Clients.S3) | [📖](src/Clients/Goa.Clients.S3/README.md) |
| **Goa.Clients.SecretsManager** | Secrets Manager client for secure storage | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.SecretsManager.svg)](https://nuget.org/packages/Goa.Clients.SecretsManager) | [📖](src/Clients/Goa.Clients.SecretsManager/README.md) |
| **Goa.Clients.Ses** | Simple Email Service client | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.Ses.svg)](https://nuget.org/packages/Goa.Clients.Ses) | [📖](src/Clients/Goa.Clients.Ses/README.md) |
| **Goa.Clients.Sns** | SNS client for messaging | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.Sns.svg)](https://nuget.org/packages/Goa.Clients.Sns) | [📖](src/Clients/Goa.Clients.Sns/README.md) |
| **Goa.Clients.Sqs** | SQS client for queue operations | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.Sqs.svg)](https://nuget.org/packages/Goa.Clients.Sqs) | [📖](src/Clients/Goa.Clients.Sqs/README.md) |
| **Goa.Clients.StepFunctions** | Step Functions client for workflow orchestration | [![NuGet](https://img.shields.io/nuget/v/Goa.Clients.StepFunctions.svg)](https://nuget.org/packages/Goa.Clients.StepFunctions) | [📖](src/Clients/Goa.Clients.StepFunctions/README.md) |
| **Goa.Templates** | Project templates for rapid development | [![NuGet](https://img.shields.io/nuget/v/Goa.Templates.svg)](https://nuget.org/packages/Goa.Templates) | [📖](src/Goa.Templates/README.md) |
