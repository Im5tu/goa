using System.Text.Json;

namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// A tool definition that the model can use.
/// </summary>
public class Tool
{
    /// <summary>
    /// The specification of the tool.
    /// </summary>
    public ToolSpec ToolSpec { get; set; } = new();
}

/// <summary>
/// The specification of a tool including its name, description, and input schema.
/// </summary>
public class ToolSpec
{
    /// <summary>
    /// The name of the tool.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what the tool does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The input schema that defines the tool's input parameters.
    /// </summary>
    public ToolInputSchema InputSchema { get; set; } = new();
}

/// <summary>
/// The input schema for a tool. This is a union type - only the Json member should be set.
/// </summary>
public class ToolInputSchema
{
    /// <summary>
    /// The JSON schema for the tool's input parameters.
    /// </summary>
    public JsonElement Json { get; set; }
}
