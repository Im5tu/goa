using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

public class EnumTypeHandlerTests
{
    private readonly EnumTypeHandler _handler;

    public EnumTypeHandlerTests()
    {
        _handler = new EnumTypeHandler();
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForEnumType()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("PriorityProp", enumType);
        
        var canHandle = _handler.CanHandle(property);
        
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForNullableEnumType()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("PriorityProp", enumType, isNullable: true);
        
        var canHandle = _handler.CanHandle(property);
        
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task CanHandle_ShouldReturnFalse_ForNonEnumTypes()
    {
        var nonEnumTypes = new[]
        {
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Int32,
            MockSymbolFactory.PrimitiveTypes.Boolean,
            MockSymbolFactory.CreateNamedTypeSymbol("CustomClass", "TestNamespace.CustomClass", "TestNamespace", typeKind: TypeKind.Class).Object
        };

        foreach (var type in nonEnumTypes)
        {
            var property = TestModelBuilders.CreatePropertyInfo($"Test{type.Name}Prop", type);
            var canHandle = _handler.CanHandle(property);
            
            await Assert.That(canHandle)
                .IsFalse()
                .Because($"EnumTypeHandler should not handle {type.Name} (TypeKind: {type.TypeKind})");
        }
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateCorrectCode_ForNonNullableEnum()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("Status", enumType);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        var expected = "new AttributeValue { S = model.Status.ToString() }";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateCorrectCode_ForNullableEnum()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("OptionalStatus", enumType, isNullable: true);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        // Nullable enum properties return null to trigger conditional assignment for sparse GSI compatibility
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForNonNullableEnum()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("Status", enumType);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        var expected = "record.TryGetString(\"Status\", out var statusStr) && Enum.TryParse<TestNamespace.Priority>(statusStr, out var statusEnum) ? statusEnum : MissingAttributeException.Throw<TestNamespace.Priority>(\"Status\", pkValue, skValue)";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForNullableEnum()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("OptionalStatus", enumType, isNullable: true);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        var expected = "record.TryGetNullableString(\"OptionalStatus\", out var optionalstatusStr) && Enum.TryParse<TestNamespace.Priority>(optionalstatusStr, out var optionalstatusEnum) ? optionalstatusEnum : null";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateKeyFormatting_ShouldGenerateCorrectCode()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("Status", enumType);
        
        var result = _handler.GenerateKeyFormatting(property);
        
        var expected = "model.Status.ToString()";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Priority_ShouldReturnCorrectValue()
    {
        await Assert.That(_handler.Priority).IsEqualTo(120);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldThrowMissingAttributeException_ForNonNullableEnum()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "UserStatus", 
            "TestNamespace.UserStatus", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("Status", enumType, isNullable: false);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains("MissingAttributeException.Throw<TestNamespace.UserStatus>")
            .Because("Non-nullable enum should throw MissingAttributeException when missing or invalid");
            
        await Assert.That(result)
            .DoesNotContain("default(")
            .Because("Should not use default value for missing non-nullable enum");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldReturnNull_ForNullableEnumMissingValue()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "UserStatus", 
            "TestNamespace.UserStatus", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("OptionalStatus", enumType, isNullable: true);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains(": null")
            .Because("Nullable enum should return null for missing or invalid values");
            
        await Assert.That(result)
            .DoesNotContain("MissingAttributeException")
            .Because("Nullable enum should not throw exception for missing values");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldUseEnumTryParse()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "DocumentStatus", 
            "TestNamespace.DocumentStatus", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("Status", enumType);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains("Enum.TryParse<TestNamespace.DocumentStatus>")
            .Because("Should use Enum.TryParse for safe enum parsing");
            
        await Assert.That(result)
            .Contains("TryGetString")
            .Because("Should first try to get string value from DynamoDB record");
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldUseToString_ForSerialization()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("Level", enumType);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        await Assert.That(result)
            .Contains("model.Level.ToString()")
            .Because("Enum serialization should use ToString() to get string representation");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateUniqueVariableNames()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var property = TestModelBuilders.CreatePropertyInfo("MyComplexPropertyName", enumType);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains("mycomplexpropertynameStr")
            .Because("String variable should be generated from property name");
            
        await Assert.That(result)
            .Contains("mycomplexpropertynameEnum")
            .Because("Enum variable should be generated from property name");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldHandleSpecialCharacters_InPropertyNames()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Status", 
            "TestNamespace.Status", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
            
        var specialPropertyNames = new[] { "Property_With_Underscores", "PropertyWith123Numbers", "UPPER_CASE_PROP" };

        foreach (var propName in specialPropertyNames)
        {
            var property = TestModelBuilders.CreatePropertyInfo(propName, enumType);
            var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
            
            await Assert.That(result)
                .Contains($"\"{propName}\"")
                .Because($"FromDynamoRecord should correctly quote property name '{propName}'");
                
            await Assert.That(result)
                .Contains($"{propName.ToLowerInvariant()}Str")
                .Because($"FromDynamoRecord should generate lowercase variable name for '{propName}'");
        }
    }
}