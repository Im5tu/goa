using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

public class DateTimeTypeHandlerTests
{
    private readonly DateTimeTypeHandler _handler = new();

    [Test]
    public async Task Priority_ShouldBe150()
    {
        await Assert.That(_handler.Priority)
            .IsEqualTo(150);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task CanHandle_WithDateTime_WithoutUnixTimestampAttribute_ShouldReturnTrue(bool isNullable)
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: isNullable
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task CanHandle_WithDateTimeOffset_WithoutUnixTimestampAttribute_ShouldReturnTrue(bool isNullable)
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: isNullable
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task CanHandle_WithDateTime_WithUnixTimestampAttribute_ShouldReturnFalse()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false,
            format: Models.UnixTimestampFormat.Seconds
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task CanHandle_WithDateTimeOffset_WithUnixTimestampAttribute_ShouldReturnFalse()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: false,
            format: Models.UnixTimestampFormat.Milliseconds
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task CanHandle_WithNonDateTimeType_ShouldReturnFalse()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Name",
            MockSymbolFactory.PrimitiveTypes.String
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task GenerateToAttributeValue_NonNullableDateTime_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "new AttributeValue { S = model.CreatedAt.ToString(\"o\") }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_NullableDateTime_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: true
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "model.UpdatedAt.HasValue ? new AttributeValue { S = model.UpdatedAt.Value.ToString(\"o\") } : new AttributeValue { NULL = true }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_NonNullableDateTimeOffset_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: false
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "new AttributeValue { S = model.CreatedAt.ToString(\"o\") }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_NullableDateTimeOffset_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: true
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "model.UpdatedAt.HasValue ? new AttributeValue { S = model.UpdatedAt.Value.ToString(\"o\") } : new AttributeValue { NULL = true }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableDateTime_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetDateTime(\"CreatedAt\", out var createdat) ? createdat : MissingAttributeException.Throw<DateTime>(\"CreatedAt\", pk, sk)";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NullableDateTime_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: true
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetNullableDateTime(\"UpdatedAt\", out var updatedat) ? updatedat : null";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableDateTimeOffset_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: false
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetDateTimeOffset(\"CreatedAt\", out var createdat) ? createdat : MissingAttributeException.Throw<DateTimeOffset>(\"CreatedAt\", pk, sk)";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NullableDateTimeOffset_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: true
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetNullableDateTimeOffset(\"UpdatedAt\", out var updatedat) ? updatedat : null";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateKeyFormatting_DateTime_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false
        );

        // Act
        var result = _handler.GenerateKeyFormatting(propertyInfo);

        // Assert
        var expected = "model.CreatedAt.ToString(\"o\")";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateKeyFormatting_DateTimeOffset_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: false
        );

        // Act
        var result = _handler.GenerateKeyFormatting(propertyInfo);

        // Assert
        var expected = "model.UpdatedAt.ToString(\"o\")";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_UnsupportedType_ShouldReturnDefault()
    {
        // Arrange - Create a mock property with an unsupported type but that passes CanHandle
        var unsupportedType = MockSymbolFactory.CreateNamedTypeSymbol(
            "UnsupportedType",
            "TestNamespace.UnsupportedType",
            "TestNamespace"
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "UnsupportedProperty",
            unsupportedType,
            isNullable: false
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "default(TestNamespace.UnsupportedType)";
        await Assert.That(result)
            .IsEqualTo(expected);
    }
}