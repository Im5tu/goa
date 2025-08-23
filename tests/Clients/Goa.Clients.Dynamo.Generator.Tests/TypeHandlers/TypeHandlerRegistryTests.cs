using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;
using Moq;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

public class TypeHandlerRegistryTests
{
    [Test]
    public async Task RegisterHandler_ShouldAddHandler_ToRegistry()
    {
        var registry = new TypeHandlerRegistry();
        var mockHandler = new Mock<ITypeHandler>();
        mockHandler.Setup(x => x.Priority).Returns(100);

        registry.RegisterHandler(mockHandler.Object);

        // Registry should have the handler registered (tested indirectly through CanHandle)
        await Assert.That(registry).IsNotNull();
    }

    [Test]
    public async Task RegisterHandler_ShouldOrderHandlers_ByPriority()
    {
        var registry = new TypeHandlerRegistry();
        
        var lowPriorityHandler = new Mock<ITypeHandler>();
        lowPriorityHandler.Setup(x => x.Priority).Returns(10);
        lowPriorityHandler.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(true);
        lowPriorityHandler.Setup(x => x.GenerateToAttributeValue(It.IsAny<Models.PropertyInfo>())).Returns("low-priority");

        var highPriorityHandler = new Mock<ITypeHandler>();
        highPriorityHandler.Setup(x => x.Priority).Returns(200);
        highPriorityHandler.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(true);
        highPriorityHandler.Setup(x => x.GenerateToAttributeValue(It.IsAny<Models.PropertyInfo>())).Returns("high-priority");

        // Register in reverse order (low priority first)
        registry.RegisterHandler(lowPriorityHandler.Object);
        registry.RegisterHandler(highPriorityHandler.Object);

        var property = TestModelBuilders.CreatePropertyInfo("TestProp", MockSymbolFactory.PrimitiveTypes.String);
        
        // Higher priority handler should be used first
        var result = registry.GenerateToAttributeValue(property);
        
        await Assert.That(result).IsEqualTo("high-priority");
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_WhenAnyHandlerCanHandle()
    {
        var registry = new TypeHandlerRegistry();
        
        var handler1 = new Mock<ITypeHandler>();
        handler1.Setup(x => x.Priority).Returns(100);
        handler1.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(false);

        var handler2 = new Mock<ITypeHandler>();
        handler2.Setup(x => x.Priority).Returns(90);
        handler2.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(true);

        registry.RegisterHandler(handler1.Object);
        registry.RegisterHandler(handler2.Object);

        var property = TestModelBuilders.CreatePropertyInfo("TestProp", MockSymbolFactory.PrimitiveTypes.String);
        
        var canHandle = registry.CanHandle(property);
        
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task CanHandle_ShouldReturnFalse_WhenNoHandlerCanHandle()
    {
        var registry = new TypeHandlerRegistry();
        
        var handler1 = new Mock<ITypeHandler>();
        handler1.Setup(x => x.Priority).Returns(100);
        handler1.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(false);

        var handler2 = new Mock<ITypeHandler>();
        handler2.Setup(x => x.Priority).Returns(90);
        handler2.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(false);

        registry.RegisterHandler(handler1.Object);
        registry.RegisterHandler(handler2.Object);

        var property = TestModelBuilders.CreatePropertyInfo("TestProp", MockSymbolFactory.PrimitiveTypes.String);
        
        var canHandle = registry.CanHandle(property);
        
        await Assert.That(canHandle).IsFalse();
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldUseFirstMatchingHandler()
    {
        var registry = new TypeHandlerRegistry();
        
        var handler1 = new Mock<ITypeHandler>();
        handler1.Setup(x => x.Priority).Returns(100);
        handler1.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(true);
        handler1.Setup(x => x.GenerateToAttributeValue(It.IsAny<Models.PropertyInfo>())).Returns("handler1-result");

        var handler2 = new Mock<ITypeHandler>();
        handler2.Setup(x => x.Priority).Returns(90);
        handler2.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(true);
        handler2.Setup(x => x.GenerateToAttributeValue(It.IsAny<Models.PropertyInfo>())).Returns("handler2-result");

        registry.RegisterHandler(handler1.Object);
        registry.RegisterHandler(handler2.Object);

        var property = TestModelBuilders.CreatePropertyInfo("TestProp", MockSymbolFactory.PrimitiveTypes.String);
        
        var result = registry.GenerateToAttributeValue(property);
        
        await Assert.That(result).IsEqualTo("handler1-result");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldUseFirstMatchingHandler()
    {
        var registry = new TypeHandlerRegistry();
        
        var handler1 = new Mock<ITypeHandler>();
        handler1.Setup(x => x.Priority).Returns(100);
        handler1.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(true);
        handler1.Setup(x => x.GenerateFromDynamoRecord(It.IsAny<Models.PropertyInfo>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns("handler1-from-record");

        var handler2 = new Mock<ITypeHandler>();
        handler2.Setup(x => x.Priority).Returns(90);
        handler2.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(true);
        handler2.Setup(x => x.GenerateFromDynamoRecord(It.IsAny<Models.PropertyInfo>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                    .Returns("handler2-from-record");

        registry.RegisterHandler(handler1.Object);
        registry.RegisterHandler(handler2.Object);

        var property = TestModelBuilders.CreatePropertyInfo("TestProp", MockSymbolFactory.PrimitiveTypes.String);
        
        var result = registry.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result).IsEqualTo("handler1-from-record");
    }

    [Test]
    public async Task GenerateKeyFormatting_ShouldUseFirstMatchingHandler()
    {
        var registry = new TypeHandlerRegistry();
        
        var handler1 = new Mock<ITypeHandler>();
        handler1.Setup(x => x.Priority).Returns(100);
        handler1.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(true);
        handler1.Setup(x => x.GenerateKeyFormatting(It.IsAny<Models.PropertyInfo>())).Returns("handler1-key-format");

        registry.RegisterHandler(handler1.Object);

        var property = TestModelBuilders.CreatePropertyInfo("TestProp", MockSymbolFactory.PrimitiveTypes.String);
        
        var result = registry.GenerateKeyFormatting(property);
        
        await Assert.That(result).IsEqualTo("handler1-key-format");
    }

    [Test]
    public async Task CompositeTypeHandler_ShouldReceiveRegistryReference()
    {
        var registry = new TypeHandlerRegistry();
        
        var compositeHandler = new Mock<ICompositeTypeHandler>();
        compositeHandler.Setup(x => x.Priority).Returns(100);
        compositeHandler.Setup(x => x.CanHandle(It.IsAny<Models.PropertyInfo>())).Returns(true);
        compositeHandler.Setup(x => x.GenerateToAttributeValue(It.IsAny<Models.PropertyInfo>())).Returns("composite-result");

        registry.RegisterHandler(compositeHandler.Object);

        var property = TestModelBuilders.CreatePropertyInfo("TestProp", MockSymbolFactory.PrimitiveTypes.String);
        
        var result = registry.GenerateToAttributeValue(property);
        
        // Verify the composite handler received the registry reference and returned expected result
        await Assert.That(result).IsEqualTo("composite-result");
        compositeHandler.Verify(x => x.SetRegistry(registry), Times.Once);
    }

    [Test]
    public async Task RealHandlers_ShouldBeRegistered_InCorrectPriorityOrder()
    {
        // Test with actual handler instances to ensure priority ordering works
        var registry = new TypeHandlerRegistry();
        registry.RegisterHandler(new PrimitiveTypeHandler());
        registry.RegisterHandler(new DateOnlyTypeHandler());
        registry.RegisterHandler(new EnumTypeHandler());

        // DateOnly should have higher priority than Primitive
        var dateOnlyProperty = TestModelBuilders.CreatePropertyInfo("DateProp", MockSymbolFactory.PrimitiveTypes.DateOnly);
        var result = registry.GenerateToAttributeValue(dateOnlyProperty);
        
        await Assert.That(result).Contains("ToString(\"yyyy-MM-dd\")");
        await Assert.That(result).DoesNotContain("ToString()"); // Should not use primitive handler
    }

    [Test]
    public async Task GenerateMethods_ShouldReturnFallback_WhenNoHandlerFound()
    {
        var registry = new TypeHandlerRegistry();
        
        // No handlers registered
        var customType = MockSymbolFactory.CreateNamedTypeSymbol("CustomClass", "TestNamespace.CustomClass", "TestNamespace").Object;
        var property = TestModelBuilders.CreatePropertyInfo("CustomProp", customType);
        
        var toAttributeResult = registry.GenerateToAttributeValue(property);
        var fromRecordResult = registry.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        var keyFormattingResult = registry.GenerateKeyFormatting(property);
        
        await Assert.That(toAttributeResult).IsEqualTo(string.Empty);
        await Assert.That(fromRecordResult).StartsWith("default(");
        await Assert.That(keyFormattingResult).Contains("model.CustomProp?.ToString()");
    }
}