# Serialization Contexts

In Goa, serialization contexts are used to to improve the first run performance of the JSON serialization via Source Generators.

## Logging

The default logging serialization context is defined as:

```csharp
[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(IDictionary<string, object>))]
[JsonSerializable(typeof(IDictionary<string, string>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
internal partial class LoggingSerializationContext : JsonSerializerContext;
```

If you are logging a type that is not defined here, you change the default serialization context for logging by overriding `ConfigureLoggingJsonSerializerContext` on a function, eg:

```csharp
protected override JsonSerializerContext ConfigureLoggingJsonSerializerContext() => LoggingSerializationContext.Default;
```