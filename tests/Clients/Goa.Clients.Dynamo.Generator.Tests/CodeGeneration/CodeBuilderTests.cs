using Goa.Clients.Dynamo.Generator.CodeGeneration;

namespace Goa.Clients.Dynamo.Generator.Tests.CodeGeneration;

public class CodeBuilderTests
{
    [Test]
    public async Task AppendLine_WithEmptyString_ShouldAddNewLine()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.AppendLine("");
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo(Environment.NewLine);
    }
    
    [Test]
    public async Task AppendLine_WithNoParameters_ShouldAddNewLine()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.AppendLine();
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo(Environment.NewLine);
    }
    
    [Test]
    public async Task AppendLine_WithText_ShouldAddIndentedLine()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.AppendLine("public class Test");
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo("public class Test" + Environment.NewLine);
    }
    
    [Test]
    public async Task Append_WithText_ShouldAddTextWithoutNewLine()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.Append("test");
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo("test");
    }
    
    [Test]
    public async Task OpenBrace_ShouldAddBraceAndIncreaseIndent()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.OpenBrace();
        builder.AppendLine("content");
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo("{" + Environment.NewLine + "    content" + Environment.NewLine);
    }
    
    [Test]
    public async Task CloseBrace_ShouldDecreaseIndentAndAddBrace()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.OpenBrace();
        builder.AppendLine("content");
        builder.CloseBrace();
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo("{" + Environment.NewLine + "    content" + Environment.NewLine + "}" + Environment.NewLine);
    }
    
    [Test]
    public async Task OpenBraceWithLine_ShouldAddLineAndOpenBrace()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.OpenBraceWithLine("public class Test");
        builder.AppendLine("public void Method() { }");
        builder.CloseBrace();
        var result = builder.ToString();
        
        // Assert
        var expected = "public class Test" + Environment.NewLine +
                      "{" + Environment.NewLine +
                      "    public void Method() { }" + Environment.NewLine +
                      "}" + Environment.NewLine;
        await Assert.That(result)
            .IsEqualTo(expected);
    }
    
    [Test]
    public async Task Indent_ShouldIncreaseIndentationLevel()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.Indent();
        builder.AppendLine("indented content");
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo("    indented content" + Environment.NewLine);
    }
    
    [Test]
    public async Task Unindent_ShouldDecreaseIndentationLevel()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.Indent();
        builder.Indent();
        builder.AppendLine("double indented");
        builder.Unindent();
        builder.AppendLine("single indented");
        var result = builder.ToString();
        
        // Assert
        var expected = "        double indented" + Environment.NewLine +
                      "    single indented" + Environment.NewLine;
        await Assert.That(result)
            .IsEqualTo(expected);
    }
    
    [Test]
    public async Task Unindent_BelowZero_ShouldStayAtZero()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.Unindent(); // Should not go below 0
        builder.Unindent(); // Should not go below 0
        builder.AppendLine("no indent");
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo("no indent" + Environment.NewLine);
    }
    
    [Test]
    public async Task MultipleIndentLevels_ShouldProduceCorrectIndentation()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.AppendLine("level 0");
        builder.Indent();
        builder.AppendLine("level 1");
        builder.Indent();
        builder.AppendLine("level 2");
        builder.Indent();
        builder.AppendLine("level 3");
        var result = builder.ToString();
        
        // Assert
        var expected = "level 0" + Environment.NewLine +
                      "    level 1" + Environment.NewLine +
                      "        level 2" + Environment.NewLine +
                      "            level 3" + Environment.NewLine;
        await Assert.That(result)
            .IsEqualTo(expected);
    }
    
    [Test]
    public async Task Clear_ShouldResetBuilderAndIndentation()
    {
        // Arrange
        var builder = new CodeBuilder();
        builder.Indent();
        builder.AppendLine("some content");
        
        // Act
        builder.Clear();
        builder.AppendLine("new content");
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo("new content" + Environment.NewLine);
    }
    
    [Test]
    public async Task FluentInterface_ShouldAllowChaining()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        var result = builder
            .AppendLine("namespace Test")
            .OpenBrace()
            .AppendLine("public class Example")
            .OpenBrace()
            .AppendLine("public void Method()")
            .OpenBrace()
            .AppendLine("// implementation")
            .CloseBrace()
            .CloseBrace()
            .CloseBrace()
            .ToString();
        
        // Assert
        var expected = "namespace Test" + Environment.NewLine +
                      "{" + Environment.NewLine +
                      "    public class Example" + Environment.NewLine +
                      "    {" + Environment.NewLine +
                      "        public void Method()" + Environment.NewLine +
                      "        {" + Environment.NewLine +
                      "            // implementation" + Environment.NewLine +
                      "        }" + Environment.NewLine +
                      "    }" + Environment.NewLine +
                      "}" + Environment.NewLine;
        await Assert.That(result)
            .IsEqualTo(expected);
    }
    
    [Test]
    public async Task MixedContent_ShouldHandleComplexScenarios()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.AppendLine("using System;");
        builder.AppendLine();
        builder.OpenBraceWithLine("namespace TestNamespace");
        builder.AppendLine();
        builder.OpenBraceWithLine("public class TestClass");
        builder.AppendLine("private string _field;");
        builder.AppendLine();
        builder.OpenBraceWithLine("public void TestMethod()");
        builder.AppendLine("var result = GetValue();");
        builder.CloseBrace();
        builder.CloseBrace();
        builder.CloseBrace();
        var result = builder.ToString();
        
        // Assert
        var expected = "using System;" + Environment.NewLine +
                      Environment.NewLine +
                      "namespace TestNamespace" + Environment.NewLine +
                      "{" + Environment.NewLine +
                      Environment.NewLine +
                      "    public class TestClass" + Environment.NewLine +
                      "    {" + Environment.NewLine +
                      "        private string _field;" + Environment.NewLine +
                      Environment.NewLine +
                      "        public void TestMethod()" + Environment.NewLine +
                      "        {" + Environment.NewLine +
                      "            var result = GetValue();" + Environment.NewLine +
                      "        }" + Environment.NewLine +
                      "    }" + Environment.NewLine +
                      "}" + Environment.NewLine;
        await Assert.That(result)
            .IsEqualTo(expected);
    }
    
    [Test]
    public async Task ToString_EmptyBuilder_ShouldReturnEmptyString()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        var result = builder.ToString();
        
        // Assert
        await Assert.That(result)
            .IsEqualTo(string.Empty);
    }
    
    [Test]
    public async Task IndentString_ShouldBeFourSpaces()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.Indent();
        builder.AppendLine("test");
        var result = builder.ToString();
        
        // Assert - Should have exactly 4 spaces before "test"
        await Assert.That(result)
            .StartsWith("    test");
        await Assert.That(result.Substring(0, 4))
            .IsEqualTo("    "); // Exactly 4 spaces
    }
    
    [Test]
    public async Task NestedBraces_ShouldMaintainCorrectIndentation()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.OpenBraceWithLine("if (condition)");
        builder.OpenBraceWithLine("while (loop)");
        builder.AppendLine("DoSomething();");
        builder.CloseBrace();
        builder.CloseBrace();
        var result = builder.ToString();
        
        // Assert
        var expected = "if (condition)" + Environment.NewLine +
                      "{" + Environment.NewLine +
                      "    while (loop)" + Environment.NewLine +
                      "    {" + Environment.NewLine +
                      "        DoSomething();" + Environment.NewLine +
                      "    }" + Environment.NewLine +
                      "}" + Environment.NewLine;
        await Assert.That(result)
            .IsEqualTo(expected);
    }
    
    [Test]
    public async Task ManualIndentAndBraces_ShouldWorkTogether()
    {
        // Arrange
        var builder = new CodeBuilder();
        
        // Act
        builder.AppendLine("class Test");
        builder.Indent();
        builder.AppendLine(": BaseClass");
        builder.Unindent();
        builder.OpenBrace();
        builder.AppendLine("// content");
        builder.CloseBrace();
        var result = builder.ToString();
        
        // Assert
        var expected = "class Test" + Environment.NewLine +
                      "    : BaseClass" + Environment.NewLine +
                      "{" + Environment.NewLine +
                      "    // content" + Environment.NewLine +
                      "}" + Environment.NewLine;
        await Assert.That(result)
            .IsEqualTo(expected);
    }
}