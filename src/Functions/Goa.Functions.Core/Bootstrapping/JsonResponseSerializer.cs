using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Goa.Functions.Core.Bootstrapping;

internal sealed class JsonResponseSerializer<T> : IResponseSerializer<T>
{
    private static readonly string JsonContentType = "application/json";
    private readonly JsonTypeInfo<T> _typeInfo;

    public JsonResponseSerializer(JsonSerializerContext jsonSerializerContext)
    {
        _typeInfo = jsonSerializerContext.GetTypeInfo(typeof(T)) as JsonTypeInfo<T> ?? throw new ArgumentException(nameof(jsonSerializerContext), $"Type {typeof(T)} is not registered properly in the serialization context");
    }

    public HttpContent Serialize(T response)
    {
        var json = JsonSerializer.Serialize(response, _typeInfo);
        return new StringContent(json, Encoding.UTF8, JsonContentType);
    }
}
