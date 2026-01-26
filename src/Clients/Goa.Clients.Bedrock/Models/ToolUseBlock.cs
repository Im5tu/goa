using System.Text.Json;

namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// A tool use block indicating the model wants to invoke a tool.
/// </summary>
public class ToolUseBlock
{
    /// <summary>
    /// A unique identifier for this tool use request.
    /// </summary>
    public string ToolUseId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the tool to use.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The input parameters for the tool as a JSON element.
    /// </summary>
    public JsonElement Input { get; set; }
}
