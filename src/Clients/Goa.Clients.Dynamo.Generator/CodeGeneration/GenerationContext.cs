using Goa.Clients.Dynamo.Generator.Models;
using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.CodeGeneration;

/// <summary>
/// Context information available during code generation.
/// </summary>
public class GenerationContext
{
    public Dictionary<string, string> AvailableConversions { get; set; } = new();
    public Dictionary<string, List<DynamoTypeInfo>> TypeRegistry { get; set; } = new();
    public Action<Diagnostic>? ReportDiagnostic { get; set; }
}