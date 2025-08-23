using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Attributes;
using Goa.Clients.Dynamo.Generator.Models;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.Attributes;

public class DynamoModelAttributeHandlerTests
{
    private readonly DynamoModelAttributeHandler _handler = new();

    [Test]
    public async Task AttributeTypeName_ShouldReturnCorrectTypeName()
    {
        // Act
        var typeName = _handler.AttributeTypeName;
        
        // Assert
        await Assert.That(typeName).IsEqualTo("Goa.Clients.Dynamo.DynamoModelAttribute");
    }

    [Test]
    public async Task CanHandle_WithMatchingAttribute_ShouldReturnTrue()
    {
        // Arrange
        var attributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.DynamoModelAttribute");
        
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
        var constructorArgs = new object[] { "USER#<Id>", "METADATA" };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.DynamoModelAttribute",
            constructorArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result is DynamoModelAttributeInfo).IsTrue();
        
        var dynamoInfo = (DynamoModelAttributeInfo)result!;
        await Assert.That(dynamoInfo.AttributeTypeName).IsEqualTo("Goa.Clients.Dynamo.DynamoModelAttribute");
        await Assert.That(dynamoInfo.PK).IsEqualTo("USER#<Id>");
        await Assert.That(dynamoInfo.SK).IsEqualTo("METADATA");
        await Assert.That(dynamoInfo.PKName).IsEqualTo("PK");
        await Assert.That(dynamoInfo.SKName).IsEqualTo("SK");
    }

    [Test]
    public async Task ParseAttribute_WithNamedArguments_ShouldReturnCorrectInfo()
    {
        // Arrange
        var namedArgs = new Dictionary<string, object?>
        {
            ["PK"] = "PRODUCT#<Id>",
            ["SK"] = "DETAILS",
            ["PKName"] = "PartitionKey",
            ["SKName"] = "SortKey"
        };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.DynamoModelAttribute",
            namedArgs: namedArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result is DynamoModelAttributeInfo).IsTrue();
        
        var dynamoInfo = (DynamoModelAttributeInfo)result!;
        await Assert.That(dynamoInfo.PK).IsEqualTo("PRODUCT#<Id>");
        await Assert.That(dynamoInfo.SK).IsEqualTo("DETAILS");
        await Assert.That(dynamoInfo.PKName).IsEqualTo("PartitionKey");
        await Assert.That(dynamoInfo.SKName).IsEqualTo("SortKey");
    }

    [Test]
    public async Task ParseAttribute_WithMixedArguments_ShouldPrioritizeNamedArguments()
    {
        // Arrange - Constructor args provide initial values, named args override
        var constructorArgs = new object[] { "OLD_PK", "OLD_SK" };
        var namedArgs = new Dictionary<string, object?>
        {
            ["PK"] = "NEW_PK",
            ["SK"] = "NEW_SK"
        };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.DynamoModelAttribute",
            constructorArgs,
            namedArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        
        var dynamoInfo = (DynamoModelAttributeInfo)result!;
        await Assert.That(dynamoInfo.PK).IsEqualTo("NEW_PK");
        await Assert.That(dynamoInfo.SK).IsEqualTo("NEW_SK");
    }

    [Test]
    public async Task ParseAttribute_WithEmptyPK_ShouldReturnNull()
    {
        // Arrange
        var constructorArgs = new object[] { "", "METADATA" };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.DynamoModelAttribute",
            constructorArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseAttribute_WithEmptySK_ShouldReturnNull()
    {
        // Arrange
        var constructorArgs = new object[] { "USER#<Id>", "" };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.DynamoModelAttribute",
            constructorArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ParseAttribute_WithInsufficientConstructorArgs_ShouldReturnNull()
    {
        // Arrange - Only one constructor argument provided
        var constructorArgs = new object[] { "USER#<Id>" };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.DynamoModelAttribute",
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
        var attributeInfo = new DynamoModelAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.DynamoModelAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.DynamoModelAttribute",
            PK = "USER#<Id>",
            SK = "METADATA",
            PKName = "PK",
            SKName = "SK"
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task ValidateAttribute_WithEmptyPK_ShouldReportDiagnostic()
    {
        // Arrange
        var attributeInfo = new DynamoModelAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.DynamoModelAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.DynamoModelAttribute",
            PK = "",
            SK = "METADATA",
            PKName = "PK",
            SKName = "SK"
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics.Count).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("DYNAMO002");
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("PK pattern cannot be null or empty on type 'TestClass'");
    }

    [Test]
    public async Task ValidateAttribute_WithEmptySK_ShouldReportDiagnostic()
    {
        // Arrange
        var attributeInfo = new DynamoModelAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.DynamoModelAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.DynamoModelAttribute",
            PK = "USER#<Id>",
            SK = "",
            PKName = "PK",
            SKName = "SK"
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics.Count).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("DYNAMO003");
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("SK pattern cannot be null or empty on type 'TestClass'");
    }

    [Test]
    public async Task ValidateAttribute_WithBothEmptyPKAndSK_ShouldReportTwoDiagnostics()
    {
        // Arrange
        var attributeInfo = new DynamoModelAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.DynamoModelAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.DynamoModelAttribute",
            PK = "",
            SK = "",
            PKName = "PK",
            SKName = "SK"
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics.Count).IsEqualTo(2);
        await Assert.That(diagnostics.Any(d => d.Id == "DYNAMO002")).IsTrue();
        await Assert.That(diagnostics.Any(d => d.Id == "DYNAMO003")).IsTrue();
    }

    [Test]
    public async Task ValidateAttribute_WithWrongAttributeType_ShouldNotReportDiagnostics()
    {
        // Arrange
        var attributeInfo = new TestAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("SomeOtherAttribute"),
            AttributeTypeName = "SomeOtherAttribute"
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }
}