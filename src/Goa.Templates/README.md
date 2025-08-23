# Goa AWS Lambda Templates

Project templates for creating AWS Lambda functions using the Goa framework with built-in support for high-performance, AOT-compiled .NET applications.

## Available Templates

### API Gateway Lambda (`goa.apigw`)
Creates a Lambda function for API Gateway integration using ASP.NET Core.

**Usage:**
```bash
dotnet new goa.apigw -n MyApiFunction
```

**Options:**
- `--functionType` - Choose API Gateway type:
  - `httpv2` (default) - HTTP API with V2 payload format
  - `httpv1` - HTTP API with V1 payload format  
  - `restapi` - REST API
- `--includeOpenApi` - Include OpenAPI documentation with Scalar UI (default: false)

**Features:**
- ASP.NET Core routing and endpoints
- JSON source generation for optimal performance
- AOT compilation ready
- Sample `/ping` endpoint included

### DynamoDB Lambda (`goa.dynamodb`)
Creates a Lambda function for processing DynamoDB streams.

**Usage:**
```bash
dotnet new goa.dynamodb -n MyDynamoFunction
```

**Features:**
- Batch processing of DynamoDB stream records
- Dependency injection support
- Record failure handling
- AOT compilation ready

### S3 Lambda (`goa.s3`)
Creates a Lambda function for processing S3 events.

**Usage:**
```bash
dotnet new goa.s3 -n MyS3Function
```

**Options:**
- `--processingType` - Choose processing mode:
  - `batch` (default) - Process S3 events as a batch
  - `single` - Process S3 events one at a time

**Features:**
- Single or batch processing of S3 events
- Support for all S3 event types (ObjectCreated, ObjectRemoved, etc.)
- Event failure handling
- Dependency injection support
- AOT compilation ready

## Getting Started

1. Install the template package:
   ```bash
   dotnet new install Goa.Templates
   ```

2. Create a new function:
   ```bash
   # Basic API Gateway function
   dotnet new goa.apigw -n MyFunction
   
   # With OpenAPI documentation
   dotnet new goa.apigw -n MyFunction --includeOpenApi true
   
   # DynamoDB stream function
   dotnet new goa.dynamodb -n MyDynamoFunction
   
   # S3 event function (batch processing)
   dotnet new goa.s3 -n MyS3Function
   
   # S3 event function (single processing)
   dotnet new goa.s3 -n MyS3Function --processingType single
   
   cd MyFunction
   ```

3. Build and deploy:
   ```bash
   dotnet publish -c Release
   # Deploy using your preferred method (SAM, CDK, etc.)
   ```

## Requirements

- .NET 10.0 SDK or later
- AWS CLI configured for deployment

## Learn More

Visit the [Goa documentation](https://github.com/im5tu/goa) for more information about the framework and deployment options.
