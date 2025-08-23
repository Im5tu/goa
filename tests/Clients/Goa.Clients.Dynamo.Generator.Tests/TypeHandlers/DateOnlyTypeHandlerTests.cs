using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

public class DateOnlyTypeHandlerTests
{
    private readonly DateOnlyTypeHandler _handler;

    public DateOnlyTypeHandlerTests()
    {
        _handler = new DateOnlyTypeHandler();
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForDateOnlyType()
    {
        var property = TestModelBuilders.CreatePropertyInfo("DateProp", MockSymbolFactory.PrimitiveTypes.DateOnly);
        
        var canHandle = _handler.CanHandle(property);
        
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForNullableDateOnlyType()
    {
        var property = TestModelBuilders.CreatePropertyInfo("DateProp", MockSymbolFactory.PrimitiveTypes.DateOnly, isNullable: true);
        
        var canHandle = _handler.CanHandle(property);
        
        await Assert.That(canHandle).IsTrue();
    }

    [Test]
    public async Task CanHandle_ShouldReturnFalse_ForNonDateOnlyTypes()
    {
        var nonDateOnlyTypes = new[]
        {
            MockSymbolFactory.PrimitiveTypes.DateTime,
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Int32,
            MockSymbolFactory.PrimitiveTypes.TimeOnly
        };

        foreach (var type in nonDateOnlyTypes)
        {
            var property = TestModelBuilders.CreatePropertyInfo($"Test{type.Name}Prop", type);
            var canHandle = _handler.CanHandle(property);
            
            await Assert.That(canHandle)
                .IsFalse()
                .Because($"DateOnlyTypeHandler should not handle {type.Name}");
        }
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateCorrectCode_ForNonNullableDateOnly()
    {
        var property = TestModelBuilders.CreatePropertyInfo("CreatedDate", MockSymbolFactory.PrimitiveTypes.DateOnly);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        var expected = "new AttributeValue { S = model.CreatedDate.ToString(\"yyyy-MM-dd\") }";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateCorrectCode_ForNullableDateOnly()
    {
        var property = TestModelBuilders.CreatePropertyInfo("OptionalDate", MockSymbolFactory.PrimitiveTypes.DateOnly, isNullable: true);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        var expected = "model.OptionalDate.HasValue ? new AttributeValue { S = model.OptionalDate.Value.ToString(\"yyyy-MM-dd\") } : new AttributeValue { NULL = true }";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForNonNullableDateOnly()
    {
        var property = TestModelBuilders.CreatePropertyInfo("CreatedDate", MockSymbolFactory.PrimitiveTypes.DateOnly);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        var expected = "record.TryGetString(\"CreatedDate\", out var createddateStr) && DateOnly.TryParse(createddateStr, out var createddate) ? createddate : MissingAttributeException.Throw<DateOnly>(\"CreatedDate\", pkValue, skValue)";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForNullableDateOnly()
    {
        var property = TestModelBuilders.CreatePropertyInfo("OptionalDate", MockSymbolFactory.PrimitiveTypes.DateOnly, isNullable: true);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        var expected = "record.TryGetNullableString(\"OptionalDate\", out var optionaldateStr) && DateOnly.TryParse(optionaldateStr, out var optionaldate) ? optionaldate : (DateOnly?)null";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateKeyFormatting_ShouldGenerateCorrectCode()
    {
        var property = TestModelBuilders.CreatePropertyInfo("CreatedDate", MockSymbolFactory.PrimitiveTypes.DateOnly);
        
        var result = _handler.GenerateKeyFormatting(property);
        
        var expected = "model.CreatedDate.ToString(\"yyyy-MM-dd\")";
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Priority_ShouldReturnCorrectValue()
    {
        await Assert.That(_handler.Priority).IsEqualTo(150);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldUseCorrectParsingMethod()
    {
        var property = TestModelBuilders.CreatePropertyInfo("TestDate", MockSymbolFactory.PrimitiveTypes.DateOnly);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains("DateOnly.TryParse")
            .Because("DateOnly should use TryParse for parsing from DynamoDB records");
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldUseISO8601Format()
    {
        var property = TestModelBuilders.CreatePropertyInfo("TestDate", MockSymbolFactory.PrimitiveTypes.DateOnly);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        await Assert.That(result)
            .Contains("\"yyyy-MM-dd\"")
            .Because("DateOnly should use ISO 8601 date format for serialization");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldThrowMissingAttributeException_ForNonNullable()
    {
        var property = TestModelBuilders.CreatePropertyInfo("RequiredDate", MockSymbolFactory.PrimitiveTypes.DateOnly, isNullable: false);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains("MissingAttributeException.Throw<DateOnly>")
            .Because("Non-nullable DateOnly should throw MissingAttributeException when missing");
            
        await Assert.That(result)
            .DoesNotContain("default(")
            .Because("Should not use default value for missing non-nullable DateOnly");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldReturnNull_ForNullableMissingValue()
    {
        var property = TestModelBuilders.CreatePropertyInfo("OptionalDate", MockSymbolFactory.PrimitiveTypes.DateOnly, isNullable: true);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        await Assert.That(result)
            .Contains("(DateOnly?)null")
            .Because("Nullable DateOnly should return null for missing values");
            
        await Assert.That(result)
            .DoesNotContain("MissingAttributeException")
            .Because("Nullable DateOnly should not throw exception for missing values");
    }
}