using Goa.Clients.Dynamo.Generator.CodeGeneration;
using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;
using Goa.Clients.Dynamo.Generator.Models;
using System.Collections.Immutable;

namespace Goa.Clients.Dynamo.Generator.Tests.CodeGeneration;

public class JsonMapperGeneratorTests
{
    private readonly TypeHandlerRegistry _typeHandlerRegistry;
    private readonly JsonMapperGenerator _generator;

    public JsonMapperGeneratorTests()
    {
        _typeHandlerRegistry = CreateTypeHandlerRegistry();
        _generator = new JsonMapperGenerator(_typeHandlerRegistry);
    }

    [Test]
    public async Task GenerateCode_WithUnixTimestampAndCollectionOfComplexType_ShouldEmitFromUnixTimeConversion()
    {
        // Arrange: A parent type with [UnixTimestamp] DateTimeOffset + List<NestedType>
        // This reproduces the bug where adding a referenced type via collection
        // causes UnixTimestamp handling to break in the generated JSON mapper.

        var nestedProperties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Name", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Quantity", MockSymbolFactory.PrimitiveTypes.Int32),
        };

        var nestedType = TestModelBuilders.CreateDynamoTypeInfo(
            "Ingredient",
            "TestNamespace.Ingredient",
            properties: nestedProperties);

        // Create a collection type: List<Ingredient>
        var listMock = MockSymbolFactory.CreateNamedTypeSymbol(
            "List",
            "System.Collections.Generic.List<TestNamespace.Ingredient>",
            "System.Collections.Generic");
        listMock.Setup(x => x.IsGenericType).Returns(true);
        listMock.Setup(x => x.TypeArguments).Returns(ImmutableArray.Create<Microsoft.CodeAnalysis.ITypeSymbol>(nestedType.Symbol!));

        var parentProperties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Name", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreateUnixTimestampPropertyInfo("UpdatedAt", MockSymbolFactory.PrimitiveTypes.DateTimeOffset),
            TestModelBuilders.CreateCollectionPropertyInfo(
                "Ingredients",
                listMock.Object,
                nestedType.Symbol!,
                isNullable: true),
        };

        var parentType = TestModelBuilders.CreateDynamoTypeInfo(
            "Recipe",
            "TestNamespace.Recipe",
            properties: parentProperties,
            attributes: new List<AttributeInfo>
            {
                TestModelBuilders.CreateDynamoModelAttribute("RECIPE#<Id>", "METADATA")
            });

        var types = new List<DynamoTypeInfo> { parentType, nestedType };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert: The generated code must contain FromUnixTimeSeconds for the UpdatedAt read path
        await Assert.That(result)
            .Contains("FromUnixTimeSeconds");

        // Assert: The generated code must contain ToUnixTimeSeconds for the UpdatedAt write path
        await Assert.That(result)
            .Contains("ToUnixTimeSeconds");

        // Assert: The generated code should NOT directly assign long to DateTimeOffset without conversion
        await Assert.That(result)
            .DoesNotContain("= (DateTimeOffset)");
    }

    [Test]
    public async Task GenerateCode_WithIgnoredPropertyAndCollectionOfComplexType_ShouldSkipIgnoredProperty()
    {
        // Arrange: A parent type with [Ignore] property + List<NestedType>
        var nestedProperties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Name", MockSymbolFactory.PrimitiveTypes.String),
        };

        var nestedType = TestModelBuilders.CreateDynamoTypeInfo(
            "Tag",
            "TestNamespace.Tag",
            properties: nestedProperties);

        var listMock = MockSymbolFactory.CreateNamedTypeSymbol(
            "List",
            "System.Collections.Generic.List<TestNamespace.Tag>",
            "System.Collections.Generic");
        listMock.Setup(x => x.IsGenericType).Returns(true);
        listMock.Setup(x => x.TypeArguments).Returns(ImmutableArray.Create<Microsoft.CodeAnalysis.ITypeSymbol>(nestedType.Symbol!));

        var ignoredAttr = new IgnoreAttributeInfo { Direction = Models.IgnoreDirection.Always };

        var parentProperties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("InternalState", MockSymbolFactory.PrimitiveTypes.String,
                attributes: new List<AttributeInfo> { ignoredAttr }),
            TestModelBuilders.CreateCollectionPropertyInfo(
                "Tags",
                listMock.Object,
                nestedType.Symbol!,
                isNullable: true),
        };

        var parentType = TestModelBuilders.CreateDynamoTypeInfo(
            "Item",
            "TestNamespace.Item",
            properties: parentProperties,
            attributes: new List<AttributeInfo>
            {
                TestModelBuilders.CreateDynamoModelAttribute("ITEM#<Id>", "METADATA")
            });

        var types = new List<DynamoTypeInfo> { parentType, nestedType };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert: InternalState should NOT appear in the generated write code
        await Assert.That(result)
            .DoesNotContain("\"InternalState\"");
    }

    private static TypeHandlerRegistry CreateTypeHandlerRegistry()
    {
        var registry = new TypeHandlerRegistry();
        registry.RegisterHandler(new PrimitiveTypeHandler());
        registry.RegisterHandler(new EnumTypeHandler());
        registry.RegisterHandler(new DateOnlyTypeHandler());
        registry.RegisterHandler(new TimeOnlyTypeHandler());
        registry.RegisterHandler(new DateTimeTypeHandler());
        registry.RegisterHandler(new UnixTimestampTypeHandler());
        var collectionHandler = new CollectionTypeHandler();
        collectionHandler.SetRegistry(registry);
        registry.RegisterHandler(collectionHandler);
        var complexHandler = new ComplexTypeHandler();
        complexHandler.SetRegistry(registry);
        registry.RegisterHandler(complexHandler);
        return registry;
    }
}
