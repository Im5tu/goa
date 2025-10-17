using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.CodeGeneration;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Tests.Integration;

/// <summary>
/// Integration tests matching the NullAwarenessTest scenario from TestConsole.
/// Tests that nullable and non-nullable types are handled correctly.
/// </summary>
public class NullAwarenessIntegrationTests
{
    private readonly TypeHandlerRegistry _typeHandlerRegistry;
    private readonly MapperGenerator _mapperGenerator;

    public NullAwarenessIntegrationTests()
    {
        _typeHandlerRegistry = CreateTypeHandlerRegistry();
        _mapperGenerator = new MapperGenerator(_typeHandlerRegistry);
    }

    [Test]
    public async Task NullableStringProperty_ShouldGenerateCorrectToAttributeValueCode()
    {
        var property = TestModelBuilders.CreatePropertyInfo(
            "Description",
            MockSymbolFactory.PrimitiveTypes.String,
            isNullable: true);

        var result = _typeHandlerRegistry.GenerateToAttributeValue(property);

        // Nullable strings return null to trigger conditional assignment for sparse GSI compatibility
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task NonNullableStringProperty_ShouldGenerateCorrectToAttributeValueCode()
    {
        var property = TestModelBuilders.CreatePropertyInfo(
            "Name",
            MockSymbolFactory.PrimitiveTypes.String,
            isNullable: false);

        var result = _typeHandlerRegistry.GenerateToAttributeValue(property);

        var expected = "new AttributeValue { S = model.Name }";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task NullableIntProperty_ShouldGenerateCorrectToAttributeValueCode()
    {
        var property = TestModelBuilders.CreatePropertyInfo(
            "Count", 
            MockSymbolFactory.PrimitiveTypes.Int32, 
            isNullable: true);

        var result = _typeHandlerRegistry.GenerateToAttributeValue(property);

        // Nullable properties return null to trigger conditional assignment for sparse GSI compatibility
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task NonNullableIntProperty_ShouldGenerateCorrectToAttributeValueCode()
    {
        var property = TestModelBuilders.CreatePropertyInfo(
            "Id", 
            MockSymbolFactory.PrimitiveTypes.Int32, 
            isNullable: false);

        var result = _typeHandlerRegistry.GenerateToAttributeValue(property);

        var expected = "new AttributeValue { N = model.Id.ToString() }";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task NullableStringProperty_ShouldGenerateCorrectFromDynamoRecordCode()
    {
        var property = TestModelBuilders.CreatePropertyInfo(
            "Description", 
            MockSymbolFactory.PrimitiveTypes.String, 
            isNullable: true);

        var result = _typeHandlerRegistry.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        var expected = "record.TryGetNullableString(\"Description\", out var description) ? description : null";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task NonNullableStringProperty_ShouldThrowMissingAttributeException()
    {
        var property = TestModelBuilders.CreatePropertyInfo(
            "Name", 
            MockSymbolFactory.PrimitiveTypes.String, 
            isNullable: false);

        var result = _typeHandlerRegistry.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        var expected = "record.TryGetString(\"Name\", out var name) ? name : MissingAttributeException.Throw<string>(\"Name\", pkValue, skValue)";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task NullableIntProperty_ShouldGenerateCorrectFromDynamoRecordCode()
    {
        var property = TestModelBuilders.CreatePropertyInfo(
            "Count", 
            MockSymbolFactory.PrimitiveTypes.Int32, 
            isNullable: true);

        var result = _typeHandlerRegistry.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        var expected = "record.TryGetNullableInt(\"Count\", out var count) ? count : null";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task NonNullableIntProperty_ShouldThrowMissingAttributeException()
    {
        var property = TestModelBuilders.CreatePropertyInfo(
            "Id", 
            MockSymbolFactory.PrimitiveTypes.Int32, 
            isNullable: false);

        var result = _typeHandlerRegistry.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        var expected = "record.TryGetInt(\"Id\", out var id) ? id : MissingAttributeException.Throw<int>(\"Id\", pkValue, skValue)";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task CompleteModel_ShouldGenerateCorrectMapperCode()
    {
        // Create a test model similar to what's tested in NullAwarenessTest
        var testModel = CreateNullAwarenessTestModel();
        var generationContext = CreateGenerationContext();

        var generatedCode = _mapperGenerator.GenerateCode(new[] { testModel }, generationContext);

        // Verify the generated code contains correct null handling
        // Non-nullable strings are assigned directly
        await Assert.That(generatedCode).Contains("new AttributeValue { S = model.RequiredName }");
        // Nullable strings use conditional assignment for sparse GSI compatibility
        await Assert.That(generatedCode).Contains("if (model.OptionalDescription != null)");
        await Assert.That(generatedCode).Contains("record[\"OptionalDescription\"] = new AttributeValue { S = model.OptionalDescription };");
        // Nullable properties use conditional assignment for sparse GSI compatibility
        await Assert.That(generatedCode).Contains("if (model.OptionalCount.HasValue)");
        await Assert.That(generatedCode).Contains("new AttributeValue { N = model.OptionalCount.Value.ToString() }");

        // Verify MissingAttributeException for non-nullable types
        await Assert.That(generatedCode).Contains("MissingAttributeException.Throw<string>(\"RequiredName\"");
        await Assert.That(generatedCode).Contains("MissingAttributeException.Throw<int>(\"RequiredId\"");
        
        // Verify null returns for nullable types  
        await Assert.That(generatedCode).Contains("record.TryGetNullableString(\"OptionalDescription\", out var optionaldescription) ? optionaldescription : null");
        await Assert.That(generatedCode).Contains("record.TryGetNullableInt(\"OptionalCount\", out var optionalcount) ? optionalcount : null");
    }

    [Test]
    public async Task DateOnlyProperties_ShouldHandleNullabilityCorrectly()
    {
        var nullableDateOnlyProperty = TestModelBuilders.CreatePropertyInfo(
            "OptionalDate", 
            MockSymbolFactory.PrimitiveTypes.DateOnly, 
            isNullable: true);

        var nonNullableDateOnlyProperty = TestModelBuilders.CreatePropertyInfo(
            "RequiredDate", 
            MockSymbolFactory.PrimitiveTypes.DateOnly, 
            isNullable: false);

        var nullableResult = _typeHandlerRegistry.GenerateToAttributeValue(nullableDateOnlyProperty);
        var nonNullableResult = _typeHandlerRegistry.GenerateToAttributeValue(nonNullableDateOnlyProperty);

        // Nullable DateOnly properties return null to trigger conditional assignment for sparse GSI compatibility
        await Assert.That(nullableResult).IsNull();
        
        await Assert.That(nonNullableResult).DoesNotContain("HasValue");
        await Assert.That(nonNullableResult).DoesNotContain("NULL = true");
    }

    [Test]
    public async Task EnumProperties_ShouldHandleNullabilityCorrectly()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Status", 
            "TestNamespace.Status", 
            "TestNamespace",
            typeKind: Microsoft.CodeAnalysis.TypeKind.Enum).Object;

        var nullableEnumProperty = TestModelBuilders.CreatePropertyInfo(
            "OptionalStatus", 
            enumType, 
            isNullable: true);

        var nonNullableEnumProperty = TestModelBuilders.CreatePropertyInfo(
            "RequiredStatus", 
            enumType, 
            isNullable: false);

        var nullableFromRecord = _typeHandlerRegistry.GenerateFromDynamoRecord(nullableEnumProperty, "record", "pkValue", "skValue");
        var nonNullableFromRecord = _typeHandlerRegistry.GenerateFromDynamoRecord(nonNullableEnumProperty, "record", "pkValue", "skValue");

        await Assert.That(nullableFromRecord).Contains(": null");
        await Assert.That(nullableFromRecord).DoesNotContain("MissingAttributeException");
        
        await Assert.That(nonNullableFromRecord).Contains("MissingAttributeException.Throw<TestNamespace.Status>");
        await Assert.That(nonNullableFromRecord).DoesNotContain(": null");
    }

    [Test]
    public async Task AllPrimitiveTypes_ShouldHandleNullabilityConsistently()
    {
        var primitiveTypes = new[]
        {
            (MockSymbolFactory.PrimitiveTypes.Boolean, "bool"),
            (MockSymbolFactory.PrimitiveTypes.Int32, "int"),
            (MockSymbolFactory.PrimitiveTypes.Double, "double"),
            (MockSymbolFactory.PrimitiveTypes.Decimal, "decimal"),
            (MockSymbolFactory.PrimitiveTypes.Guid, "Guid")
        };

        foreach (var (type, typeName) in primitiveTypes)
        {
            // Test nullable variant
            var nullableProperty = TestModelBuilders.CreatePropertyInfo($"Nullable{typeName}Prop", type, isNullable: true);
            var nullableFromRecord = _typeHandlerRegistry.GenerateFromDynamoRecord(nullableProperty, "record", "pkValue", "skValue");
            
            await Assert.That(nullableFromRecord)
                .Contains(": null")
                .Because($"Nullable {typeName} should return null for missing values");
                
            await Assert.That(nullableFromRecord)
                .DoesNotContain("MissingAttributeException")
                .Because($"Nullable {typeName} should not throw exception");

            // Test non-nullable variant
            var nonNullableProperty = TestModelBuilders.CreatePropertyInfo($"NonNullable{typeName}Prop", type, isNullable: false);
            var nonNullableFromRecord = _typeHandlerRegistry.GenerateFromDynamoRecord(nonNullableProperty, "record", "pkValue", "skValue");
            
            // For Guid, the implementation uses simple type name instead of full display string
            var expectedTypeName = type.Name == "Guid" ? "Guid" : type.ToDisplayString();
            await Assert.That(nonNullableFromRecord)
                .Contains($"MissingAttributeException.Throw<{expectedTypeName}>")
                .Because($"Non-nullable {typeName} should throw MissingAttributeException");
                
            await Assert.That(nonNullableFromRecord)
                .DoesNotContain("default(")
                .Because($"Non-nullable {typeName} should not use default value");
        }
    }

    private TypeHandlerRegistry CreateTypeHandlerRegistry()
    {
        var registry = new TypeHandlerRegistry();
        registry.RegisterHandler(new DateOnlyTypeHandler());
        registry.RegisterHandler(new TimeOnlyTypeHandler());
        registry.RegisterHandler(new EnumTypeHandler());
        registry.RegisterHandler(new PrimitiveTypeHandler());
        return registry;
    }

    private DynamoTypeInfo CreateNullAwarenessTestModel()
    {
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("RequiredId", MockSymbolFactory.PrimitiveTypes.Int32, isNullable: false),
            TestModelBuilders.CreatePropertyInfo("RequiredName", MockSymbolFactory.PrimitiveTypes.String, isNullable: false),
            TestModelBuilders.CreatePropertyInfo("OptionalDescription", MockSymbolFactory.PrimitiveTypes.String, isNullable: true),
            TestModelBuilders.CreatePropertyInfo("OptionalCount", MockSymbolFactory.PrimitiveTypes.Int32, isNullable: true),
            TestModelBuilders.CreatePropertyInfo("RequiredDate", MockSymbolFactory.PrimitiveTypes.DateOnly, isNullable: false),
            TestModelBuilders.CreatePropertyInfo("OptionalDate", MockSymbolFactory.PrimitiveTypes.DateOnly, isNullable: true)
        };

        var attributes = new List<AttributeInfo>
        {
            TestModelBuilders.CreateDynamoModelAttribute("TEST#<RequiredId>", "DATA", "PK", "SK")
        };

        return TestModelBuilders.CreateDynamoTypeInfo(
            "NullAwarenessTestModel",
            "TestNamespace.NullAwarenessTestModel",
            properties: properties,
            attributes: attributes);
    }

    private GenerationContext CreateGenerationContext()
    {
        return new GenerationContext
        {
            AvailableConversions = new Dictionary<string, string>
            {
                ["TestNamespace.NullAwarenessTestModel"] = "NullAwarenessTestModel"
            },
            TypeRegistry = new Dictionary<string, List<DynamoTypeInfo>>(),
            ReportDiagnostic = _ => { } // No-op for tests
        };
    }
}