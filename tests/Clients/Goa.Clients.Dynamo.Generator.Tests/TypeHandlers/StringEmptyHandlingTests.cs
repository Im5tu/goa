using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

/// <summary>
/// Tests for the string empty-string handling fix.
/// Ensures that both nullable and non-nullable strings use conditional assignment
/// to skip empty strings when writing to DynamoDB.
/// </summary>
public class StringEmptyHandlingTests
{
    private readonly PrimitiveTypeHandler _handler;

    public StringEmptyHandlingTests()
    {
        _handler = new PrimitiveTypeHandler();
    }

    [Test]
    public async Task GenerateToAttributeValue_NonNullableString_ShouldReturnNull()
    {
        // Arrange
        var property = TestModelBuilders.CreatePropertyInfo(
            "Name",
            MockSymbolFactory.PrimitiveTypes.String,
            isNullable: false);

        // Act
        var result = _handler.GenerateToAttributeValue(property);

        // Assert
        await Assert.That(result)
            .IsNull()
            .Because("Non-nullable strings should use conditional assignment to skip empty strings");
    }

    [Test]
    public async Task GenerateToAttributeValue_NullableString_ShouldReturnNull()
    {
        // Arrange
        var property = TestModelBuilders.CreatePropertyInfo(
            "Description",
            MockSymbolFactory.PrimitiveTypes.String,
            isNullable: true);

        // Act
        var result = _handler.GenerateToAttributeValue(property);

        // Assert
        await Assert.That(result)
            .IsNull()
            .Because("Nullable strings should use conditional assignment to skip empty strings");
    }

    [Test]
    public async Task GenerateConditionalAssignment_NonNullableString_ShouldCheckIsNullOrEmpty()
    {
        // Arrange
        var property = TestModelBuilders.CreatePropertyInfo(
            "Name",
            MockSymbolFactory.PrimitiveTypes.String,
            isNullable: false);

        // Act
        var result = _handler.GenerateConditionalAssignment(property, "record");

        // Assert
        await Assert.That(result)
            .IsNotNull()
            .Because("Non-nullable strings should generate conditional assignment");

        await Assert.That(result)
            .Contains("!string.IsNullOrEmpty(model.Name)")
            .Because("Non-nullable strings should check for both null and empty");

        await Assert.That(result)
            .Contains("record[\"Name\"] = new AttributeValue { S = model.Name };")
            .Because("Should assign S attribute when string is not null or empty");
    }

    [Test]
    public async Task GenerateConditionalAssignment_NullableString_ShouldCheckIsNullOrEmpty()
    {
        // Arrange
        var property = TestModelBuilders.CreatePropertyInfo(
            "Description",
            MockSymbolFactory.PrimitiveTypes.String,
            isNullable: true);

        // Act
        var result = _handler.GenerateConditionalAssignment(property, "record");

        // Assert
        await Assert.That(result)
            .IsNotNull()
            .Because("Nullable strings should generate conditional assignment");

        await Assert.That(result)
            .Contains("!string.IsNullOrEmpty(model.Description)")
            .Because("Nullable strings should check for both null and empty");

        await Assert.That(result)
            .Contains("record[\"Description\"] = new AttributeValue { S = model.Description };")
            .Because("Should assign S attribute when string is not null or empty");
    }

    [Test]
    public async Task GenerateConditionalAssignment_NonStringNonNullableType_ShouldReturnNull()
    {
        // Arrange
        var property = TestModelBuilders.CreatePropertyInfo(
            "Count",
            MockSymbolFactory.PrimitiveTypes.Int32,
            isNullable: false);

        // Act
        var result = _handler.GenerateConditionalAssignment(property, "record");

        // Assert
        await Assert.That(result)
            .IsNull()
            .Because("Non-nullable non-string types don't need conditional assignment");
    }

    [Test]
    public async Task GenerateConditionalAssignment_NullableNumericType_ShouldCheckHasValue()
    {
        // Arrange
        var property = TestModelBuilders.CreatePropertyInfo(
            "OptionalCount",
            MockSymbolFactory.PrimitiveTypes.Int32,
            isNullable: true);

        // Act
        var result = _handler.GenerateConditionalAssignment(property, "record");

        // Assert
        await Assert.That(result)
            .IsNotNull()
            .Because("Nullable numeric types should generate conditional assignment");

        await Assert.That(result)
            .Contains("model.OptionalCount.HasValue")
            .Because("Nullable numeric types should check HasValue");

        await Assert.That(result)
            .DoesNotContain("IsNullOrEmpty")
            .Because("Numeric types don't need empty check");
    }

    [Test]
    public async Task StringConditionalAssignment_ShouldNotIncludeHasValue()
    {
        // Arrange
        var nullableStringProperty = TestModelBuilders.CreatePropertyInfo(
            "Description",
            MockSymbolFactory.PrimitiveTypes.String,
            isNullable: true);

        var nonNullableStringProperty = TestModelBuilders.CreatePropertyInfo(
            "Name",
            MockSymbolFactory.PrimitiveTypes.String,
            isNullable: false);

        // Act
        var nullableResult = _handler.GenerateConditionalAssignment(nullableStringProperty, "record");
        var nonNullableResult = _handler.GenerateConditionalAssignment(nonNullableStringProperty, "record");

        // Assert
        await Assert.That(nullableResult)
            .DoesNotContain("HasValue")
            .Because("Strings don't have HasValue property");

        await Assert.That(nonNullableResult)
            .DoesNotContain("HasValue")
            .Because("Strings don't have HasValue property");
    }

    [Test]
    public async Task GenerateConditionalAssignment_MultipleStringProperties_ShouldAllUseIsNullOrEmpty()
    {
        // Arrange
        var stringProperties = new[]
        {
            ("Name", false),
            ("Description", true),
            ("Email", false),
            ("PhoneNumber", true)
        };

        foreach (var (propName, isNullable) in stringProperties)
        {
            var property = TestModelBuilders.CreatePropertyInfo(
                propName,
                MockSymbolFactory.PrimitiveTypes.String,
                isNullable: isNullable);

            // Act
            var result = _handler.GenerateConditionalAssignment(property, "record");

            // Assert
            await Assert.That(result)
                .Contains($"!string.IsNullOrEmpty(model.{propName})")
                .Because($"{(isNullable ? "Nullable" : "Non-nullable")} string property '{propName}' should check IsNullOrEmpty");
        }
    }

    [Test]
    public async Task GenerateConditionalAssignment_CharType_ShouldNotUseIsNullOrEmpty()
    {
        // Arrange - char is a primitive but not a string
        var property = TestModelBuilders.CreatePropertyInfo(
            "Initial",
            MockSymbolFactory.PrimitiveTypes.Char,
            isNullable: false);

        // Act
        var result = _handler.GenerateConditionalAssignment(property, "record");

        // Assert - Char should not use IsNullOrEmpty since it's not a string
        await Assert.That(result)
            .IsNull()
            .Because("Non-nullable char doesn't need conditional assignment");
    }

    [Test]
    public async Task GenerateConditionalAssignment_NullableChar_ShouldCheckHasValue()
    {
        // Arrange
        var property = TestModelBuilders.CreatePropertyInfo(
            "OptionalInitial",
            MockSymbolFactory.PrimitiveTypes.Char,
            isNullable: true);

        // Act
        var result = _handler.GenerateConditionalAssignment(property, "record");

        // Assert
        await Assert.That(result)
            .Contains("model.OptionalInitial.HasValue")
            .Because("Nullable char should check HasValue, not IsNullOrEmpty");

        await Assert.That(result)
            .DoesNotContain("IsNullOrEmpty")
            .Because("Char types don't use IsNullOrEmpty");
    }
}
