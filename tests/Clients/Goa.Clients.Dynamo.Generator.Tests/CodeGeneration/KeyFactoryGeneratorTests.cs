using Goa.Clients.Dynamo.Generator.CodeGeneration;
using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.Tests.Helpers;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Tests.CodeGeneration;

public class KeyFactoryGeneratorTests
{
    private readonly TypeHandlerRegistry _typeHandlerRegistry;
    private readonly KeyFactoryGenerator _generator;

    public KeyFactoryGeneratorTests()
    {
        _typeHandlerRegistry = CreateTypeHandlerRegistry();
        _generator = new KeyFactoryGenerator(_typeHandlerRegistry);
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
    public async Task GenerateCode_WithTypesWithoutDynamoModel_ShouldReturnEmptyString()
    {
        // Arrange
        var types = new List<DynamoTypeInfo>
        {
            TestModelBuilders.CreateDynamoTypeInfo("User", "TestNamespace.User", "TestNamespace")
        };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .IsEqualTo(string.Empty);
    }

    [Test]
    public async Task GenerateCode_WithStaticKeys_ShouldGenerateStaticMethods()
    {
        // Arrange
        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "STATIC_PK",
            sk: "STATIC_SK"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "StaticEntity",
            "TestNamespace.StaticEntity",
            "TestNamespace",
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .Contains("public static class DynamoKeyFactory");
        await Assert.That(result)
            .Contains("public static class StaticEntity");
        await Assert.That(result)
            .Contains("public static string PK() => \"STATIC_PK\";");
        await Assert.That(result)
            .Contains("public static string SK() => \"STATIC_SK\";");
    }

    [Test]
    public async Task GenerateCode_WithDynamicKeys_ShouldGenerateParameterizedMethods()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Category", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "ENTITY#<Id>",
            sk: "METADATA#<Category>"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "DynamicEntity",
            "TestNamespace.DynamicEntity",
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
            .Contains("public static class DynamicEntity");
        await Assert.That(result)
            .Contains("public static string PK(string id)");
        await Assert.That(result)
            .Contains("=> $\"ENTITY#{ id?.ToString() ?? \"\" }\";");
        await Assert.That(result)
            .Contains("public static string SK(string category)");
        await Assert.That(result)
            .Contains("=> $\"METADATA#{ category?.ToString() ?? \"\" }\";");
    }

    [Test]
    public async Task GenerateCode_WithGSI_ShouldGenerateGSIMethods()
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
            sk: "USER#<Id>"
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

        // Assert
        await Assert.That(result)
            .Contains("public static class User");
        
        // Primary key methods
        await Assert.That(result)
            .Contains("public static string PK(string id)");
        await Assert.That(result)
            .Contains("=> $\"USER#{ id?.ToString() ?? \"\" }\";");
        await Assert.That(result)
            .Contains("public static string SK() => \"METADATA\";");
        
        // GSI index name method
        await Assert.That(result)
            .Contains("public static string GSI_EmailIndex_Name() => \"EmailIndex\";");
        
        // GSI key methods with GSI_ prefix
        await Assert.That(result)
            .Contains("public static string GSI_EmailIndex_PK(string email)");
        await Assert.That(result)
            .Contains("=> $\"EMAIL#{ email?.ToString() ?? \"\" }\";");
        await Assert.That(result)
            .Contains("public static string GSI_EmailIndex_SK(string id)");
        await Assert.That(result)
            .Contains("=> $\"USER#{ id?.ToString() ?? \"\" }\";");
    }

    [Test]
    public async Task GenerateCode_WithMultipleGSI_ShouldGenerateAllGSIMethods()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Email", MockSymbolFactory.PrimitiveTypes.String),
            TestModelBuilders.CreatePropertyInfo("Status", MockSymbolFactory.PrimitiveTypes.String)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "USER#<Id>",
            sk: "METADATA"
        );

        var emailGsi = TestModelBuilders.CreateGSIAttribute(
            "EmailIndex",
            pk: "EMAIL#<Email>",
            sk: "USER#<Id>"
        );

        var statusGsi = TestModelBuilders.CreateGSIAttribute(
            "StatusIndex",
            pk: "STATUS#<Status>",
            sk: "USER#<Id>"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr, emailGsi, statusGsi }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        // EmailIndex GSI with GSI_ prefix
        await Assert.That(result)
            .Contains("public static string GSI_EmailIndex_PK(string email)");
        await Assert.That(result)
            .Contains("public static string GSI_EmailIndex_SK(string id)");
        
        // StatusIndex GSI with GSI_ prefix
        await Assert.That(result)
            .Contains("public static string GSI_StatusIndex_PK(string status)");
        await Assert.That(result)
            .Contains("public static string GSI_StatusIndex_SK(string id)");
    }

    [Test]
    public async Task GenerateCode_WithDateTime_ShouldUseTypeHandlerFormatting()
    {
        // Arrange
        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("CreatedAt", MockSymbolFactory.PrimitiveTypes.DateTime)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "ENTITY#<CreatedAt>",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "TimestampEntity",
            "TestNamespace.TimestampEntity",
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
            .Contains("public static string PK(System.DateTime createdAt)");
        await Assert.That(result)
            .Contains("=> $\"ENTITY#{ createdAt.ToString(\"o\") }\";");
    }

    [Test]
    public async Task GenerateCode_WithComplexType_ShouldUseToStringFormatting()
    {
        // Arrange
        var complexType = MockSymbolFactory.CreateNamedTypeSymbol(
            "Address",
            "TestNamespace.Address",
            "TestNamespace"
        ).Object;

        var properties = new List<PropertyInfo>
        {
            TestModelBuilders.CreatePropertyInfo("Location", complexType)
        };

        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "ENTITY#<Location>",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "LocationEntity",
            "TestNamespace.LocationEntity",
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
            .Contains("public static string PK(TestNamespace.Address location)");
        await Assert.That(result)
            .Contains("=> $\"ENTITY#{ location?.ToString() ?? \"\" }\";");
    }

    [Test]
    public async Task GenerateCode_WithMissingProperty_ShouldUseFallbackFormatting()
    {
        // Arrange - Create type with no properties but key references a property
        var dynamoModelAttr = TestModelBuilders.CreateDynamoModelAttribute(
            pk: "ENTITY#<MissingProperty>",
            sk: "METADATA"
        );

        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "IncompleteEntity",
            "TestNamespace.IncompleteEntity",
            "TestNamespace",
            properties: new List<PropertyInfo>(), // No properties
            attributes: new List<AttributeInfo> { dynamoModelAttr }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert
        await Assert.That(result)
            .Contains("public static string PK(object missingProperty)");
        await Assert.That(result)
            .Contains("=> $\"ENTITY#{ missingProperty?.ToString() ?? \"\" }\";");
    }

    [Test]
    public async Task GenerateCode_WithMultipleTypes_ShouldGenerateAllTypeClasses()
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
    }

    [Test]
    public async Task GenerateCode_ShouldIncludeRequiredUsings()
    {
        // Arrange
        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "TestEntity",
            "TestNamespace.TestEntity",
            "TestNamespace",
            attributes: new List<AttributeInfo> { 
                TestModelBuilders.CreateDynamoModelAttribute("STATIC_PK", "STATIC_SK") 
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
            .Contains("using Goa.Clients.Dynamo;");
        await Assert.That(result)
            .Contains("namespace TestNamespace;");
    }

    [Test]
    public async Task GenerateCode_WithEmptyNamespace_ShouldUseGeneratedNamespace()
    {
        // Arrange
        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "TestEntity",
            "TestEntity",
            "", // Empty namespace
            attributes: new List<AttributeInfo> { 
                TestModelBuilders.CreateDynamoModelAttribute("STATIC_PK", "STATIC_SK") 
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
    public async Task DebugGSI_KeyFactory_PrintsActualOutput()
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
            sk: "USER#<Id>"
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
        Console.WriteLine("=== ACTUAL KEY FACTORY GENERATED CODE ===");
        Console.WriteLine(result);
        Console.WriteLine("=== END KEY FACTORY GENERATED CODE ===");

        // Just assert it's not empty for now
        await Assert.That(result)
            .IsNotEmpty();
    }

    [Test]
    public async Task DebugGSI_NameNormalization_PrintsActualOutput()
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

        // Test various GSI name patterns
        var gsi1 = TestModelBuilders.CreateGSIAttribute("gsi-1", pk: "EMAIL#<Email>", sk: "USER#<Id>");
        var gsi2 = TestModelBuilders.CreateGSIAttribute("my-index", pk: "EMAIL#<Email>", sk: "USER#<Id>");
        var gsi3 = TestModelBuilders.CreateGSIAttribute("GSI1", pk: "EMAIL#<Email>", sk: "USER#<Id>");
        
        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr, gsi1, gsi2, gsi3 }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Debug - Print actual output
        Console.WriteLine("=== ACTUAL GSI NORMALIZATION OUTPUT ===");
        Console.WriteLine(result);
        Console.WriteLine("=== END GSI NORMALIZATION OUTPUT ===");

        // Just assert it's not empty for now
        await Assert.That(result)
            .IsNotEmpty();
    }

    [Test]
    public async Task GenerateCode_WithGSINameNormalization_ShouldHandleVariousIndexNames()
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

        // Test individual GSI name patterns  
        var gsi1 = TestModelBuilders.CreateGSIAttribute("gsi-1", pk: "EMAIL#<Email>", sk: "USER#<Id>");
        var gsi2 = TestModelBuilders.CreateGSIAttribute("my-index", pk: "EMAIL#<Email>", sk: "USER#<Id>");
        
        var type = TestModelBuilders.CreateDynamoTypeInfo(
            "User",
            "TestNamespace.User",
            "TestNamespace",
            properties: properties,
            attributes: new List<AttributeInfo> { dynamoModelAttr, gsi1, gsi2 }
        );

        var types = new List<DynamoTypeInfo> { type };
        var context = new GenerationContext();

        // Act
        var result = _generator.GenerateCode(types, context);

        // Assert GSI name methods - should always start with GSI_ and avoid GSI_GSI_
        await Assert.That(result)
            .Contains("public static string GSI_1_Name() => \"gsi-1\";");
        await Assert.That(result)
            .Contains("public static string GSI_My_Index_Name() => \"my-index\";");
        
        // Assert GSI key methods use the normalized names with GSI_ prefix
        await Assert.That(result)
            .Contains("public static string GSI_1_PK(string email)"); // from gsi-1
        await Assert.That(result)
            .Contains("public static string GSI_My_Index_PK(string email)"); // from my-index
        
        // Ensure we don't have GSI_GSI_ patterns
        await Assert.That(result)
            .DoesNotContain("GSI_GSI_");
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