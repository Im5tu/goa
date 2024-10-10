using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Goa.Functions.ApiGateway.Generator;

[Generator]
public class HelloSourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        // Generate a simple class
        string sourceCode = @"
        namespace GeneratedNamespace
        {
            public static class GeneratedClass
            {
                public static string HelloWorld() => ""Hello, World!"";
            }
        }";

        // Add the generated source code as a file
        context.AddSource("GeneratedClass.g.cs", SourceText.From(sourceCode, Encoding.UTF8));
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization required for this one
    }
}