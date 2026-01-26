namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// Configuration for tool use in a conversation.
/// </summary>
public class ToolConfiguration
{
    /// <summary>
    /// The list of tools available to the model.
    /// </summary>
    public List<Tool> Tools { get; set; } = new();

    /// <summary>
    /// Configuration for how the model should choose which tool to use.
    /// </summary>
    public ToolChoice? ToolChoice { get; set; }
}

/// <summary>
/// Configuration for how the model should choose which tool to use.
/// </summary>
public class ToolChoice
{
    /// <summary>
    /// If set, the model will automatically decide whether to use a tool.
    /// </summary>
    public AutoToolChoice? Auto { get; set; }

    /// <summary>
    /// If set, the model will be forced to use any available tool.
    /// </summary>
    public AnyToolChoice? Any { get; set; }

    /// <summary>
    /// If set, the model will be forced to use a specific tool.
    /// </summary>
    public SpecificToolChoice? Tool { get; set; }
}

/// <summary>
/// Marker class indicating automatic tool choice.
/// </summary>
public class AutoToolChoice { }

/// <summary>
/// Marker class indicating any tool can be chosen.
/// </summary>
public class AnyToolChoice { }

/// <summary>
/// Configuration to force the model to use a specific tool.
/// </summary>
public class SpecificToolChoice
{
    /// <summary>
    /// The name of the tool the model must use.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
