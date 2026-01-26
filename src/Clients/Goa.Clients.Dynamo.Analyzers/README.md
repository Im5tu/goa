# Goa.Clients.Dynamo.Analyzers

Roslyn analyzers and code fixes for the Goa DynamoDB client. These analyzers help enforce best practices and cleaner code patterns when working with DynamoDB models.

## Features

- Detects opportunities to use extension methods instead of static mapper calls
- Automatic code fixes for one-click refactoring
- Supports batch fixing across entire solutions
- Configurable behavior via MSBuild properties

## Quick Start

This package is automatically included when you install `Goa.Clients.Dynamo`. No additional setup is required.

If you need to install it separately:

```bash
dotnet add package Goa.Clients.Dynamo.Analyzers
```

## Diagnostics

### GOA001: Use extension method instead of DynamoMapper

| Property | Value |
|----------|-------|
| **ID** | GOA001 |
| **Category** | Usage |
| **Severity** | Info |
| **Enabled** | Yes (default) |

#### Description

When the `ToDynamoRecord()` extension method is available, prefer using it over the static `DynamoMapper` method for cleaner code.

#### Before

```csharp
var record = DynamoMapper.User.ToDynamoRecord(user);
```

#### After

```csharp
var record = user.ToDynamoRecord();
```

#### Trigger Conditions

This diagnostic is reported when all of the following conditions are met:

1. Code calls `DynamoMapper.X.ToDynamoRecord(model)` with exactly one argument
2. The model type has an available extension method, determined by either:
   - The type has the `[Extension]` attribute applied directly
   - The `GoaAutoGenerateExtensions` property is enabled AND the type (or a base type) has the `[DynamoModel]` attribute

## Configuring Extension Generation

Control whether extensions are automatically generated for all `[DynamoModel]` types using the `GoaAutoGenerateExtensions` MSBuild property.

In your `.csproj` file:

```xml
<PropertyGroup>
  <!-- Enable auto-generation of extensions for all [DynamoModel] types -->
  <GoaAutoGenerateExtensions>true</GoaAutoGenerateExtensions>
</PropertyGroup>
```

When enabled:
- The analyzer will suggest using extension methods for any type with `[DynamoModel]` (including inherited)
- No explicit `[Extension]` attribute is required on individual types

When disabled (default):
- The analyzer only suggests extensions for types with the explicit `[Extension]` attribute

## Code Fix Capabilities

The included code fix provider offers:

- **Single fix**: Apply the fix to an individual diagnostic
- **Fix all in document**: Apply to all occurrences in the current file
- **Fix all in project**: Apply to all occurrences in the current project
- **Fix all in solution**: Apply to all occurrences across the entire solution

The code fix transforms `DynamoMapper.X.ToDynamoRecord(model)` into `model.ToDynamoRecord()`, preserving any leading and trailing trivia (whitespace, comments).

## Examples

### Basic Usage

```csharp
[DynamoModel(PK = "USER#<Id>", SK = "PROFILE")]
[Extension]
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
}

// Before (triggers GOA001)
var record = DynamoMapper.User.ToDynamoRecord(user);

// After (preferred)
var record = user.ToDynamoRecord();
```

### Inheritance Scenarios

When `GoaAutoGenerateExtensions` is enabled, the analyzer recognizes inherited `[DynamoModel]` attributes:

```csharp
[DynamoModel(PK = "ENTITY#<Id>", SK = "BASE")]
public abstract class BaseEntity
{
    public string Id { get; set; }
}

// No [DynamoModel] attribute needed - inherits from BaseEntity
public class Customer : BaseEntity
{
    public string Email { get; set; }
}

// With GoaAutoGenerateExtensions=true, this triggers GOA001
var record = DynamoMapper.Customer.ToDynamoRecord(customer);

// Preferred
var record = customer.ToDynamoRecord();
```

### Explicit Extension Attribute Usage

Use the `[Extension]` attribute when you want to enable the extension method without `GoaAutoGenerateExtensions`:

```csharp
[DynamoModel(PK = "ORDER#<Id>", SK = "DETAILS")]
[Extension]  // Explicitly enables extension method
public class Order
{
    public string Id { get; set; }
    public decimal Total { get; set; }
}

// Triggers GOA001 regardless of GoaAutoGenerateExtensions setting
var record = DynamoMapper.Order.ToDynamoRecord(order);
```

## Suppressing Diagnostics

### Inline Suppression

```csharp
#pragma warning disable GOA001
var record = DynamoMapper.User.ToDynamoRecord(user);
#pragma warning restore GOA001
```

### Attribute-Based Suppression

```csharp
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "GOA001")]
public void ProcessUser(User user)
{
    var record = DynamoMapper.User.ToDynamoRecord(user);
}
```

### Project-Wide Suppression

In your `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.GOA001.severity = none
```

Or in your `.csproj`:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);GOA001</NoWarn>
</PropertyGroup>
```

## Documentation

For more information about the DynamoDB client, see the [Goa.Clients.Dynamo README](../Goa.Clients.Dynamo/README.md).

For the full Goa documentation, visit the [main repository](https://github.com/im5tu/goa).
