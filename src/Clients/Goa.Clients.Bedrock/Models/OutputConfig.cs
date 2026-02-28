namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// Configuration for the output format of a Converse request.
/// </summary>
public class OutputConfig
{
    /// <summary>
    /// The text format configuration for structured output.
    /// </summary>
    public OutputFormat? TextFormat { get; set; }
}

/// <summary>
/// The text output format specification. Set Type to "json_schema" and provide Structure for JSON schema output.
/// </summary>
public class OutputFormat
{
    /// <summary>
    /// The type of output format. Use "json_schema" for structured output.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The structure definition when Type is "json_schema".
    /// </summary>
    public OutputFormatStructure? Structure { get; set; }
}

/// <summary>
/// The structure definition for a structured output format. This is a union type - only one member should be set.
/// </summary>
public class OutputFormatStructure
{
    /// <summary>
    /// The JSON schema definition for structured output.
    /// </summary>
    public JsonSchemaDefinition? JsonSchema { get; set; }
}

/// <summary>
/// Defines a JSON schema that the model's output must adhere to.
/// </summary>
public class JsonSchemaDefinition
{
    /// <summary>
    /// The JSON schema as a JSON-stringified string. The Converse API requires this as a string, not a JSON object.
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// A name identifier for this schema.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// An optional description of what this schema represents.
    /// </summary>
    public string? Description { get; set; }
}
