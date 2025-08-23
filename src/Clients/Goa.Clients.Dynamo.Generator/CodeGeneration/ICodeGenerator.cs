using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.CodeGeneration;

/// <summary>
/// Interface for generating specific parts of the DynamoDB mapper code.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Generates code for the given types and returns the source code.
    /// </summary>
    string GenerateCode(IEnumerable<DynamoTypeInfo> types, GenerationContext context);
}

/// <summary>
/// Context information available during code generation.
/// </summary>
public class GenerationContext
{
    public Dictionary<string, string> AvailableConversions { get; set; } = new();
    public Dictionary<string, List<DynamoTypeInfo>> TypeRegistry { get; set; } = new();
    public Action<Diagnostic>? ReportDiagnostic { get; set; }
}