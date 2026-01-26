using System.Text.Json;

namespace Goa.Clients.Bedrock.Mcp;

/// <summary>
/// Represents an MCP tool definition that can be converted to Bedrock format.
/// </summary>
public class McpToolDefinition
{
    /// <summary>
    /// The name of the tool.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// A description of what the tool does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The JSON schema that defines the tool's input parameters.
    /// </summary>
    public required JsonElement InputSchema { get; init; }

    /// <summary>
    /// The handler function that executes the tool.
    /// </summary>
    public required Func<JsonElement, CancellationToken, Task<JsonElement>> Handler { get; init; }
}
