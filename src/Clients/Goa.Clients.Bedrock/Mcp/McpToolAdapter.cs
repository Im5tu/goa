using System.Collections.Frozen;
using System.Text.Json;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Mcp;

/// <summary>
/// Adapter interface for converting MCP tools to Bedrock format and executing them.
/// </summary>
public interface IMcpToolAdapter
{
    /// <summary>
    /// Converts MCP tool definitions to Bedrock Tool format.
    /// </summary>
    /// <param name="mcpTools">The MCP tool definitions to convert.</param>
    /// <returns>A read-only list of Bedrock Tool objects.</returns>
    IReadOnlyList<Tool> ToBedrockTools(IEnumerable<McpToolDefinition> mcpTools);

    /// <summary>
    /// Executes an MCP tool from a Bedrock ToolUseBlock.
    /// </summary>
    /// <param name="toolUse">The Bedrock tool use block containing the tool name and input.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A ToolResultBlock containing the execution result.</returns>
    Task<ToolResultBlock> ExecuteToolAsync(ToolUseBlock toolUse, CancellationToken ct = default);
}

/// <summary>
/// Adapter that converts MCP tool definitions to Bedrock Tool format and executes tool calls.
/// </summary>
public sealed class McpToolAdapter : IMcpToolAdapter
{
    private FrozenDictionary<string, McpToolDefinition> _tools = FrozenDictionary<string, McpToolDefinition>.Empty;

    /// <summary>
    /// Converts MCP tool definitions to Bedrock Tool format and registers them for execution.
    /// </summary>
    /// <param name="mcpTools">The MCP tool definitions to convert.</param>
    /// <returns>A read-only list of Bedrock Tool objects.</returns>
    public IReadOnlyList<Tool> ToBedrockTools(IEnumerable<McpToolDefinition> mcpTools)
    {
        ArgumentNullException.ThrowIfNull(mcpTools);

        var toolsList = mcpTools.ToList();
        var toolsDict = new Dictionary<string, McpToolDefinition>(toolsList.Count);
        var bedrockTools = new List<Tool>(toolsList.Count);

        foreach (var mcpTool in toolsList)
        {
            toolsDict[mcpTool.Name] = mcpTool;

            bedrockTools.Add(new Tool
            {
                ToolSpec = new ToolSpec
                {
                    Name = mcpTool.Name,
                    Description = mcpTool.Description,
                    InputSchema = new ToolInputSchema { Json = mcpTool.InputSchema }
                }
            });
        }

        _tools = toolsDict.ToFrozenDictionary();
        return bedrockTools;
    }

    /// <summary>
    /// Executes an MCP tool from a Bedrock ToolUseBlock.
    /// </summary>
    /// <param name="toolUse">The Bedrock tool use block containing the tool name and input.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A ToolResultBlock containing the execution result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when toolUse is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the tool is not found.</exception>
    public async Task<ToolResultBlock> ExecuteToolAsync(ToolUseBlock toolUse, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(toolUse);

        if (!_tools.TryGetValue(toolUse.Name, out var tool))
        {
            return new ToolResultBlock
            {
                ToolUseId = toolUse.ToolUseId,
                Status = "error",
                Content =
                [
                    new ContentBlock
                    {
                        Text = $"Tool '{toolUse.Name}' not found"
                    }
                ]
            };
        }

        try
        {
            var result = await tool.Handler(toolUse.Input, ct).ConfigureAwait(false);

            return new ToolResultBlock
            {
                ToolUseId = toolUse.ToolUseId,
                Status = "success",
                Content =
                [
                    new ContentBlock
                    {
                        Text = result.GetRawText()
                    }
                ]
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ToolResultBlock
            {
                ToolUseId = toolUse.ToolUseId,
                Status = "error",
                Content =
                [
                    new ContentBlock
                    {
                        Text = ex.Message
                    }
                ]
            };
        }
    }
}
