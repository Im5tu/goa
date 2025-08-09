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

### AWS Service Clients
- **Goa.Clients.Core** - Base functionality for all AWS clients
- **Goa.Clients.Dynamo** - DynamoDB client with source generator support
- **Goa.Clients.EventBridge** - EventBridge client for event routing
- **Goa.Clients.Lambda** - Lambda client for function invocation
- **Goa.Clients.Sns** - SNS client for messaging
- **Goa.Clients.Sqs** - SQS client for queue operations

### Lambda Functions
- **Goa.Functions.Core** - Core runtime and bootstrapping functionality
- **Goa.Functions.ApiGateway** - API Gateway integration with V1/V2 payload support
- **Goa.Functions.Dynamo** - DynamoDB stream processing

### Tools
- **Goa.Templates** - Project templates for rapid development
- **Goa.Clients.Dynamo.Generator** - Source generator for DynamoDB models
