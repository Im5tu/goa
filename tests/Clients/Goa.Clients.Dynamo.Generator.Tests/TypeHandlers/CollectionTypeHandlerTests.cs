using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;
using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Tests.TypeHandlers;

public class CollectionTypeHandlerTests
{
    private readonly CollectionTypeHandler _handler;
    private readonly TypeHandlerRegistry _registry;

    public CollectionTypeHandlerTests()
    {
        _handler = new CollectionTypeHandler();
        _registry = CreateTypeHandlerRegistry();
        _handler.SetRegistry(_registry);
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForArrayTypes()
    {
        var arrayTypes = new[]
        {
            MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.String).Object,
            MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.Int32).Object,
            MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.Double).Object,
            MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.DateTime).Object
        };

        foreach (var arrayType in arrayTypes)
        {
            var property = TestModelBuilders.CreatePropertyInfo($"Test{arrayType.ElementType.Name}Array", arrayType);
            var canHandle = _handler.CanHandle(property);
            
            await Assert.That(canHandle)
                .IsTrue()
                .Because($"CollectionTypeHandler should handle {arrayType.ElementType.Name}[] arrays");
        }
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForListTypes()
    {
        var listTypes = new[]
        {
            MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.String).Object,
            MockSymbolFactory.CreateGenericType("IList", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Int32).Object,
            MockSymbolFactory.CreateGenericType("ICollection", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Double).Object,
            MockSymbolFactory.CreateGenericType("IEnumerable", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Boolean).Object
        };

        foreach (var listType in listTypes)
        {
            var property = TestModelBuilders.CreatePropertyInfo($"Test{listType.Name}Prop", listType);
            var canHandle = _handler.CanHandle(property);
            
            await Assert.That(canHandle)
                .IsTrue()
                .Because($"CollectionTypeHandler should handle {listType.Name}<T> types");
        }
    }

    [Test]
    public async Task CanHandle_ShouldReturnTrue_ForSetTypes()
    {
        var setTypes = new[]
        {
            MockSymbolFactory.CreateGenericType("HashSet", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.String).Object,
            MockSymbolFactory.CreateGenericType("ISet", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Int32).Object,
            MockSymbolFactory.CreateGenericType("IReadOnlySet", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Double).Object
        };

        foreach (var setType in setTypes)
        {
            var property = TestModelBuilders.CreatePropertyInfo($"Test{setType.Name}Prop", setType);
            var canHandle = _handler.CanHandle(property);
            
            await Assert.That(canHandle)
                .IsTrue()
                .Because($"CollectionTypeHandler should handle {setType.Name}<T> types");
        }
    }

    [Test]
    public async Task CanHandle_ShouldReturnFalse_ForNonCollectionTypes()
    {
        var nonCollectionTypes = new[]
        {
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Int32,
            MockSymbolFactory.PrimitiveTypes.Boolean,
            MockSymbolFactory.CreateNamedTypeSymbol("CustomClass", "TestNamespace.CustomClass", "TestNamespace").Object
        };

        foreach (var type in nonCollectionTypes)
        {
            var property = TestModelBuilders.CreatePropertyInfo($"Test{type.Name}Prop", type);
            var canHandle = _handler.CanHandle(property);
            
            await Assert.That(canHandle)
                .IsFalse()
                .Because($"CollectionTypeHandler should not handle non-collection type {type.Name}");
        }
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateStringSet_ForStringCollections()
    {
        var stringCollectionTypes = new (ITypeSymbol, string)[]
        {
            (MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.String).Object, "StringArray"),
            (MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.String).Object, "StringList"),
            (MockSymbolFactory.CreateGenericType("IEnumerable", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.String).Object, "StringEnumerable")
        };

        foreach (var (collectionType, propName) in stringCollectionTypes)
        {
            var property = TestModelBuilders.CreateCollectionPropertyInfo(propName, collectionType, MockSymbolFactory.PrimitiveTypes.String);
            var result = _handler.GenerateToAttributeValue(property);
            
            var expected = $"new AttributeValue {{ SS = model.{propName}?.ToList() ?? new List<string>() }}";
            await Assert.That(result)
                .IsEqualTo(expected)
                .Because($"String collections should generate SS (String Set) attribute value");
        }
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateNumberSet_ForNumericCollections()
    {
        var numericCollectionTypes = new (ITypeSymbol, string, ITypeSymbol)[]
        {
            (MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.Int32).Object, "IntArray", MockSymbolFactory.PrimitiveTypes.Int32),
            (MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Double).Object, "DoubleList", MockSymbolFactory.PrimitiveTypes.Double),
            (MockSymbolFactory.CreateGenericType("ISet", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Decimal).Object, "DecimalSet", MockSymbolFactory.PrimitiveTypes.Decimal)
        };

        foreach (var (collectionType, propName, elementType) in numericCollectionTypes)
        {
            var property = TestModelBuilders.CreateCollectionPropertyInfo(propName, collectionType, elementType);
            var result = _handler.GenerateToAttributeValue(property);
            
            var expected = $"new AttributeValue {{ NS = model.{propName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}";
            await Assert.That(result)
                .IsEqualTo(expected)
                .Because($"Numeric collections should generate NS (Number Set) attribute value");
        }
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateStringSet_ForBooleanCollections()
    {
        var boolCollectionType = MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.Boolean).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("BoolArray", boolCollectionType, MockSymbolFactory.PrimitiveTypes.Boolean);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        var expected = "new AttributeValue { SS = model.BoolArray?.Select(x => x.ToString()).ToList() ?? new List<string>() }";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("Boolean collections should generate SS with ToString() conversion");
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateStringSet_ForGuidCollections()
    {
        var guidCollectionType = MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.Guid).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("GuidArray", guidCollectionType, MockSymbolFactory.PrimitiveTypes.Guid);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        var expected = "new AttributeValue { SS = model.GuidArray?.Select(x => x.ToString()).ToList() ?? new List<string>() }";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("Guid collections should generate SS with ToString() conversion");
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateStringSet_ForDateTimeCollections()
    {
        var dateTimeCollectionType = MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.DateTime).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("DateTimeArray", dateTimeCollectionType, MockSymbolFactory.PrimitiveTypes.DateTime);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        var expected = "new AttributeValue { SS = model.DateTimeArray?.Select(x => x.ToString(\"o\")).ToList() ?? new List<string>() }";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("DateTime collections should generate SS with ISO format");
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateStringSet_ForEnumCollections()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
        var enumCollectionType = MockSymbolFactory.CreateArrayType(enumType).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("PriorityArray", enumCollectionType, enumType);
        
        var result = _handler.GenerateToAttributeValue(property);
        
        var expected = "new AttributeValue { SS = model.PriorityArray?.Select(x => x.ToString()).ToList() ?? new List<string>() }";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("Enum collections should generate SS with ToString() conversion");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForStringArrays()
    {
        var stringArrayType = MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.String).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("StringArray", stringArrayType, MockSymbolFactory.PrimitiveTypes.String);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        var expected = "(((record.TryGetStringSet(\"StringArray\", out var stringarray) ? stringarray : null))?.ToArray() ?? Array.Empty<string>())";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("String arrays should deserialize from SS using ToArray()");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForStringLists()
    {
        var stringListType = MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.String).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("StringList", stringListType, MockSymbolFactory.PrimitiveTypes.String);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        var expected = "(((record.TryGetStringSet(\"StringList\", out var stringlist) ? stringlist : null))?.ToList() ?? new List<string>())";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("String lists should deserialize from SS using ToList()");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForIntegerSets()
    {
        var intSetType = MockSymbolFactory.CreateGenericType("HashSet", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Int32).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("IntSet", intSetType, MockSymbolFactory.PrimitiveTypes.Int32);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
        
        var expected = "new HashSet<int>((record.TryGetIntSet(\"IntSet\", out var intset) ? intset : null) ?? Enumerable.Empty<int>())";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("Integer HashSet should deserialize from NS and convert to HashSet");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForBooleanCollections()
    {
        var boolListType = MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Boolean).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("BoolList", boolListType, MockSymbolFactory.PrimitiveTypes.Boolean);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        var expected = "(((record.TryGetStringSet(\"BoolList\", out var boollistStrs) ? boollistStrs.Select(bool.Parse) : null))?.ToList() ?? new List<bool>())";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("Boolean collections should deserialize from SS with bool.Parse");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForGuidCollections()
    {
        var guidListType = MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Guid).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("GuidList", guidListType, MockSymbolFactory.PrimitiveTypes.Guid);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        var expected = "(((record.TryGetStringSet(\"GuidList\", out var guidlistStrs) ? guidlistStrs.Select(Guid.Parse) : null))?.ToList() ?? new List<System.Guid>())";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("Guid collections should deserialize from SS with Guid.Parse");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldGenerateCorrectCode_ForEnumCollections()
    {
        var enumType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Priority", 
            "TestNamespace.Priority", 
            "TestNamespace",
            typeKind: TypeKind.Enum).Object;
        var enumListType = MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", enumType).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("PriorityList", enumListType, enumType);
        
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        var expected = "(((record.TryGetEnumSet<TestNamespace.Priority>(\"PriorityList\", out var prioritylist) ? prioritylist : null))?.ToList() ?? new List<TestNamespace.Priority>())";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("Enum collections should deserialize using TryGetEnumSet with proper type");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldHandleReadOnlyCollections()
    {
        var readOnlyTypes = new (ITypeSymbol, string, ITypeSymbol)[]
        {
            (MockSymbolFactory.CreateGenericType("IReadOnlyCollection", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.String).Object, "StringReadOnlyCollection", MockSymbolFactory.PrimitiveTypes.String),
            (MockSymbolFactory.CreateGenericType("IReadOnlyList", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Int32).Object, "IntReadOnlyList", MockSymbolFactory.PrimitiveTypes.Int32),
            (MockSymbolFactory.CreateGenericType("IReadOnlySet", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.Double).Object, "DoubleReadOnlySet", MockSymbolFactory.PrimitiveTypes.Double)
        };

        foreach (var (collectionType, propName, elementType) in readOnlyTypes)
        {
            var property = TestModelBuilders.CreateCollectionPropertyInfo(propName, collectionType, elementType);
            var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
            
            await Assert.That(result)
                .IsNotNull()
                .Because($"ReadOnly collection types should be supported: {collectionType.Name}");
                
            await Assert.That(result)
                .DoesNotContain("default(")
                .Because($"ReadOnly collections should not fall back to default value");
        }
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldHandleNumericTypeConversions()
    {
        var numericConversions = new (ITypeSymbol, string, string)[]
        {
            (MockSymbolFactory.PrimitiveTypes.Byte, "TryGetIntSet", "(byte)x"),
            (MockSymbolFactory.PrimitiveTypes.SByte, "TryGetIntSet", "(sbyte)x"),
            (MockSymbolFactory.PrimitiveTypes.Int16, "TryGetIntSet", "(short)x"),
            (MockSymbolFactory.PrimitiveTypes.UInt16, "TryGetIntSet", "(ushort)x"),
            (MockSymbolFactory.PrimitiveTypes.UInt32, "TryGetLongSet", "(uint)x"),
            (MockSymbolFactory.PrimitiveTypes.UInt64, "TryGetLongSet", "(ulong)x"),
            (MockSymbolFactory.PrimitiveTypes.Single, "TryGetDoubleSet", "(float)x"),
            (MockSymbolFactory.PrimitiveTypes.Decimal, "TryGetDoubleSet", "(decimal)x")
        };

        foreach (var (elementType, expectedMethod, expectedConversion) in numericConversions)
        {
            var listType = MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", elementType).Object;
            var property = TestModelBuilders.CreateCollectionPropertyInfo($"{elementType.Name}List", listType, elementType);
            
            var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
            
            await Assert.That(result)
                .Contains(expectedMethod)
                .Because($"{elementType.Name} should use {expectedMethod} for retrieval");
                
            await Assert.That(result)
                .Contains(expectedConversion)
                .Because($"{elementType.Name} should include proper type conversion");
        }
    }

    [Test]
    public async Task GenerateKeyFormatting_ShouldGenerateStringJoin()
    {
        var stringArrayType = MockSymbolFactory.CreateArrayType(MockSymbolFactory.PrimitiveTypes.String).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("Tags", stringArrayType, MockSymbolFactory.PrimitiveTypes.String);
        
        var result = _handler.GenerateKeyFormatting(property);
        
        var expected = "string.Join(\",\", model.Tags ?? Enumerable.Empty<object>())";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("Collection key formatting should join elements with comma");
    }

    [Test]
    public async Task Priority_ShouldReturnCorrectValue()
    {
        await Assert.That(_handler.Priority).IsEqualTo(110);
    }

    [Test]
    public async Task GenerateToAttributeValue_ShouldGenerateMapList_ForComplexCollections()
    {
        var customType = MockSymbolFactory.CreateNamedTypeSymbol("CustomClass", "TestNamespace.CustomClass", "TestNamespace").Object;
        var customCollectionType = MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", customType).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("CustomList", customCollectionType, customType);

        var result = _handler.GenerateToAttributeValue(property);

        var expected = "model.CustomList != null ? new AttributeValue { L = model.CustomList.Select(item => (item != null ? new AttributeValue { M = DynamoMapper.CustomClass.ToDynamoRecord(item) } : new AttributeValue { NULL = true })).ToList() } : new AttributeValue { NULL = true }";
        await Assert.That(result)
            .IsEqualTo(expected)
            .Because("Complex type collections should generate L (List) attribute with M (Map) elements and handle null items");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldHandleCollectionInterfaces()
    {
        var interfaceTypes = new[]
        {
            MockSymbolFactory.CreateGenericType("IList", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.String).Object,
            MockSymbolFactory.CreateGenericType("ICollection", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.String).Object,
            MockSymbolFactory.CreateGenericType("IEnumerable", "System.Collections.Generic", MockSymbolFactory.PrimitiveTypes.String).Object
        };

        foreach (var interfaceType in interfaceTypes)
        {
            var property = TestModelBuilders.CreateCollectionPropertyInfo($"String{interfaceType.Name}", interfaceType, MockSymbolFactory.PrimitiveTypes.String);
            var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");
            
            await Assert.That(result)
                .IsNotNull()
                .Because($"{interfaceType.Name} should generate valid code");
                
            await Assert.That(result)
                .DoesNotContain("default(")
                .Because($"{interfaceType.Name} should not fall back to default value");
        }
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ShouldHandleConcreteCollectionTypes()
    {
        var collectionType = MockSymbolFactory.CreateGenericType("Collection", "System.Collections.ObjectModel", MockSymbolFactory.PrimitiveTypes.String).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("StringCollection", collectionType, MockSymbolFactory.PrimitiveTypes.String);

        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        await Assert.That(result)
            .Contains("System.Collections.ObjectModel.Collection<string>")
            .Because("Collection<T> should use the full type name for construction");
    }

    #region Null Element and Nested Collection Tests

    [Test]
    public async Task GenerateToAttributeValue_ComplexCollectionWithNullElements_ShouldGenerateNullCheck()
    {
        // Arrange
        var customType = MockSymbolFactory.CreateNamedTypeSymbol("CustomClass", "TestNamespace.CustomClass", "TestNamespace").Object;
        var customCollectionType = MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", customType).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("CustomList", customCollectionType, customType);

        // Act
        var result = _handler.GenerateToAttributeValue(property);

        // Assert
        await Assert.That(result)
            .Contains("item != null")
            .Because("Complex type collections should check for null items before converting");

        await Assert.That(result)
            .Contains("new AttributeValue { NULL = true }")
            .Because("Null items should be represented as DynamoDB NULL attribute");
    }

    [Test]
    public async Task GenerateToAttributeValue_NestedStringCollection_TwoLevels_ShouldGenerateCorrectCode()
    {
        // Arrange
        var innerCollectionType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String).Object;

        var nestedCollectionType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            innerCollectionType).Object;

        var property = TestModelBuilders.CreateCollectionPropertyInfo("NestedStrings", nestedCollectionType, innerCollectionType);

        // Act
        var result = _handler.GenerateToAttributeValue(property);

        // Assert
        await Assert.That(result)
            .Contains("L =")
            .Because("Nested collections should use L (List) attribute type");

        await Assert.That(result)
            .IsNotNull()
            .Because("Nested collections should be supported");
    }

    [Test]
    public async Task GenerateToAttributeValue_NestedIntCollection_TwoLevels_ShouldGenerateCorrectCode()
    {
        // Arrange
        var innerCollectionType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.Int32).Object;

        var nestedCollectionType = MockSymbolFactory.CreateGenericType(
            "IEnumerable",
            "System.Collections.Generic",
            innerCollectionType).Object;

        var property = TestModelBuilders.CreateCollectionPropertyInfo("NestedInts", nestedCollectionType, innerCollectionType);

        // Act
        var result = _handler.GenerateToAttributeValue(property);

        // Assert
        await Assert.That(result)
            .Contains("L =")
            .Because("Nested numeric collections should use L (List) attribute type");

        await Assert.That(result)
            .IsNotNull()
            .Because("Nested collections should be supported");
    }

    [Test]
    public async Task GenerateToAttributeValue_NestedCollection_WithNullInnerCollection_ShouldHandleNulls()
    {
        // Arrange
        var innerCollectionType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String).Object;

        var nestedCollectionType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            innerCollectionType).Object;

        var property = TestModelBuilders.CreateCollectionPropertyInfo("NestedStrings", nestedCollectionType, innerCollectionType);

        // Act
        var result = _handler.GenerateToAttributeValue(property);

        // Assert
        await Assert.That(result)
            .Contains("item != null")
            .Because("Nested collections should check for null inner collections");

        await Assert.That(result)
            .Contains("NULL = true")
            .Because("Null inner collections should be represented as DynamoDB NULL");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_NestedStringCollection_ShouldDeserializeCorrectly()
    {
        // Arrange
        var innerCollectionType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String).Object;

        var nestedCollectionType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            innerCollectionType).Object;

        var property = TestModelBuilders.CreateCollectionPropertyInfo("NestedStrings", nestedCollectionType, innerCollectionType);

        // Act
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        // Assert
        await Assert.That(result)
            .IsNotNull()
            .Because("Nested collections should generate deserialization code");

        await Assert.That(result)
            .Contains("record.TryGetList")
            .Because("Nested collections should use TryGetList for retrieval");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ComplexNestedCollection_ShouldHandleMapTypes()
    {
        // Arrange - IEnumerable<ProductInfo> where ProductInfo is a complex type
        var complexType = MockSymbolFactory.CreateNamedTypeSymbol("ProductInfo", "TestNamespace.ProductInfo", "TestNamespace").Object;
        var complexCollectionType = MockSymbolFactory.CreateGenericType("IEnumerable", "System.Collections.Generic", complexType).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("Products", complexCollectionType, complexType);

        // Act
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        // Assert
        await Assert.That(result)
            .Contains("DynamoMapper.ProductInfo.FromDynamoRecord")
            .Because("Complex type collections should deserialize elements using DynamoMapper");

        await Assert.That(result)
            .IsNotNull()
            .Because("Complex type collections should be supported");
    }

    [Test]
    public async Task GenerateFromDynamoRecord_ComplexCollection_ShouldPreserveNullElements()
    {
        // Arrange
        var complexType = MockSymbolFactory.CreateNamedTypeSymbol("CustomClass", "TestNamespace.CustomClass", "TestNamespace").Object;
        var complexCollectionType = MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", complexType).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("CustomList", complexCollectionType, complexType);

        // Act
        var result = _handler.GenerateFromDynamoRecord(property, "record", "pkValue", "skValue");

        // Assert
        await Assert.That(result)
            .IsNotNull()
            .Because("Complex type collections should deserialize correctly");

        // The generated code should handle nulls in the list properly
        await Assert.That(result)
            .DoesNotContain("default(")
            .Because("Should not use default values for missing elements");
    }

    [Test]
    public async Task GenerateToAttributeValue_ThreeLevelNestedCollection_ShouldBeSupported()
    {
        // Arrange - List<List<List<string>>>
        var level1Type = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String).Object;

        var level2Type = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            level1Type).Object;

        var level3Type = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            level2Type).Object;

        var property = TestModelBuilders.CreateCollectionPropertyInfo("DeepNested", level3Type, level2Type);

        // Act
        var result = _handler.GenerateToAttributeValue(property);

        // Assert
        await Assert.That(result)
            .IsNotNull()
            .Because("Three-level nested collections should be supported");

        await Assert.That(result)
            .Contains("L =")
            .Because("Deep nested collections should use List attribute type");
    }

    [Test]
    public async Task GenerateToAttributeValue_CollectionOfNullableElements_ShouldNotFilterNulls()
    {
        // Arrange - Collection elements can be null, they should be preserved as NULL attributes
        var customType = MockSymbolFactory.CreateNamedTypeSymbol("Address", "TestNamespace.Address", "TestNamespace").Object;
        var collectionType = MockSymbolFactory.CreateGenericType("List", "System.Collections.Generic", customType).Object;
        var property = TestModelBuilders.CreateCollectionPropertyInfo("Addresses", collectionType, customType);

        // Act
        var result = _handler.GenerateToAttributeValue(property);

        // Assert
        await Assert.That(result)
            .Contains("item != null ? new AttributeValue { M =")
            .Because("Should check each item for null");

        await Assert.That(result)
            .Contains(": new AttributeValue { NULL = true }")
            .Because("Null items should be preserved as NULL attributes, not filtered out");
    }

    #endregion

    private TypeHandlerRegistry CreateTypeHandlerRegistry()
    {
        var registry = new TypeHandlerRegistry();
        registry.RegisterHandler(new PrimitiveTypeHandler());
        registry.RegisterHandler(new EnumTypeHandler());
        registry.RegisterHandler(_handler);
        return registry;
    }
}