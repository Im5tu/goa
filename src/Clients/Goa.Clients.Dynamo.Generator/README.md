# DynamoDB Source Generator

A .NET source generator that automatically creates type-safe mapping code for DynamoDB operations using attributes to define table structure and indexes.

## Features

- **Automatic Code Generation**: Generates `DynamoKeyFactory` and `DynamoMapper` classes from attributed models
- **Type Safety**: Compile-time validation of attribute patterns and type conversions
- **Inheritance Support**: Works with abstract base classes and concrete implementations
- **Global Secondary Index Support**: Up to 5 GSIs per model with custom naming
- **Comprehensive Type Support**: Handles primitives, collections, enums, DateTime, and nullable types
- **Compile-Time Diagnostics**: Clear error messages for configuration issues

## Quick Start

1. **Define your model** with DynamoDB attributes:

```csharp
[DynamoModel(PK = "USER#<Id>", SK = "PROFILE#<Email>")]
[GlobalSecondaryIndex(Name = "EmailIndex", PK = "EMAIL#<Email>", SK = "USER#<Id>")]
public record UserProfile(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt,
    bool IsActive
);
```

2. **Generated code** will be automatically created:

```csharp
// DynamoKeyFactory.g.cs
public static class DynamoKeyFactory
{
    public static class UserProfile
    {
        public static string PK(string id) => $"USER#{id}";
        public static string SK(string email) => $"PROFILE#{email}";
        public static string GSI_EmailIndex_PK(string email) => $"EMAIL#{email}";
        public static string GSI_EmailIndex_SK(string id) => $"USER#{id}";
        public static string GSI_EmailIndex_IndexName() => "EmailIndex";
    }
}

// DynamoMapper.g.cs
public static class DynamoMapper
{
    public static class UserProfile
    {
        public static DynamoRecord ToDynamoRecord(UserProfile model) { /* ... */ }
        public static UserProfile FromDynamoRecord(DynamoRecord record) { /* ... */ }
    }
}
```

## Attributes

### DynamoModelAttribute

Defines the primary table structure:

```csharp
[DynamoModel(
    PK = "USER#<Id>",           // Partition key pattern
    SK = "PROFILE#<Email>",     // Sort key pattern
    PKName = "PK",              // Optional: Custom PK column name (default: "PK")
    SKName = "SK"               // Optional: Custom SK column name (default: "SK")
)]
```

### GlobalSecondaryIndexAttribute

Defines Global Secondary Indexes (up to 5 per model):

```csharp
[GlobalSecondaryIndex(
    Name = "EmailIndex",        // Required: GSI name
    PK = "EMAIL#<Email>",       // Required: GSI partition key pattern
    SK = "USER#<Id>",           // Required: GSI sort key pattern
    PKName = "GSI_1_PK",        // Optional: Custom GSI PK column name
    SKName = "GSI_1_SK"         // Optional: Custom GSI SK column name
)]
```

## Key Patterns

Use `<PropertyName>` placeholders in key patterns that will be replaced with actual property values:

- **String properties**: `"USER#<Id>"` → `"USER#123"`
- **DateTime properties**: `"DATE#<CreatedAt>"` → `"DATE#2023-12-01"` (yyyy-MM-dd format)
- **Other types**: Converted to string representation

## Inheritance Support

The generator supports abstract base classes:

```csharp
[DynamoModel(PK = "ENTITY#<Id>", SK = "METADATA")]
public abstract record BaseEntity(string Id, DateTime CreatedAt, bool IsActive);

// Concrete implementation inherits the DynamoModel configuration
public record UserProfile(
    string Id,
    DateTime CreatedAt,
    bool IsActive,
    string Email
) : BaseEntity(Id, CreatedAt, IsActive);
```

Generated code includes type switching for abstract types:

```csharp
public static BaseEntity FromDynamoRecord(DynamoRecord record)
{
    if (!record.TryGetNullableString("Type", out var typeValue) || typeValue == null)
        throw new InvalidOperationException("Type attribute is required for abstract type");

    return typeValue switch
    {
        "UserProfile" => DynamoMapper.UserProfile.FromDynamoRecord(record),
        _ => throw new InvalidOperationException($"Unknown type '{typeValue}'")
    };
}
```

## Supported Types

### Primitive Types

| Type | DynamoDB Type | Format (if applicable) | Nullable Support |
|------|---------------|------------------------|------------------|
| `string` | S | | Yes |
| `bool` | BOOL | | Yes |
| `byte` | N | | Yes |
| `sbyte` | N | | Yes |
| `char` | S | Single character | Yes |
| `short` | N | | Yes |
| `ushort` | N | | Yes |
| `int` | N | | Yes |
| `uint` | N | | Yes |
| `long` | N | | Yes |
| `ulong` | N | | Yes |
| `float` | N | | Yes |
| `double` | N | | Yes |
| `decimal` | N | | Yes |
| `DateTime` | S | ISO 8601 (o) | Yes |
| `DateTimeOffset` | S | ISO 8601 (o) | Yes |
| `TimeSpan` | S | .NET TimeSpan format | Yes |
| `Guid` | S | Standard GUID format | Yes |
| `Enum` | S | Enum name as string | Yes |

### Collection Types

| Collection Type | DynamoDB Type | Element Types Supported | Notes |
|-----------------|---------------|-------------------------|-------|
| `IEnumerable<T>` | SS/NS | All primitive types, enums | Returns as enumerable |
| `List<T>` | SS/NS | All primitive types, enums | Converts to `List<T>` |
| `IList<T>` | SS/NS | All primitive types, enums | Returns as `List<T>` |
| `ICollection<T>` | SS/NS | All primitive types, enums | Returns as `List<T>` |
| `Collection<T>` | SS/NS | All primitive types, enums | Returns as `Collection<T>` |
| `ISet<T>` | SS/NS | All primitive types, enums | Returns as `HashSet<T>` |
| `HashSet<T>` | SS/NS | All primitive types, enums | Returns as `HashSet<T>` |
| `IReadOnlyCollection<T>` | SS/NS | All primitive types, enums | Returns as `List<T>` |
| `IReadOnlyList<T>` | SS/NS | All primitive types, enums | Returns as `List<T>` |
| `IReadOnlySet<T>` | SS/NS | All primitive types, enums | Returns as `HashSet<T>` |
| `T[]` | SS/NS | All primitive types, enums | Converts to array |

### Special Collection Handling

| Element Type | Storage Method | Conversion Notes |
|--------------|----------------|------------------|
| `string` | SS (String Set) | Direct storage |
| Numeric types | NS (Number Set) | Converted to string representation |
| `DateTime` | SS (String Set) | ISO 8601 format |
| `DateTimeOffset` | SS (String Set) | ISO 8601 format |
| `TimeSpan` | SS (String Set) | .NET TimeSpan format |
| `Guid` | SS (String Set) | Standard GUID format |
| Enums | SS (String Set) | Enum name as string |

### Dictionary Types

| Dictionary Type | DynamoDB Type | Supported Key/Value Types | Nullable Support |
|-----------------|---------------|---------------------------|------------------|
| `IDictionary<string, string>` | M (Map) | String keys, String values | Yes |
| `Dictionary<string, string>` | M (Map) | String keys, String values | Yes |
| `IReadOnlyDictionary<string, string>` | M (Map) | String keys, String values | Yes |
| `IDictionary<string, int>` | M (Map) | String keys, Int values | Yes |
| `Dictionary<string, int>` | M (Map) | String keys, Int values | Yes |
| `IReadOnlyDictionary<string, int>` | M (Map) | String keys, Int values | Yes |

**Supported Dictionary Types**:
- Dictionary types with **string keys** and **string values** are stored as DynamoDB Maps (M type)
- Dictionary types with **string keys** and **int values** are stored as DynamoDB Maps with numeric values
- Non-nullable dictionaries default to empty dictionaries when attribute is missing
- Nullable dictionaries default to `null` when attribute is missing

**Extension Methods**:
- `TryGetStringDictionary` / `TryGetNullableStringDictionary` for `Dictionary<string, string>`
- `TryGetStringIntDictionary` / `TryGetNullableStringIntDictionary` for `Dictionary<string, int>`

**Unsupported Dictionary Types**:
- Dictionaries with non-string keys (e.g., `Dictionary<int, string>`)
- Dictionaries with complex value types (e.g., `Dictionary<string, CustomClass>`)
- These will receive appropriate default values based on nullability but won't be serialized

### Nullability Handling

The generator properly handles nullable reference types and nullable value types:

#### Collection Nullability
- **Nullable collections** (e.g., `List<string>?`): Default to `null` when attribute is missing
- **Non-nullable collections** (e.g., `List<string>`): Default to appropriate empty collection when attribute is missing

#### Examples
```csharp
public record CollectionExample(
    List<string> RequiredTags,        // Defaults to new List<string>()
    List<string>? OptionalTags,       // Defaults to null
    string[] RequiredCategories,      // Defaults to Array.Empty<string>()
    string[]? OptionalCategories,     // Defaults to null
    Dictionary<string, string> Config, // Defaults to new Dictionary<string, string>() - fully serialized
    Dictionary<string, string>? Meta,  // Defaults to null - fully serialized when present
    Dictionary<string, int> Counters   // Defaults to new Dictionary<string, int>() - fully serialized
);
```

### Unsupported Types

The following types are currently unsupported and will receive appropriate default handling:

- **Nested collections**: `IEnumerable<IEnumerable<T>>`, `List<List<T>>`, etc. (defaults based on nullability)
- **Complex objects**: Custom classes/records as collection elements
- **Nullable collections of nullable types**: e.g., `List<int?>?`

## Extension Methods

The generator works with type-safe extension methods for value extraction:

```csharp
// Non-nullable extraction
if (record.TryGetString("Name", out var name)) { /* ... */ }
if (record.TryGetInt("Age", out var age)) { /* ... */ }

// Nullable extraction
if (record.TryGetNullableString("Description", out var description)) { /* ... */ }
if (record.TryGetNullableDateTime("UpdatedAt", out var updatedAt)) { /* ... */ }
```

## Compile-Time Diagnostics

The generator provides helpful error messages:

- **DYNAMO001**: Too many GlobalSecondaryIndex attributes (maximum 5)
- **DYNAMO002**: Missing PK property in DynamoModelAttribute
- **DYNAMO003**: Missing SK property in DynamoModelAttribute
- **DYNAMO004**: Missing Name property in GlobalSecondaryIndexAttribute
- **DYNAMO005**: Property not found in placeholder - occurs when a placeholder like `<PropertyName>` in PK/SK patterns references a property that doesn't exist on the model or any of its base classes

## Usage Examples

### Creating Records
```csharp
var user = new UserProfile("123", "john@example.com", "John", "Doe", DateTime.Now, true);
var dynamoRecord = DynamoMapper.UserProfile.ToDynamoRecord(user);
```

### Querying with Keys
```csharp
var pk = DynamoKeyFactory.UserProfile.PK("123");
var sk = DynamoKeyFactory.UserProfile.SK("john@example.com");

// GSI queries
var gsiPK = DynamoKeyFactory.UserProfile.GSI_EmailIndex_PK("john@example.com");
var indexName = DynamoKeyFactory.UserProfile.GSI_EmailIndex_IndexName();
```

### Converting from DynamoDB
```csharp
DynamoRecord record = /* from DynamoDB response */;
var user = DynamoMapper.UserProfile.FromDynamoRecord(record);
```
