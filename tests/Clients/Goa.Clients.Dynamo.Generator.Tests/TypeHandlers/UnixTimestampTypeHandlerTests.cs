using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

public class UnixTimestampTypeHandlerTests
{
    private readonly UnixTimestampTypeHandler _handler = new();

    [Test]
    public async Task Priority_ShouldBe200()
    {
        await Assert.That(_handler.Priority)
            .IsEqualTo(200);
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task CanHandle_WithUnixTimestampAttributeAndDateTime_ShouldReturnTrue(bool isNullable)
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: isNullable,
            format: Models.UnixTimestampFormat.Seconds
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
    public async Task CanHandle_WithUnixTimestampAttributeAndDateTimeOffset_ShouldReturnTrue(bool isNullable)
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: isNullable,
            format: Models.UnixTimestampFormat.Milliseconds
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task CanHandle_WithoutUnixTimestampAttribute_ShouldReturnFalse()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task CanHandle_WithUnixTimestampAttributeButInvalidType_ShouldReturnFalse()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "InvalidProperty",
            MockSymbolFactory.PrimitiveTypes.String,
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
    public async Task GenerateToAttributeValue_NonNullableDateTime_SecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false,
            format: Models.UnixTimestampFormat.Seconds
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "new AttributeValue { N = ((DateTimeOffset)model.CreatedAt).ToUnixTimeSeconds().ToString() }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_NonNullableDateTime_MillisecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false,
            format: Models.UnixTimestampFormat.Milliseconds
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "new AttributeValue { N = ((DateTimeOffset)model.CreatedAt).ToUnixTimeMilliseconds().ToString() }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_NullableDateTime_SecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: true,
            format: Models.UnixTimestampFormat.Seconds
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "model.UpdatedAt.HasValue ? new AttributeValue { N = ((DateTimeOffset)model.UpdatedAt.Value).ToUnixTimeSeconds().ToString() } : new AttributeValue { NULL = true }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_NullableDateTime_MillisecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: true,
            format: Models.UnixTimestampFormat.Milliseconds
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "model.UpdatedAt.HasValue ? new AttributeValue { N = ((DateTimeOffset)model.UpdatedAt.Value).ToUnixTimeMilliseconds().ToString() } : new AttributeValue { NULL = true }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableDateTime_SecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false,
            format: Models.UnixTimestampFormat.Seconds
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetUnixTimestampSeconds(\"CreatedAt\", out var createdat) ? createdat : MissingAttributeException.Throw<DateTime>(\"CreatedAt\", pk, sk)";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableDateTime_MillisecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false,
            format: Models.UnixTimestampFormat.Milliseconds
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetUnixTimestampMilliseconds(\"CreatedAt\", out var createdat) ? createdat : MissingAttributeException.Throw<DateTime>(\"CreatedAt\", pk, sk)";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NullableDateTime_SecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: true,
            format: Models.UnixTimestampFormat.Seconds
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetNullableUnixTimestampSeconds(\"UpdatedAt\", out var updatedat) ? updatedat : null";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NullableDateTime_MillisecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: true,
            format: Models.UnixTimestampFormat.Milliseconds
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetNullableUnixTimestampMilliseconds(\"UpdatedAt\", out var updatedat) ? updatedat : null";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableDateTimeOffset_SecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: false,
            format: Models.UnixTimestampFormat.Seconds
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetUnixTimestampSecondsAsOffset(\"CreatedAt\", out var createdat) ? createdat : MissingAttributeException.Throw<DateTimeOffset>(\"CreatedAt\", pk, sk)";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableDateTimeOffset_MillisecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: false,
            format: Models.UnixTimestampFormat.Milliseconds
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetUnixTimestampMillisecondsAsOffset(\"CreatedAt\", out var createdat) ? createdat : MissingAttributeException.Throw<DateTimeOffset>(\"CreatedAt\", pk, sk)";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NullableDateTimeOffset_SecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: true,
            format: Models.UnixTimestampFormat.Seconds
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetNullableUnixTimestampSecondsAsOffset(\"UpdatedAt\", out var updatedat) ? updatedat : null";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NullableDateTimeOffset_MillisecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "UpdatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTimeOffset,
            isNullable: true,
            format: Models.UnixTimestampFormat.Milliseconds
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetNullableUnixTimestampMillisecondsAsOffset(\"UpdatedAt\", out var updatedat) ? updatedat : null";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateKeyFormatting_SecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false,
            format: Models.UnixTimestampFormat.Seconds
        );

        // Act
        var result = _handler.GenerateKeyFormatting(propertyInfo);

        // Assert
        var expected = "((DateTimeOffset)model.CreatedAt).ToUnixTimeSeconds().ToString()";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateKeyFormatting_MillisecondsFormat_ShouldGenerateCorrectCode()
    {
        // Arrange
        var propertyInfo = TestModelBuilders.CreateUnixTimestampPropertyInfo(
            "CreatedAt",
            MockSymbolFactory.PrimitiveTypes.DateTime,
            isNullable: false,
            format: Models.UnixTimestampFormat.Milliseconds
        );

        // Act
        var result = _handler.GenerateKeyFormatting(propertyInfo);

        // Assert
        var expected = "((DateTimeOffset)model.CreatedAt).ToUnixTimeMilliseconds().ToString()";
        await Assert.That(result)
            .IsEqualTo(expected);
    }
}