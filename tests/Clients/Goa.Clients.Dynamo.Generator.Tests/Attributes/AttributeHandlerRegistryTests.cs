using Microsoft.CodeAnalysis;
using Moq;
using Goa.Clients.Dynamo.Generator.Attributes;
using Goa.Clients.Dynamo.Generator.Models;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.Attributes;

public class AttributeHandlerRegistryTests
{
    [Test]
    public async Task RegisterHandler_ShouldAddHandlerToRegistry()
    {
        // Arrange
        var registry = new AttributeHandlerRegistry();
        var mockHandler = new Mock<IAttributeHandler>();

        // Act
        registry.RegisterHandler(mockHandler.Object);

        // Assert - Verify handler is registered by checking it processes attributes
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;
        var attributes = registry.ProcessAttributes(mockSymbol);

        await Assert.That(attributes).IsNotNull();
    }

    [Test]
    public async Task ProcessAttributes_WithNoHandlers_ShouldReturnEmptyList()
    {
        // Arrange
        var registry = new AttributeHandlerRegistry();
        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol("TestClass", "TestClass", "TestNamespace").Object;

        // Act
        var attributes = registry.ProcessAttributes(mockSymbol);

        // Assert
        await Assert.That(attributes).IsNotNull();
        await Assert.That(attributes).IsEmpty();
    }

    [Test]
    public async Task ProcessAttributes_WithMatchingHandler_ShouldReturnAttributeInfo()
    {
        // Arrange
        var registry = new AttributeHandlerRegistry();
        var mockHandler = new Mock<IAttributeHandler>();
        var mockAttributeData = MockSymbolFactory.CreateAttributeData("TestAttribute");
        var expectedAttributeInfo = new TestAttributeInfo
        {
            AttributeData = mockAttributeData,
            AttributeTypeName = "TestAttribute"
        };

        mockHandler.Setup(h => h.CanHandle(It.IsAny<AttributeData>())).Returns(true);
        mockHandler.Setup(h => h.ParseAttribute(It.IsAny<AttributeData>())).Returns(expectedAttributeInfo);

        registry.RegisterHandler(mockHandler.Object);

        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol(
            "TestClass",
            "TestClass",
            "TestNamespace",
            attributes: [mockAttributeData]).Object;

        // Act
        var attributes = registry.ProcessAttributes(mockSymbol);

        // Assert
        await Assert.That(attributes).Count().EqualTo(1);
        await Assert.That(attributes[0]).IsEqualTo(expectedAttributeInfo);

        mockHandler.Verify(h => h.CanHandle(mockAttributeData), Times.Once);
        mockHandler.Verify(h => h.ParseAttribute(mockAttributeData), Times.Once);
    }

    [Test]
    public async Task ProcessAttributes_WithNonMatchingHandler_ShouldReturnEmptyList()
    {
        // Arrange
        var registry = new AttributeHandlerRegistry();
        var mockHandler = new Mock<IAttributeHandler>();
        var mockAttributeData = MockSymbolFactory.CreateAttributeData("TestAttribute");

        mockHandler.Setup(h => h.CanHandle(It.IsAny<AttributeData>())).Returns(false);

        registry.RegisterHandler(mockHandler.Object);

        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol(
            "TestClass",
            "TestClass",
            "TestNamespace",
            attributes: [mockAttributeData]).Object;

        // Act
        var attributes = registry.ProcessAttributes(mockSymbol);

        // Assert
        await Assert.That(attributes).IsEmpty();

        mockHandler.Verify(h => h.CanHandle(mockAttributeData), Times.Once);
        mockHandler.Verify(h => h.ParseAttribute(It.IsAny<AttributeData>()), Times.Never);
    }

    [Test]
    public async Task ProcessAttributes_WithMultipleHandlers_ShouldUseFirstMatchingHandler()
    {
        // Arrange
        var registry = new AttributeHandlerRegistry();
        var firstHandler = new Mock<IAttributeHandler>();
        var secondHandler = new Mock<IAttributeHandler>();
        var mockAttributeData = MockSymbolFactory.CreateAttributeData("TestAttribute");
        var expectedAttributeInfo = new TestAttributeInfo
        {
            AttributeData = mockAttributeData,
            AttributeTypeName = "TestAttribute"
        };

        // Both handlers can handle the attribute
        firstHandler.Setup(h => h.CanHandle(It.IsAny<AttributeData>())).Returns(true);
        firstHandler.Setup(h => h.ParseAttribute(It.IsAny<AttributeData>())).Returns(expectedAttributeInfo);
        secondHandler.Setup(h => h.CanHandle(It.IsAny<AttributeData>())).Returns(true);

        registry.RegisterHandler(firstHandler.Object);
        registry.RegisterHandler(secondHandler.Object);

        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol(
            "TestClass",
            "TestClass",
            "TestNamespace",
            attributes: [mockAttributeData]).Object;

        // Act
        var attributes = registry.ProcessAttributes(mockSymbol);

        // Assert
        await Assert.That(attributes).Count().EqualTo(1);

        // First handler should be used
        firstHandler.Verify(h => h.CanHandle(mockAttributeData), Times.Once);
        firstHandler.Verify(h => h.ParseAttribute(mockAttributeData), Times.Once);

        // Second handler should not be called since first one handled it
        secondHandler.Verify(h => h.CanHandle(It.IsAny<AttributeData>()), Times.Never);
    }

    [Test]
    public async Task ValidateAttributes_ShouldCallValidateOnMatchingHandlers()
    {
        // Arrange
        var registry = new AttributeHandlerRegistry();
        var mockHandler = new Mock<IAttributeHandler>();
        var mockAttributeData = MockSymbolFactory.CreateAttributeData("TestAttribute");
        var attributeInfo = new TestAttributeInfo
        {
            AttributeData = mockAttributeData,
            AttributeTypeName = "TestAttribute"
        };

        mockHandler.Setup(h => h.AttributeTypeName).Returns("TestAttribute");
        mockHandler.Setup(h => h.CanHandle(It.IsAny<AttributeData>())).Returns(true);
        mockHandler.Setup(h => h.ParseAttribute(It.IsAny<AttributeData>())).Returns(attributeInfo);

        registry.RegisterHandler(mockHandler.Object);

        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol(
            "TestClass",
            "TestClass",
            "TestNamespace",
            attributes: [mockAttributeData]).Object;

        var diagnostics = new List<Diagnostic>();

        // Act
        registry.ValidateAttributes(mockSymbol, diagnostics.Add);

        // Assert
        await Task.CompletedTask; // Add await to satisfy async requirement
        mockHandler.Verify(h => h.ValidateAttribute(
            attributeInfo,
            mockSymbol,
            It.IsAny<Action<Diagnostic>>()), Times.Once);
    }

    [Test]
    public async Task GetAttributes_ShouldReturnOnlyAttributesOfSpecifiedType()
    {
        // Arrange
        var registry = new AttributeHandlerRegistry();
        var handler1 = new Mock<IAttributeHandler>();
        var handler2 = new Mock<IAttributeHandler>();

        var attributeData1 = MockSymbolFactory.CreateAttributeData("TestAttribute1");
        var attributeData2 = MockSymbolFactory.CreateAttributeData("TestAttribute2");

        var attribute1 = new TestAttributeInfo
        {
            AttributeData = attributeData1,
            AttributeTypeName = "TestAttribute1"
        };

        var attribute2 = new DynamoModelAttributeInfo
        {
            AttributeData = attributeData2,
            AttributeTypeName = "TestAttribute2",
            PK = "TEST#<Id>",
            SK = "DATA",
            PKName = "PK",
            SKName = "SK"
        };

        handler1.Setup(h => h.CanHandle(attributeData1)).Returns(true);
        handler1.Setup(h => h.ParseAttribute(attributeData1)).Returns(attribute1);
        handler2.Setup(h => h.CanHandle(attributeData2)).Returns(true);
        handler2.Setup(h => h.ParseAttribute(attributeData2)).Returns(attribute2);

        registry.RegisterHandler(handler1.Object);
        registry.RegisterHandler(handler2.Object);

        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol(
            "TestClass",
            "TestClass",
            "TestNamespace",
            attributes: [attributeData1, attributeData2]).Object;

        // Act
        var dynamoAttributes = registry.GetAttributes<DynamoModelAttributeInfo>(mockSymbol);

        // Assert
        await Assert.That(dynamoAttributes).Count().EqualTo(1);
        await Assert.That(dynamoAttributes[0]).IsEqualTo(attribute2);
    }

    [Test]
    public async Task ProcessAttributes_HandlerReturnsNull_ShouldNotAddToResults()
    {
        // Arrange
        var registry = new AttributeHandlerRegistry();
        var mockHandler = new Mock<IAttributeHandler>();
        var mockAttributeData = MockSymbolFactory.CreateAttributeData("TestAttribute");

        mockHandler.Setup(h => h.CanHandle(It.IsAny<AttributeData>())).Returns(true);
        mockHandler.Setup(h => h.ParseAttribute(It.IsAny<AttributeData>())).Returns((AttributeInfo?)null);

        registry.RegisterHandler(mockHandler.Object);

        var mockSymbol = MockSymbolFactory.CreateNamedTypeSymbol(
            "TestClass",
            "TestClass",
            "TestNamespace",
            attributes: [mockAttributeData]).Object;

        // Act
        var attributes = registry.ProcessAttributes(mockSymbol);

        // Assert
        await Assert.That(attributes).IsEmpty();

        mockHandler.Verify(h => h.CanHandle(mockAttributeData), Times.Once);
        mockHandler.Verify(h => h.ParseAttribute(mockAttributeData), Times.Once);
    }
}

/// <summary>
/// Test implementation of AttributeInfo for testing purposes.
/// </summary>
public class TestAttributeInfo : AttributeInfo
{
}
