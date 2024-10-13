using System.Text.Json.Serialization;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;

namespace TestConsole;

[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Data))]
public partial class CustomSerializationContext : JsonSerializerContext
{
}

public class Function : FunctionBase<Data, Data>
{
    protected override Task<Data> HandleRequestAsync(IServiceProvider services, Data request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request);
    }
}

public class Data
{
    public string Payload { get; set; }
}


// Source Generated
public class Entrypoint
{
    public static async Task Main(string[] args)
    {
        await new LambdaBootstrapper<Function, Data, Data>(CustomSerializationContext.Default).RunAsync();
    }
}
