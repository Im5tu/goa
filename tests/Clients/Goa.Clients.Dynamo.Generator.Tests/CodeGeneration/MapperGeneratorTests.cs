using Goa.Clients.Dynamo.Generator.CodeGeneration;
using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Tests.CodeGeneration;

public class MapperGeneratorTests
{
    private readonly TypeHandlerRegistry _typeHandlerRegistry;
    private readonly MapperGenerator _generator;

    public MapperGeneratorTests()
    {
        _typeHandlerRegistry = CreateTypeHandlerRegistry();
        _generator = new MapperGenerator(_typeHandlerRegistry);
    }

    [Test]
    public async Task GenerateCode_WithNoTypes_ShouldReturnEmptyString()
    {
        // Arrange
        var types = new List<DynamoTypeInfo>();
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GenerateCode_WithBasicDynamoModel_ShouldGenerateMapperClass()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Name", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "USER#<Id>",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .Contains("public static class DynamoMapper");
        await Assert.That(result)
            .Contains("public static class User");
        await Assert.That(result)
            .Contains("public static DynamoRecord ToDynamoRecord(TestNamespace.User model)");
        await Assert.That(result)
            .Contains("public static TestNamespace.User FromDynamoRecord(DynamoRecord record, string? parentPkValue = null, string? parentSkValue = null)");
    }

    [Test]
    public async Task GenerateCode_WithPrimaryKeys_ShouldGenerateKeyAssignments()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "USER#<Id>",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .Contains("record[\"PK\"] = new AttributeValue { S = $\"USER#{ model.Id?.ToString() ?? \"\" }\" };");
        await Assert.That(result)
            .Contains("record[\"SK\"] = new AttributeValue { S = \"METADATA\" };");
    }

    [Test]
    public async Task GenerateCode_WithCustomPKSKNames_ShouldUseCustomNames()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "USER#<Id>",
            sk: "METADATA",
            pkName: "HashKey",
            skName: "RangeKey"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User", 
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .Contains("record[\"HashKey\"] = new AttributeValue { S = $\"USER#{ model.Id?.ToString() ?? \"\" }\" };");
        await Assert.That(result)
            .Contains("record[\"RangeKey\"] = new AttributeValue { S = \"METADATA\" };");
    }

    [Test]
    public async Task GenerateCode_WithGSI_ShouldGenerateGSIAssignments()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Email", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "USER#<Id>",
            sk: "METADATA"
        );

        var gsiAttr = TestModelBuilders.CreateGSIAttribute(
            "EmailIndex",
            pk: "EMAIL#<Email>",
            sk: "USER#<Id>",
            pkName: "GSI1PK",
            skName: "GSI1SK"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr, gsiAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert - Based on actual generated output
        await Assert.That(result)
            .Contains("record[\"GSI1PK\"] = new AttributeValue { S = $\"EMAIL#{ model.Email?.ToString() ?? \"\" }\" };");
        await Assert.That(result)
            .Contains("record[\"GSI1SK\"] = new AttributeValue { S = $\"USER#{ model.Id?.ToString() ?? \"\" }\" };");
    }

    [Test]
    public async Task GenerateCode_WithPropertyMappings_ShouldGeneratePropertyCode()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Name", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Age", MockSymbolFactory.PrimitiveTypes.Int32)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "USER#<Id>",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert - Based on actual output from debug test
        await Assert.That(result)
            .Contains("record[\"Id\"] = new AttributeValue { S = model.Id };");
        await Assert.That(result)
            .Contains("record[\"Name\"] = new AttributeValue { S = model.Name };");
        await Assert.That(result)
            .Contains("record[\"Age\"] = new AttributeValue { N = model.Age.ToString() };");
    }

    [Test]
    public async Task GenerateCode_WithComplexProperties_ShouldUseTypeHandlers()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("CreatedAt", MockSymbolFactory.PrimitiveTypes.DateTime),
            TestModelBuilders.CreateUnixTimestampPropertyInfo("UpdatedAt", MockSymbolFactory.PrimitiveTypes.DateTime, format: Models.UnixTimestampFormat.Seconds)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "ENTITY#123",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "Entity",
            "TestNamespace.Entity",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        // DateTime should use DateTimeTypeHandler  
        await Assert.That(result)
            .Contains("record[\"CreatedAt\"] = new AttributeValue { S = model.CreatedAt.ToString(\"o\") };");
        
        // UnixTimestamp should use UnixTimestampTypeHandler
        await Assert.That(result)
            .Contains("record[\"UpdatedAt\"] = new AttributeValue { N = ((DateTimeOffset)model.UpdatedAt).ToUnixTimeSeconds().ToString() };");
    }

    [Test]
    public async Task GenerateCode_WithCollectionProperties_ShouldUseCollectionHandler()
    {
        // Arrange
        var listType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String
        ).Object;

        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreateCollectionPropertyInfo("Tags", listType, MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "ENTITY#123",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "Entity",
            "TestNamespace.Entity",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert - Based on actual CollectionTypeHandler output
        await Assert.That(result)
            .Contains("record[\"Tags\"] = new AttributeValue { SS = model.Tags?.ToList() ?? new List<string>() };");
    }

    [Test]
    public async Task GenerateCode_WithRequiredUsings_ShouldIncludeAllNamespaces()
    {
        // Arrange
        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            attributes: new List<AttributeInfo> { 
                TestModelBuilders.CreateDynamoModelAttribute("USER#123", "METADATA") 
            }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .Contains("#nullable enable");
        await Assert.That(result)
            .Contains("using System;");
        await Assert.That(result)
            .Contains("using System.Collections.Generic;");
        await Assert.That(result)
            .Contains("using System.Linq;");
        await Assert.That(result)
            .Contains("using Goa.Clients.Dynamo;");
        await Assert.That(result)
            .Contains("using Goa.Clients.Dynamo.Models;");
        await Assert.That(result)
            .Contains("using Goa.Clients.Dynamo.Exceptions;");
        await Assert.That(result)
            .Contains("using Goa.Clients.Dynamo.Extensions;");
        await Assert.That(result)
            .Contains("namespace TestNamespace;");
    }

    [Test]
    public async Task DebugGSI_PrintsActualOutput()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Email", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "USER#<Id>",
            sk: "METADATA"
        );

        var gsiAttr = TestModelBuilders.CreateGSIAttribute(
            "EmailIndex",
            pk: "EMAIL#<Email>",
            sk: "USER#<Id>",
            pkName: "GSI1PK",
            skName: "GSI1SK"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr, gsiAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Debug - Print actual output
        Console.WriteLine("=== ACTUAL GSI GENERATED CODE ===");
        Console.WriteLine(result);
        Console.WriteLine("=== END GSI GENERATED CODE ===");

        // Just assert it's not empty for now
        await Assert.That(result)
            .IsNotEmpty();
    }

    [Test]
    public async Task GenerateCode_WithEmptyNamespace_ShouldUseGeneratedNamespace()
    {
        // Arrange
        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "User",
            "", // Empty namespace
            attributes: new List<AttributeInfo> { 
                TestModelBuilders.CreateDynamoModelAttribute("USER#123", "METADATA") 
            }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .Contains("namespace Generated;");
    }

    [Test]
    public async Task GenerateCode_WithMultipleTypes_ShouldGenerateAllMappers()
    {
        // Arrange
        var userType = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            attributes: new List<AttributeInfo> { 
                TestModelBuilders.CreateDynamoModelAttribute("USER#<Id>", "METADATA") 
            }
        );

        var productType = TestModelBuilders.CreateDynamoTypeInfo(
            "Product",
            "TestNamespace.Product",
            "TestNamespace",
            attributes: new List<AttributeInfo> { 
                TestModelBuilders.CreateDynamoModelAttribute("PRODUCT#<Id>", "METADATA") 
            }
        );

        var types = new List<DynamoTypeInfo> { userType, productType };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .Contains("public static class User");
        await Assert.That(result)
            .Contains("public static class Product");
        await Assert.That(result)
            .Contains("public static DynamoRecord ToDynamoRecord(TestNamespace.User model)");
        await Assert.That(result)
            .Contains("public static DynamoRecord ToDynamoRecord(TestNamespace.Product model)");
    }

    [Test]
    public async Task GenerateCode_WithFromDynamoRecord_ShouldGenerateFromMethods()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Name", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "USER#<Id>",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert - Now only one method with default parameters
        await Assert.That(result)
            .Contains("public static TestNamespace.User FromDynamoRecord(DynamoRecord record, string? parentPkValue = null, string? parentSkValue = null)");
        await Assert.That(result)
            .Contains("var pkValue = record.TryGetNullableString(\"PK\", out var pk) ? pk : parentPkValue ?? string.Empty;");
        await Assert.That(result)
            .Contains("var skValue = record.TryGetNullableString(\"SK\", out var sk) ? sk : parentSkValue ?? string.Empty;");
    }

    [Test]
    public async Task GenerateCode_WithConcreteTypeConstruction_ShouldGenerateObjectInitializer()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Name", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "USER#<Id>",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert - Based on actual output from debug test
        await Assert.That(result)
            .Contains("return new TestNamespace.User()");
        await Assert.That(result)
            .Contains("Id = record.TryGetString(\"Id\", out var id) ? id : MissingAttributeException.Throw<string>(\"Id\", pkValue, skValue),");
        await Assert.That(result)
            .Contains("Name = record.TryGetString(\"Name\", out var name) ? name : MissingAttributeException.Throw<string>(\"Name\", pkValue, skValue),");
    }

    [Test]
    public async Task GenerateCode_WithAbstractType_ToDynamoRecordShouldUseSwitchStatement()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Name", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "ENTITY#<Id>",
            sk: "METADATA"
        );

        var abstractType = TestModelBuilders.CreateDynamoTypeInfo(
            "BaseEntity",
            "TestNamespace.BaseEntity",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr },
            isAbstract: true
        );

        var concreteType = TestModelBuilders.CreateDynamoTypeInfo(
            "ConcreteEntity",
            "TestNamespace.ConcreteEntity", 
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr },
            baseType: abstractType
        );

        var types = new List<DynamoTypeInfo> { abstractType, concreteType };
        var context = new GenerationContext();
        context.TypeRegistry[abstractType.FullName] = new List<DynamoTypeInfo> { concreteType };

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert - ToDynamoRecord for abstract type should use switch statement
        await Assert.That(result)
            .Contains("public static DynamoRecord ToDynamoRecord(TestNamespace.BaseEntity model)");
        await Assert.That(result)
            .Contains("return model switch");
        await Assert.That(result)
            .Contains("TestNamespace.ConcreteEntity concrete => DynamoMapper.ConcreteEntity.ToDynamoRecord(concrete),");
        await Assert.That(result)
            .Contains("_ => throw new InvalidOperationException($\"Unknown concrete type: {model.GetType().FullName} for abstract type TestNamespace.BaseEntity\")");
    }

    private static TypeHandlerRegistry CreateTypeHandlerRegistry()
    {
        var registry = new TypeHandlerRegistry();
        registry.RegisterHandler(new PrimitiveTypeHandler());
        registry.RegisterHandler(new EnumTypeHandler());
        registry.RegisterHandler(new DateOnlyTypeHandler());
        registry.RegisterHandler(new TimeOnlyTypeHandler());
        registry.RegisterHandler(new DateTimeTypeHandler());
        registry.RegisterHandler(new UnixTimestampTypeHandler());
        var collectionHandler = new CollectionTypeHandler();
        collectionHandler.SetRegistry(registry);
        registry.RegisterHandler(collectionHandler);
        var complexHandler = new ComplexTypeHandler();
        complexHandler.SetRegistry(registry);
        registry.RegisterHandler(complexHandler);
        return registry;
    }
}