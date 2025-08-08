# Goa.Functions.ApiGateway

API Gateway integration with V1/V2 payload support for high-performance AWS Lambda functions. This package provides seamless integration with AWS API Gateway using ASP.NET Core patterns.

## Installation

```bash
dotnet add package Goa.Functions.ApiGateway
```

## Features

- Native AOT support for faster Lambda cold starts
- Support for both API Gateway V1 and V2 payload formats
- ASP.NET Core integration with familiar patterns
- Built-in JWT token validation
- Automatic request/response transformation
- Middleware support for cross-cutting concerns

## Usage

### Basic Setup

```csharp
using Goa.Functions.ApiGateway;

var builder = WebApplication.CreateBuilder(args);
builder.UseGoa();

var app = builder.Build();

app.MapGet("/users/{id}", (string id) => 
{
    return Results.Ok(new { Id = id, Name = "John Doe" });
});

app.Run();
```

### Program.cs (Minimal API)

```csharp
using Goa.Functions.ApiGateway;

var builder = WebApplication.CreateBuilder(args);
builder.UseGoa(ApiGatewayType.HttpV2);

var app = builder.Build();

// Configure your endpoints
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy" }));

app.MapPost("/users", (CreateUserRequest request) => 
{
    // Create user logic
    return Results.Created($"/users/{request.Id}", request);
});

app.MapGet("/users/{id}", (string id) => 
{
    // Get user logic
    return Results.Ok(new { Id = id, Name = "User Name" });
});

// Start the Lambda runtime
app.Run();
```

### With Controllers

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet("{id}")]
    public ActionResult<User> GetUser(string id)
    {
        // Your logic here
        return Ok(new User { Id = id, Name = "John Doe" });
    }
    
    [HttpPost]
    public ActionResult<User> CreateUser([FromBody] CreateUserRequest request)
    {
        // Your logic here
        return CreatedAtAction(nameof(GetUser), new { id = request.Id }, request);
    }
}
```

### JWT Authentication

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
builder.UseGoa();

// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-auth-provider.com";
        options.Audience = "your-api-audience";
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/protected", () => "Protected content")
    .RequireAuthorization();

app.Run();
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).