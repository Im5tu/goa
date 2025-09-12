using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

public class TimeOnlyTypeHandlerTests
{
    private readonly TimeOnlyTypeHandler _handler;

    public TimeOnlyTypeHandlerTests()
    {
        _handler = new TimeOnlyTypeHandler();
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForTimeOnlyType()
    {
        var property = TestModelBuilders.CreatePropertyInfo("TimeProp", MockSymbolFactory.PrimitiveTypes.TimeOnly);
        
        var canHandle = _handler.CanHandle(property);
        
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForNullableTimeOnlyType()
    {
        var property = TestModelBuilders.CreatePropertyInfo("TimeProp", MockSymbolFactory.PrimitiveTypes.TimeOnly, isNullable: true);
        
        var canHandle = _handler.CanHandle(property);
        
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task CanHandle_ShouldReturnFalse_ForNonTimeOnlyTypes()
    {
        var nonTimeOnlyTypes = new[]
        {
            MockSymbolFactory.PrimitiveTypes.DateTime,
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Int32,
            MockSymbolFactory.PrimitiveTypes.DateOnly
        };

        foreach (var type in nonTimeOnlyTypes)
        {
            var property = TestModelBuilders.CreatePropertyInfo($"Test{type.Name}Prop", type);
            var canHandle = _handler.CanHandle(property);
            
            await Assert.That(canHandle)
                .IsFalse()
                .Because($"TimeOnlyTypeHandler should not handle {type.Name}");
        }
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateCorrectCode_ForNonNullableTimeOnly()
    {
        var property = TestModelBuilders.CreatePropertyInfo("StartTime", MockSymbolFactory.PrimitiveTypes.TimeOnly);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        var expected = "new AttributeValue { S = model.StartTime.ToString(\"HH:mm:ss.fffffff\") }";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateCorrectCode_ForNullableTimeOnly()
    {
        var property = TestModelBuilders.CreatePropertyInfo("OptionalTime", MockSymbolFactory.PrimitiveTypes.TimeOnly, isNullable: true);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        // Nullable TimeOnly properties return null to trigger conditional assignment for sparse GSI compatibility
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForNonNullableTimeOnly()
    {
        var property = TestModelBuilders.CreatePropertyInfo("StartTime", MockSymbolFactory.PrimitiveTypes.TimeOnly);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        var expected = "record.TryGetString(\"StartTime\", out var starttimeStr) && TimeOnly.TryParse(starttimeStr, out var starttime) ? starttime : MissingAttributeException.Throw<TimeOnly>(\"StartTime\", pkValue, skValue)";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForNullableTimeOnly()
    {
        var property = TestModelBuilders.CreatePropertyInfo("OptionalTime", MockSymbolFactory.PrimitiveTypes.TimeOnly, isNullable: true);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        var expected = "record.TryGetNullableString(\"OptionalTime\", out var optionaltimeStr) && TimeOnly.TryParse(optionaltimeStr, out var optionaltime) ? optionaltime : (TimeOnly?)null";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateKeyFormatting_ShouldGenerateCorrectCode()
    {
        var property = TestModelBuilders.CreatePropertyInfo("StartTime", MockSymbolFactory.PrimitiveTypes.TimeOnly);
        
        var result = _handler.GenerateKeyFormatting(property);
        
        var expected = "model.StartTime.ToString(\"HH:mm:ss.fffffff\")";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Priority_ShouldReturnCorrectValue()
    {
        await Assert.That(_handler.Priority).IsEqualTo(150);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldUseTimeOnlyParse()
    {
        var property = TestModelBuilders.CreatePropertyInfo("TestTime", MockSymbolFactory.PrimitiveTypes.TimeOnly);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains("TimeOnly.TryParse")
            .Because("TimeOnly should use TryParse for parsing");
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldUseHighPrecisionFormat()
    {
        var property = TestModelBuilders.CreatePropertyInfo("TestTime", MockSymbolFactory.PrimitiveTypes.TimeOnly);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        await Assert.That(result)
            .Contains("\"HH:mm:ss.fffffff\"")
            .Because("TimeOnly should use high-precision format for serialization");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldThrowMissingAttributeException_ForNonNullable()
    {
        var property = TestModelBuilders.CreatePropertyInfo("RequiredTime", MockSymbolFactory.PrimitiveTypes.TimeOnly, isNullable: false);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains("MissingAttributeException.Throw<TimeOnly>")
            .Because("Non-nullable TimeOnly should throw MissingAttributeException when missing");
            
        await Assert.That(result)
            .DoesNotContain("default(")
            .Because("Should not use default value for missing non-nullable TimeOnly");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldReturnNull_ForNullableMissingValue()
    {
        var property = TestModelBuilders.CreatePropertyInfo("OptionalTime", MockSymbolFactory.PrimitiveTypes.TimeOnly, isNullable: true);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains("(TimeOnly?)null")
            .Because("Nullable TimeOnly should return null for missing values");
            
        await Assert.That(result)
            .DoesNotContain("MissingAttributeException")
            .Because("Nullable TimeOnly should not throw exception for missing values");
    }

    [Test]
    public async Task TimeFormat_ShouldIncludeFractionalSeconds()
    {
        // Test that the format includes fractional seconds for high precision
        var property = TestModelBuilders.CreatePropertyInfo("PreciseTime", MockSymbolFactory.PrimitiveTypes.TimeOnly);
        
        var toAttributeResult = _handler.GenerateToAttributeValue(property);
        var fromRecordResult = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        var keyFormatResult = _handler.GenerateKeyFormatting(property);
        
        var expectedFormat = "HH:mm:ss.fffffff";
        
        await Assert.That(toAttributeResult)
            .Contains(expectedFormat)
            .Because("ToAttributeValue should use high-precision time format");
            
        await Assert.That(fromRecordResult)
            .Contains("TimeOnly.TryParse")
            .Because("FromDynamoRecord should use TryParse method");
            
        await Assert.That(keyFormatResult)
            .Contains(expectedFormat)
            .Because("KeyFormatting should use high-precision time format");
    }
}