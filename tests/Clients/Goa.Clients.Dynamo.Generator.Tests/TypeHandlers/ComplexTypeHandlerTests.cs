using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

public class ComplexTypeHandlerTests
{
    private readonly ComplexTypeHandler _handler;
    private readonly TypeHandlerRegistry _registry;

    public ComplexTypeHandlerTests()
    {
        _handler = new ComplexTypeHandler();
        _registry = CreateTypeHandlerRegistry();
        _handler.SetRegistry(_registry);
    }

    [Test]
    public async Task Priority_ShouldBe50()
    {
        await Assert.That(_handler.Priority)
            .IsEqualTo(50);
    }

    [Test]
    public async Task CanHandle_WithComplexClass_ShouldReturnTrue()
    {
        // Arrange
        var complexType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Address",
            "TestNamespace.Address",
            "TestNamespace",
            typeKind: TypeKind.Class
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "HomeAddress",
            complexType
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task CanHandle_WithComplexRecord_ShouldReturnTrue()
    {
        // Arrange
        var recordType = MockSymbolFactory.CreateNamedTypeSymbol(
            "UserProfile",
            "TestNamespace.UserProfile",
            "TestNamespace",
            isRecord: true
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Profile",
            recordType
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task CanHandle_WithComplexStruct_ShouldReturnTrue()
    {
        // Arrange
        var structType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Coordinates",
            "TestNamespace.Coordinates",
            "TestNamespace",
            typeKind: TypeKind.Struct
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Location",
            structType
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task CanHandle_WithStringKeyedDictionary_ShouldReturnTrue()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.String
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Metadata",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.String)
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task CanHandle_WithNonStringKeyedDictionary_ShouldReturnFalse()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.Int32,
            MockSymbolFactory.PrimitiveTypes.String
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "InvalidDict",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.Int32, MockSymbolFactory.PrimitiveTypes.String)
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task CanHandle_WithPrimitiveType_ShouldReturnFalse()
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
    public async Task CanHandle_WithCollection_ShouldReturnFalse()
    {
        // Arrange
        var listType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Tags",
            listType,
            isCollection: true,
            elementType: MockSymbolFactory.PrimitiveTypes.String
        );

        // Act
        var result = _handler.CanHandle(propertyInfo);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task GenerateToAttributeValue_ComplexType_ShouldGenerateCorrectCode()
    {
        // Arrange
        var complexType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Address",
            "TestNamespace.Address",
            "TestNamespace",
            typeKind: TypeKind.Class
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "HomeAddress",
            complexType
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "model.HomeAddress != null ? new AttributeValue { M = DynamoMapper.Address.ToDynamoRecord(model.HomeAddress) } : new AttributeValue { NULL = true }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_StringStringDictionary_ShouldGenerateCorrectCode()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.String
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Metadata",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.String)
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "model.Metadata != null ? new AttributeValue { M = model.Metadata.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue { S = kvp.Value }) } : new AttributeValue { NULL = true }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_StringIntDictionary_ShouldGenerateCorrectCode()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Int32
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Scores",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.Int32)
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "model.Scores != null ? new AttributeValue { M = model.Scores.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue { N = kvp.Value.ToString() }) } : new AttributeValue { NULL = true }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateToAttributeValue_StringListDictionary_ShouldGenerateCorrectCode()
    {
        // Arrange
        var listType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String
        );
        
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            listType.Object
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "TagsByCategory",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, listType.Object)
        );

        // Act
        var result = _handler.GenerateToAttributeValue(propertyInfo);

        // Assert
        var expected = "model.TagsByCategory != null ? new AttributeValue { M = model.TagsByCategory.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue { SS = kvp.Value ?? new List<string>() }) } : new AttributeValue { NULL = true }";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ComplexType_NonNullable_ShouldGenerateCorrectCode()
    {
        // Arrange
        var complexType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Address",
            "TestNamespace.Address",
            "TestNamespace",
            typeKind: TypeKind.Class
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "HomeAddress",
            complexType,
            isNullable: false
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetMap(\"HomeAddress\", out var homeaddressMap) && homeaddressMap != null ? DynamoMapper.Address.FromDynamoRecord(homeaddressMap, pk, sk) : MissingAttributeException.Throw<TestNamespace.Address>(\"HomeAddress\", pk, sk)";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ComplexType_Nullable_ShouldGenerateCorrectCode()
    {
        // Arrange
        var complexType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Address",
            "TestNamespace.Address",
            "TestNamespace",
            typeKind: TypeKind.Class
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "MailingAddress",
            complexType,
            isNullable: true
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        var expected = "record.TryGetMap(\"MailingAddress\", out var mailingaddressMap) && mailingaddressMap != null ? DynamoMapper.Address.FromDynamoRecord(mailingaddressMap, pk, sk) : null";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_StringStringDictionary_ShouldGenerateCorrectCode()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.String
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Metadata",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.String)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert - non-nullable dictionaries replace ": null" with ": new Dictionary<>()"
        var expected = "(record.TryGetStringDictionary(\"Metadata\", out var metadata) ? metadata : new Dictionary<string, string>())";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_StringIntDictionary_ShouldGenerateCorrectCode()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Int32
        ).Object;
        
        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Scores",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.Int32)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert - non-nullable dictionaries replace ": null" with ": new Dictionary<>()"
        var expected = "(record.TryGetStringIntDictionary(\"Scores\", out var scores) ? scores : new Dictionary<string, int>())";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateFromDynamoRecord_StringListDictionary_ShouldGenerateCorrectCode()
    {
        // Arrange
        var listType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String
        );

        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            listType.Object
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "TagsByCategory",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, listType.Object)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert - After the fix, non-nullable dictionaries replace ": null" with ": new Dictionary<>()"
        var expected = "(record.TryGetMap(\"TagsByCategory\", out var tagsbycategoryMap) && tagsbycategoryMap != null ? tagsbycategoryMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.SS?.ToList() ?? new List<string>()) : new Dictionary<string, System.Collections.Generic.List<string>>())";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    [Test]
    public async Task GenerateKeyFormatting_ComplexType_ShouldGenerateCorrectCode()
    {
        // Arrange
        var complexType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Address",
            "TestNamespace.Address",
            "TestNamespace",
            typeKind: TypeKind.Class
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "HomeAddress",
            complexType
        );

        // Act
        var result = _handler.GenerateKeyFormatting(propertyInfo);

        // Assert
        var expected = "model.HomeAddress?.ToString() ?? \"\"";
        await Assert.That(result)
            .IsEqualTo(expected);
    }

    #region Nullable Dictionary Handling Tests

    [Test]
    public async Task GenerateFromDynamoRecord_NullableStringDoubleDictionary_ShouldReturnNull()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Double
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Measurements",
            dictType,
            isNullable: true,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.Double)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains(": null")
            .Because("Nullable dictionary should return null when missing");

        await Assert.That(result)
            .DoesNotContain("new Dictionary")
            .Because("Nullable dictionary should not fall back to empty dictionary");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableStringDoubleDictionary_ShouldReturnEmptyDict()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Double
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Measurements",
            dictType,
            isNullable: false,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.Double)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains("new Dictionary<string, double>")
            .Because("Non-nullable dictionary should fall back to empty dictionary");

        await Assert.That(result)
            .DoesNotContain(": null")
            .Because("Non-nullable dictionary should not return null");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NullableStringDateTimeDictionary_ShouldReturnNull()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.DateTime
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "ImportantDates",
            dictType,
            isNullable: true,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.DateTime)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains(": null")
            .Because("Nullable DateTime dictionary should return null when missing");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableStringDateTimeDictionary_ShouldReturnEmptyDict()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.DateTime
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "ImportantDates",
            dictType,
            isNullable: false,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.DateTime)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains("new Dictionary<string, System.DateTime>")
            .Because("Non-nullable DateTime dictionary should fall back to empty dictionary");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NullableStringEnumDictionary_ShouldReturnNull()
    {
        // Arrange
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority",
            "TestNamespace.Priority",
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;

        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            enumType
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "TaskPriorities",
            dictType,
            isNullable: true,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, enumType)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains(": null")
            .Because("Nullable enum dictionary should return null when missing");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableStringEnumDictionary_ShouldReturnEmptyDict()
    {
        // Arrange
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority",
            "TestNamespace.Priority",
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;

        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            enumType
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "TaskPriorities",
            dictType,
            isNullable: false,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, enumType)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains("new Dictionary<string, TestNamespace.Priority>")
            .Because("Non-nullable enum dictionary should fall back to empty dictionary");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NullableNestedDictionary_ShouldReturnNull()
    {
        // Arrange
        var innerDictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.String
        );

        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            innerDictType.Object
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "NestedConfig",
            dictType,
            isNullable: true,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, innerDictType.Object)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains(": null")
            .Because("Nullable nested dictionary should return null when missing");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NonNullableNestedDictionary_ShouldReturnEmptyDict()
    {
        // Arrange
        var innerDictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.String
        );

        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            innerDictType.Object
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "NestedConfig",
            dictType,
            isNullable: false,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, innerDictType.Object)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains("new Dictionary<string, System.Collections.Generic.Dictionary<string, string>>")
            .Because("Non-nullable nested dictionary should fall back to empty dictionary");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_DoubleDictionary_ShouldUseCultureInvariantParsing()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Double
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "Measurements",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.Double)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains("System.Globalization.CultureInfo.InvariantCulture")
            .Because("Double parsing should use InvariantCulture to avoid locale issues");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_DateTimeDictionary_ShouldUseDateTimeParseExact()
    {
        // Arrange
        var dictType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.DateTime
        ).Object;

        var propertyInfo = TestModelBuilders.CreatePropertyInfo(
            "ImportantDates",
            dictType,
            isDictionary: true,
            dictionaryTypes: (MockSymbolFactory.PrimitiveTypes.String, MockSymbolFactory.PrimitiveTypes.DateTime)
        );

        // Act
        var result = _handler.GenerateFromDynamoRecord(propertyInfo, "record", "pk", "sk");

        // Assert
        await Assert.That(result)
            .Contains("DateTime.ParseExact")
            .Because("DateTime parsing should use ParseExact for precise round-trip formatting");

        await Assert.That(result)
            .Contains("\"o\"")
            .Because("DateTime should use roundtrip format");

        await Assert.That(result)
            .Contains("System.Globalization.DateTimeStyles.RoundtripKind")
            .Because("DateTime should preserve UTC/Local kind information");
    }

    #endregion

    private static TypeHandlerRegistry CreateTypeHandlerRegistry()
    {
        var registry = new TypeHandlerRegistry();
        registry.RegisterHandler(new PrimitiveTypeHandler());
        registry.RegisterHandler(new EnumTypeHandler());
        registry.RegisterHandler(new DateOnlyTypeHandler());
        registry.RegisterHandler(new TimeOnlyTypeHandler());
        var collectionHandler = new CollectionTypeHandler();
        collectionHandler.SetRegistry(registry);
        registry.RegisterHandler(collectionHandler);
        registry.RegisterHandler(new UnixTimestampTypeHandler());
        var complexHandler = new ComplexTypeHandler();
        complexHandler.SetRegistry(registry);
        registry.RegisterHandler(complexHandler);
        return registry;
    }
}