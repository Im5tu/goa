using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

public class PrimitiveTypeHandlerTests
{
    private readonly PrimitiveTypeHandler _handler;

    public PrimitiveTypeHandlerTests()
    {
        _handler = new PrimitiveTypeHandler();
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForAllPrimitiveTypes()
    {
        // Test all primitive types
        var primitiveTypes = new[]
        {
            (MockSymbolFactory.PrimitiveTypes.Boolean, "bool"),
            (MockSymbolFactory.PrimitiveTypes.Byte, "byte"),
            (MockSymbolFactory.PrimitiveTypes.SByte, "sbyte"),
            (MockSymbolFactory.PrimitiveTypes.Char, "char"),
            (MockSymbolFactory.PrimitiveTypes.Int16, "short"),
            (MockSymbolFactory.PrimitiveTypes.UInt16, "ushort"),
            (MockSymbolFactory.PrimitiveTypes.Int32, "int"),
            (MockSymbolFactory.PrimitiveTypes.UInt32, "uint"),
            (MockSymbolFactory.PrimitiveTypes.Int64, "long"),
            (MockSymbolFactory.PrimitiveTypes.UInt64, "ulong"),
            (MockSymbolFactory.PrimitiveTypes.Single, "float"),
            (MockSymbolFactory.PrimitiveTypes.Double, "double"),
            (MockSymbolFactory.PrimitiveTypes.Decimal, "decimal"),
            (MockSymbolFactory.PrimitiveTypes.String, "string"),
            (MockSymbolFactory.PrimitiveTypes.Guid, "Guid")
        };

        foreach (var (type, typeName) in primitiveTypes)
        {
            var property = TestModelBuilders.CreatePropertyInfo($"Test{typeName}Prop", type);
            var canHandle = _handler.CanHandle(property);
            
            await Assert.That(canHandle)
                .IsTrue()
                .Because($"PrimitiveTypeHandler should handle {typeName}");
        }
    }

    [Test]
    public async Task CanHandle_ShouldReturnFalse_ForNonPrimitiveTypes()
    {
        // Test non-primitive types
        var customType = MockSymbolFactory.CreateNamedTypeSymbol("CustomClass", "TestNamespace.CustomClass", "TestNamespace").Object;
        var property = TestModelBuilders.CreatePropertyInfo("TestProp", customType);
        
        var canHandle = _handler.CanHandle(property);
        
        await Assert.That(canHandle).IsFalse();
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateCorrectCode_ForNonNullableTypes()
    {
        var testCases = new[]
        {
            (MockSymbolFactory.PrimitiveTypes.Boolean, "BoolProp", "new AttributeValue { BOOL = model.BoolProp }"),
            (MockSymbolFactory.PrimitiveTypes.Int32, "IntProp", "new AttributeValue { N = model.IntProp.ToString() }"),
            (MockSymbolFactory.PrimitiveTypes.String, "StringProp", "new AttributeValue { S = model.StringProp }"),
            (MockSymbolFactory.PrimitiveTypes.Decimal, "DecimalProp", "new AttributeValue { N = model.DecimalProp.ToString() }"),
            (MockSymbolFactory.PrimitiveTypes.Double, "DoubleProp", "new AttributeValue { N = model.DoubleProp.ToString() }"),
            (MockSymbolFactory.PrimitiveTypes.Guid, "GuidProp", "new AttributeValue { S = model.GuidProp.ToString() }")
        };

        foreach (var (type, propName, expectedCode) in testCases)
        {
            var property = TestModelBuilders.CreatePropertyInfo(propName, type, isNullable: false);
            var result = _handler.GenerateToAttributeValue(property);
            
            await Assert.That(result)
                .IsEqualTo(expectedCode)
                .Because($"ToAttributeValue for non-nullable {type.Name} should generate correct code");
        }
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateCorrectCode_ForNullableTypes()
    {
        var testCases = new[]
        {
            (MockSymbolFactory.PrimitiveTypes.Boolean, "BoolProp", (string?)null), // Nullable primitives return null for conditional assignment
            (MockSymbolFactory.PrimitiveTypes.Int32, "IntProp", (string?)null),     // Nullable primitives return null for conditional assignment
            (MockSymbolFactory.PrimitiveTypes.String, "StringProp", (string?)null)  // Nullable strings also return null for conditional assignment
        };

        foreach (var (type, propName, expectedCode) in testCases)
        {
            var property = TestModelBuilders.CreatePropertyInfo(propName, type, isNullable: true);
            var result = _handler.GenerateToAttributeValue(property);
            
            await Assert.That(result)
                .IsEqualTo(expectedCode)
                .Because($"ToAttributeValue for nullable {type.Name} should return {(expectedCode == null ? "null to trigger conditional assignment" : "direct assignment")}");
        }
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForNonNullableTypes()
    {
        var testCases = new[]
        {
            (MockSymbolFactory.PrimitiveTypes.Boolean, "BoolProp", 
             "record.TryGetBool(\"BoolProp\", out var boolprop) ? boolprop : MissingAttributeException.Throw<bool>(\"BoolProp\", pkValue, skValue)"),
            (MockSymbolFactory.PrimitiveTypes.Int32, "IntProp", 
             "record.TryGetInt(\"IntProp\", out var intprop) ? intprop : MissingAttributeException.Throw<int>(\"IntProp\", pkValue, skValue)"),
            (MockSymbolFactory.PrimitiveTypes.String, "StringProp", 
             "record.TryGetString(\"StringProp\", out var stringprop) ? stringprop : MissingAttributeException.Throw<string>(\"StringProp\", pkValue, skValue)"),
            (MockSymbolFactory.PrimitiveTypes.Decimal, "DecimalProp", 
             "record.TryGetDecimal(\"DecimalProp\", out var decimalprop) ? decimalprop : MissingAttributeException.Throw<decimal>(\"DecimalProp\", pkValue, skValue)")
        };

        foreach (var (type, propName, expectedCode) in testCases)
        {
            var property = TestModelBuilders.CreatePropertyInfo(propName, type, isNullable: false);
            var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
            
            await Assert.That(result)
                .IsEqualTo(expectedCode)
                .Because($"FromDynamoRecord for non-nullable {type.Name} should throw MissingAttributeException");
        }
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForNullableTypes()
    {
        var testCases = new[]
        {
            (MockSymbolFactory.PrimitiveTypes.Boolean, "BoolProp", 
             "record.TryGetNullableBool(\"BoolProp\", out var boolprop) ? boolprop : null"),
            (MockSymbolFactory.PrimitiveTypes.Int32, "IntProp", 
             "record.TryGetNullableInt(\"IntProp\", out var intprop) ? intprop : null"),
            (MockSymbolFactory.PrimitiveTypes.String, "StringProp", 
             "record.TryGetNullableString(\"StringProp\", out var stringprop) ? stringprop : null")
        };

        foreach (var (type, propName, expectedCode) in testCases)
        {
            var property = TestModelBuilders.CreatePropertyInfo(propName, type, isNullable: true);
            var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
            
            await Assert.That(result)
                .IsEqualTo(expectedCode)
                .Because($"FromDynamoRecord for nullable {type.Name} should return null for missing values");
        }
    }

    [Test]
    public async Task GenerateKeyFormatting_ShouldGenerateCorrectCode_ForAllTypes()
    {
        var testCases = new[]
        {
            (MockSymbolFactory.PrimitiveTypes.String, "StringProp", "model.StringProp?.ToString() ?? \"\""),
            (MockSymbolFactory.PrimitiveTypes.Int32, "IntProp", "model.IntProp?.ToString() ?? \"\""),
            (MockSymbolFactory.PrimitiveTypes.Boolean, "BoolProp", "model.BoolProp?.ToString() ?? \"\""),
            (MockSymbolFactory.PrimitiveTypes.Guid, "GuidProp", "model.GuidProp?.ToString() ?? \"\""),
            (MockSymbolFactory.PrimitiveTypes.Decimal, "DecimalProp", "model.DecimalProp?.ToString() ?? \"\"")
        };

        foreach (var (type, propName, expectedCode) in testCases)
        {
            var property = TestModelBuilders.CreatePropertyInfo(propName, type);
            var result = _handler.GenerateKeyFormatting(property);
            
            await Assert.That(result)
                .IsEqualTo(expectedCode)
                .Because($"KeyFormatting for {type.Name} should generate correct property access");
        }
    }

    [Test]
    public async Task Priority_ShouldReturnCorrectValue()
    {
        await Assert.That(_handler.Priority).IsEqualTo(100);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldNotUsePlaceholderDefault_ForAnyType()
    {
        // Ensure we never generate `default(T)` which was a previous issue
        var primitiveTypes = new[]
        {
            MockSymbolFactory.PrimitiveTypes.Boolean,
            MockSymbolFactory.PrimitiveTypes.Int32,
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Decimal,
            MockSymbolFactory.PrimitiveTypes.Double,
            MockSymbolFactory.PrimitiveTypes.Guid
        };

        foreach (var type in primitiveTypes)
        {
            var property = TestModelBuilders.CreatePropertyInfo($"Test{type.Name}Prop", type, isNullable: false);
            var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
            
            await Assert.That(result)
                .DoesNotContain("default(")
                .Because($"FromDynamoRecord for {type.Name} should not use default() - should throw MissingAttributeException instead");
                
            await Assert.That(result)
                .Contains("MissingAttributeException.Throw")
                .Because($"FromDynamoRecord for non-nullable {type.Name} should throw MissingAttributeException for missing attributes");
        }
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldHandleSpecialCharacters_InPropertyNames()
    {
        // Test property names that might cause issues in generated code
        var specialPropertyNames = new[] { "Property_With_Underscores", "PropertyWith123Numbers", "UPPER_CASE_PROP" };

        foreach (var propName in specialPropertyNames)
        {
            var property = TestModelBuilders.CreatePropertyInfo(propName, MockSymbolFactory.PrimitiveTypes.String);
            var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
            
            await Assert.That(result)
                .Contains($"\"{propName}\"")
                .Because($"FromDynamoRecord should correctly quote property name '{propName}'");
                
            await Assert.That(result)
                .Contains(propName.ToLowerInvariant())
                .Because($"FromDynamoRecord should generate lowercase variable name for '{propName}'");
        }
    }
}