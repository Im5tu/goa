using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Attributes;
using Goa.Clients.Dynamo.Generator.Models;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.Attributes;

public class GSIAttributeHandlerTests
{
    private readonly GSIAttributeHandler _handler = new();

    [Test]
    public async Task AttributeTypeName_ShouldReturnCorrectTypeName()
    {
        // Act
        var typeName = _handler.AttributeTypeName;
        
        // Assert
        await Assert.That(typeName).IsEqualTo("Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute");
    }

    [Test]
    public async Task CanHandle_WithMatchingAttribute_ShouldReturnTrue()
    {
        // Arrange
        var attributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute");
        
        // Act
        var canHandle = _handler.CanHandle(attributeData);
        
        // Assert
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task CanHandle_WithNonMatchingAttribute_ShouldReturnFalse()
    {
        // Arrange
        var attributeData = MockSymbolFactory.CreateAttributeData("SomeOtherAttribute");
        
        // Act
        var canHandle = _handler.CanHandle(attributeData);
        
        // Assert
        await Assert.That(canHandle).IsFalse();
    }

    [Test]
    public async Task ParseAttribute_WithConstructorArguments_ShouldReturnCorrectInfo()
    {
        // Arrange
        var constructorArgs = new object[] { "EmailIndex", "EMAIL#<Email>", "USER#<Id>" };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute",
            constructorArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result is GSIAttributeInfo).IsTrue();
        
        var gsiInfo = (GSIAttributeInfo)result!;
        await Assert.That(gsiInfo.AttributeTypeName).IsEqualTo("Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute");
        await Assert.That(gsiInfo.IndexName).IsEqualTo("EmailIndex");
        await Assert.That(gsiInfo.PK).IsEqualTo("EMAIL#<Email>");
        await Assert.That(gsiInfo.SK).IsEqualTo("USER#<Id>");
        await Assert.That(gsiInfo.PKName).IsNull();
        await Assert.That(gsiInfo.SKName).IsNull();
    }

    [Test]
    public async Task ParseAttribute_WithNamedArguments_ShouldReturnCorrectInfo()
    {
        // Arrange
        var namedArgs = new Dictionary<string, object?>
        {
            ["Name"] = "StatusIndex",
            ["PK"] = "STATUS#<Status>",
            ["SK"] = "CREATED#<CreatedAt>",
            ["PKName"] = "GSI1PK",
            ["SKName"] = "GSI1SK"
        };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute",
            namedArgs: namedArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result is GSIAttributeInfo).IsTrue();
        
        var gsiInfo = (GSIAttributeInfo)result!;
        await Assert.That(gsiInfo.IndexName).IsEqualTo("StatusIndex");
        await Assert.That(gsiInfo.PK).IsEqualTo("STATUS#<Status>");
        await Assert.That(gsiInfo.SK).IsEqualTo("CREATED#<CreatedAt>");
        await Assert.That(gsiInfo.PKName).IsEqualTo("GSI1PK");
        await Assert.That(gsiInfo.SKName).IsEqualTo("GSI1SK");
    }

    [Test]
    public async Task ParseAttribute_WithMixedArguments_ShouldPrioritizeNamedArguments()
    {
        // Arrange - Constructor args provide initial values, named args override
        var constructorArgs = new object[] { "OldIndex", "OLD_PK", "OLD_SK" };
        var namedArgs = new Dictionary<string, object?>
        {
            ["Name"] = "NewIndex",
            ["PK"] = "NEW_PK",
            ["SK"] = "NEW_SK"
        };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute",
            constructorArgs,
            namedArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        
        var gsiInfo = (GSIAttributeInfo)result!;
        await Assert.That(gsiInfo.IndexName).IsEqualTo("NewIndex");
        await Assert.That(gsiInfo.PK).IsEqualTo("NEW_PK");
        await Assert.That(gsiInfo.SK).IsEqualTo("NEW_SK");
    }

    [Test]
    public async Task ParseAttribute_WithEmptyIndexName_ShouldReturnNull()
    {
        // Arrange
        var constructorArgs = new object[] { "", "EMAIL#<Email>", "USER#<Id>" };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute",
            constructorArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseAttribute_WithInsufficientConstructorArgs_ShouldReturnNull()
    {
        // Arrange - Only two constructor arguments provided
        var constructorArgs = new object[] { "EmailIndex", "EMAIL#<Email>" };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute",
            constructorArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseAttribute_WithNonMatchingAttribute_ShouldReturnNull()
    {
        // Arrange
        var attributeData = MockSymbolFactory.CreateAttributeData("SomeOtherAttribute");
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ValidateAttribute_WithValidAttribute_ShouldNotReportDiagnostics()
    {
        // Arrange
        var attributeInfo = new GSIAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute",
            IndexName = "EmailIndex",
            PK = "EMAIL#<Email>",
            SK = "USER#<Id>",
            PKName = "GSI1PK",
            SKName = "GSI1SK"
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task ValidateAttribute_WithEmptyIndexName_ShouldReportDiagnostic()
    {
        // Arrange
        var attributeInfo = new GSIAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute",
            IndexName = "",
            PK = "EMAIL#<Email>",
            SK = "USER#<Id>"
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics.Count).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("DYNAMO007");
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("GSI IndexName cannot be null or empty on type 'TestClass'");
    }

    [Test]
    public async Task ValidateAttribute_WithEmptyPK_ShouldReportDiagnostic()
    {
        // Arrange
        var attributeInfo = new GSIAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute",
            IndexName = "EmailIndex",
            PK = "",
            SK = "USER#<Id>"
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics.Count).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("DYNAMO008");
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("GSI PK pattern cannot be null or empty on type 'TestClass'");
    }

    [Test]
    public async Task ValidateAttribute_WithEmptySK_ShouldReportDiagnostic()
    {
        // Arrange
        var attributeInfo = new GSIAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute",
            IndexName = "EmailIndex",
            PK = "EMAIL#<Email>",
            SK = ""
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics.Count).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("DYNAMO009");
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("GSI SK pattern cannot be null or empty on type 'TestClass'");
    }

    [Test]
    public async Task ValidateAttribute_WithWrongAttributeType_ShouldNotReportDiagnostics()
    {
        // Arrange
        var attributeInfo = new DynamoModelAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("SomeOtherAttribute"),
            AttributeTypeName = "SomeOtherAttribute",
            PK = "USER#<Id>",
            SK = "METADATA"
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }
}