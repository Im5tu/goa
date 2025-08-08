# Goa.Clients.Dynamo

A high-performance DynamoDB client optimized for AWS Lambda functions. This package provides a lightweight, AOT-ready DynamoDB client with strongly-typed operations and comprehensive error handling using the ErrorOr pattern.

## Installation

```bash
dotnet add package Goa.Clients.Dynamo
```

## Features

- Native AOT support for faster Lambda cold starts
- Strongly-typed request/response objects
- Minimal dependencies and memory allocations
- Built-in error handling with ErrorOr pattern
- Support for all DynamoDB operations (Get, Put, Update, Delete, Query, Scan, Batch, Transactions)
- DynamoDB model attributes for structured data access

## Usage

### Basic Setup

```csharp
using Goa.Clients.Dynamo;
using Microsoft.Extensions.DependencyInjection;

// Register DynamoDB client
services.AddDynamoDB();

// Or with custom configuration
services.AddDynamoDB(config =>
{
    config.ServiceUrl = "http://localhost:8000"; // For local DynamoDB
    config.Region = "us-west-2";
    config.LogLevel = LogLevel.Debug;
});
```

### Model Definition with Attributes

```csharp
[DynamoModel(PK = "USER#<Id>", SK = "PROFILE")]
[GlobalSecondaryIndex(Name = "EmailIndex", PK = "EMAIL#<Email>", SK = "USER")]
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### Basic Operations

```csharp
using ErrorOr;
using Goa.Clients.Dynamo;
using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.PutItem;

public class UserService
{
    private readonly IDynamoClient _client;
    
    public UserService(IDynamoClient client)
    {
        _client = client;
    }
    
    public async Task<ErrorOr<User>> GetUserAsync(string userId)
    {
        var result = await _client.GetItemAsync("Users", builder =>
        {
            builder
                .WithKey("PK", $"USER#{userId}")
                .WithKey("SK", "PROFILE");
        });
        
        if (result.IsError)
            return result.FirstError;
            
        // Convert DynamoRecord to User object
        if (result.Value.Item == null)
            return Error.NotFound("User.NotFound", "User not found");
            
        return ConvertToUser(result.Value.Item);
    }
    
    public async Task<ErrorOr<Success>> SaveUserAsync(User user)
    {
        var result = await _client.PutItemAsync("Users", builder =>
        {
            builder
                .WithAttribute("PK", $"USER#{user.Id}")
                .WithAttribute("SK", "PROFILE")
                .WithAttribute("Name", user.Name)
                .WithAttribute("Email", user.Email);
        });
            
        return result.IsError ? result.FirstError : Result.Success;
    }
}
```

### Query Operations

```csharp
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Models;

public async Task<ErrorOr<List<User>>> GetUsersByEmailAsync(string email)
{
    var users = await _client.QueryAllAsync("Users", builder =>
    {
        builder
            .WithIndex("EmailIndex")
            .WithKey(Condition.Equals("GSI_1_PK", $"EMAIL#{email}"));
    }).ToListAsync();
    
    return users.Select(ConvertToUser).ToList();
}
```

## Available Operations

- **GetItem**: Retrieve a single item by primary key
- **PutItem**: Create or replace an item
- **UpdateItem**: Modify specific attributes of an item
- **DeleteItem**: Remove an item from the table
- **Query**: Find items using primary key and optional filters
- **Scan**: Read all items in a table with optional filters
- **BatchGetItem**: Retrieve multiple items in a single request
- **BatchWriteItem**: Put or delete multiple items in a single request
- **TransactWriteItems**: Execute multiple write operations atomically
- **TransactGetItems**: Retrieve multiple items atomically

## Error Handling

All operations return `ErrorOr<T>` results, providing comprehensive error handling:

```csharp
var result = await _client.GetItemAsync(request);

if (result.IsError)
{
    // Handle errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error.Description}");
    }
    return;
}

// Use successful result
var item = result.Value.Item;
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).