using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Attributes;
using Goa.Clients.Dynamo.Generator.Models;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.Attributes;

public class UnixTimestampAttributeHandlerTests
{
    private readonly UnixTimestampAttributeHandler _handler = new();

    [Test]
    public async Task AttributeTypeName_ShouldReturnCorrectTypeName()
    {
        // Act
        var typeName = _handler.AttributeTypeName;
        
        // Assert
        await Assert.That(typeName).IsEqualTo("Goa.Clients.Dynamo.UnixTimestampAttribute");
    }

    [Test]
    public async Task CanHandle_WithMatchingAttribute_ShouldReturnTrue()
    {
        // Arrange
        var attributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.UnixTimestampAttribute");
        
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
    public async Task ParseAttribute_WithDefaultFormat_ShouldReturnSecondsFormat()
    {
        // Arrange
        var attributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.UnixTimestampAttribute");
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result is UnixTimestampAttributeInfo).IsTrue();
        
        var unixInfo = (UnixTimestampAttributeInfo)result!;
        await Assert.That(unixInfo.AttributeTypeName).IsEqualTo("Goa.Clients.Dynamo.UnixTimestampAttribute");
        await Assert.That((int)unixInfo.Format).IsEqualTo((int)UnixTimestampFormat.Seconds);
    }

    [Test]
    public async Task ParseAttribute_WithSecondsFormat_ShouldReturnSecondsFormat()
    {
        // Arrange
        var namedArgs = new Dictionary<string, object?>
        {
            ["Format"] = (int)UnixTimestampFormat.Seconds
        };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.UnixTimestampAttribute",
            namedArgs: namedArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        
        var unixInfo = (UnixTimestampAttributeInfo)result!;
        await Assert.That((int)unixInfo.Format).IsEqualTo((int)UnixTimestampFormat.Seconds);
    }

    [Test]
    public async Task ParseAttribute_WithMillisecondsFormat_ShouldReturnMillisecondsFormat()
    {
        // Arrange
        var namedArgs = new Dictionary<string, object?>
        {
            ["Format"] = (int)UnixTimestampFormat.Milliseconds
        };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.UnixTimestampAttribute",
            namedArgs: namedArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        
        var unixInfo = (UnixTimestampAttributeInfo)result!;
        await Assert.That((int)unixInfo.Format).IsEqualTo((int)UnixTimestampFormat.Milliseconds);
    }

    [Test]
    public async Task ParseAttribute_WithInvalidFormatValue_ShouldUseDefaultFormat()
    {
        // Arrange
        var namedArgs = new Dictionary<string, object?>
        {
            ["Format"] = "InvalidFormat"
        };
        var attributeData = MockSymbolFactory.CreateAttributeData(
            "Goa.Clients.Dynamo.UnixTimestampAttribute",
            namedArgs: namedArgs);
        
        // Act
        var result = _handler.ParseAttribute(attributeData);
        
        // Assert
        await Assert.That(result).IsNotNull();
        
        var unixInfo = (UnixTimestampAttributeInfo)result!;
        await Assert.That((int)unixInfo.Format).IsEqualTo((int)UnixTimestampFormat.Seconds);
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
    public async Task ValidateAttribute_WithDateTimeProperty_ShouldNotReportDiagnostics()
    {
        // Arrange
        var attributeInfo = new UnixTimestampAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.UnixTimestampAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.UnixTimestampAttribute",
            Format = Models.UnixTimestampFormat.Seconds
        };
        
        var mockProperty = MockSymbolFactory.CreatePropertySymbol(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime);
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockProperty.Object, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task ValidateAttribute_WithDateTimeOffsetProperty_ShouldNotReportDiagnostics()
    {
        // Arrange
        var attributeInfo = new UnixTimestampAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.UnixTimestampAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.UnixTimestampAttribute",
            Format = Models.UnixTimestampFormat.Seconds
        };
        
        var dateTimeOffsetType = MockSymbolFactory.CreateNamedTypeSymbol(
            nameof(DateTimeOffset),
            $"System.{nameof(DateTimeOffset)}",
            "System").Object;
        var mockProperty = MockSymbolFactory.CreatePropertySymbol("CreatedAt", dateTimeOffsetType);
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockProperty.Object, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task ValidateAttribute_WithNullableDateTimeProperty_ShouldNotReportDiagnostics()
    {
        // Arrange
        var attributeInfo = new UnixTimestampAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.UnixTimestampAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.UnixTimestampAttribute",
            Format = Models.UnixTimestampFormat.Seconds
        };
        
        var nullableDateTimeType = MockSymbolFactory.CreateNullableType(MockSymbolFactory.PrimitiveTypes.DateTime);
        var mockProperty = MockSymbolFactory.CreatePropertySymbol("CreatedAt", nullableDateTimeType.Object);
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockProperty.Object, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }

    [Test]
    public async Task ValidateAttribute_WithInvalidPropertyType_ShouldReportDiagnostic()
    {
        // Arrange
        var attributeInfo = new UnixTimestampAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.UnixTimestampAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.UnixTimestampAttribute",
            Format = Models.UnixTimestampFormat.Seconds
        };
        
        var mockProperty = MockSymbolFactory.CreatePropertySymbol("Name", MockSymbolFactory.PrimitiveTypes.String);
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockProperty.Object, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics.Count).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("DYNAMO006");
        await Assert.That(diagnostics[0].Severity).IsEqualTo(DiagnosticSeverity.Error);
        await Assert.That(diagnostics[0].GetMessage()).Contains("UnixTimestamp attribute can only be applied to DateTime or DateTimeOffset properties");
        await Assert.That(diagnostics[0].GetMessage()).Contains("Property 'Name' has type 'string'");
    }

    [Test]
    public async Task ValidateAttribute_WithNonPropertySymbol_ShouldNotReportDiagnostics()
    {
        // Arrange
        var attributeInfo = new UnixTimestampAttributeInfo
        {
            AttributeData = MockSymbolFactory.CreateAttributeData("Goa.Clients.Dynamo.UnixTimestampAttribute"),
            AttributeTypeName = "Goa.Clients.Dynamo.UnixTimestampAttribute",
            Format = Models.UnixTimestampFormat.Seconds
        };
        
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockSymbol, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics).IsEmpty();
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
        
        var mockProperty = MockSymbolFactory.CreatePropertySymbol("Name", MockSymbolFactory.PrimitiveTypes.String);
        var diagnostics = new List<Diagnostic>();
        
        // Act
        _handler.ValidateAttribute(attributeInfo, mockProperty.Object, diagnostics.Add);
        
        // Assert
        await Assert.That(diagnostics).IsEmpty();
    }
}