using System.Text;

namespace Goa.Clients.Dynamo.Generator.CodeGeneration;

/// <summary>
/// Utility for building code strings safely with proper indentation.
/// </summary>
public class CodeBuilder
{
    private readonly StringBuilder _builder = new();
    private int _indentLevel = 0;
    private const string IndentString = "    "; // 4 spaces
    
    public CodeBuilder AppendLine(string line = "")
    {
        if (string.IsNullOrEmpty(line))
        {
            _builder.AppendLine();
        }
        else
        {
            _builder.AppendLine(GetIndent() + line);
        }
        return this;
    }
    
    public CodeBuilder Append(string text)
    {
        _builder.Append(text);
        return this;
    }
    
    public CodeBuilder OpenBrace()
    {
        AppendLine("{");
        _indentLevel++;
        return this;
    }
    
    public CodeBuilder CloseBrace()
    {
        _indentLevel--;
        AppendLine("}");
        return this;
    }
    
    public CodeBuilder OpenBraceWithLine(string line)
    {
        AppendLine(line);
        return OpenBrace();
    }
    
    public CodeBuilder Indent()
    {
        _indentLevel++;
        return this;
    }
    
    public CodeBuilder Unindent()
    {
        _indentLevel = Math.Max(0, _indentLevel - 1);
        return this;
    }
    
    private string GetIndent()
    {
        return string.Concat(Enumerable.Repeat(IndentString, _indentLevel));
    }
    
    public override string ToString()
    {
        return _builder.ToString();
    }
    
    public void Clear()
    {
        _builder.Clear();
        _indentLevel = 0;
    }
}