using Microsoft.CodeAnalysis;
using Moq;
using Goa.Clients.Dynamo.Generator.Models;
using System.Collections.Immutable;

namespace Goa.Clients.Dynamo.Generator.Tests.Helpers;

/// <summary>
/// Builders for creating consistent test model data used across multiple tests.
/// </summary>
public static class TestModelBuilders
{
    /// <summary>
    /// Creates a PropertyInfo for testing type handlers.
    /// </summary>
    public static PropertyInfo CreatePropertyInfo(
        string name,
        ITypeSymbol type,
        bool isNullable = false,
        bool isCollection = false,
        bool isDictionary = false,
        ITypeSymbol? elementType = null,
        (ITypeSymbol Key, ITypeSymbol Value)? dictionaryTypes = null,
        List<AttributeInfo>? attributes = null,
        IPropertySymbol? symbol = null)
    {
        // For nullable value types, wrap the type in Nullable<T>
        var actualType = type;
        if (isNullable && IsValueType(type))
        {
            actualType = MockSymbolFactory.CreateNullableValueType(type).Object;
        }
        
        // Auto-detect collection properties based on type
        var detectedIsCollection = isCollection || IsCollectionType(type);
        var detectedElementType = elementType ?? ExtractElementType(type);
        
        return new PropertyInfo
        {
            Name = name,
            Type = actualType,
            IsNullable = isNullable,
            IsCollection = detectedIsCollection,
            IsDictionary = isDictionary,
            ElementType = detectedElementType,
            DictionaryTypes = dictionaryTypes,
            Attributes = attributes ?? new List<AttributeInfo>(),
            Symbol = symbol ?? MockSymbolFactory.CreatePropertySymbol(name, actualType).Object
        };
    }
    
    /// <summary>
    /// Creates a PropertyInfo for a collection type, automatically detecting collection properties.
    /// </summary>
    public static PropertyInfo CreateCollectionPropertyInfo(
        string name,
        ITypeSymbol collectionType,
        ITypeSymbol elementType,
        bool isNullable = false,
        List<AttributeInfo>? attributes = null)
    {
        return CreatePropertyInfo(
            name, 
            collectionType, 
            isNullable, 
            isCollection: true, 
            elementType: elementType, 
            attributes: attributes);
    }
    
    /// <summary>
    /// Creates a PropertyInfo with UnixTimestamp attribute for testing.
    /// </summary>
    public static PropertyInfo CreateUnixTimestampPropertyInfo(
        string name,
        ITypeSymbol type,
        bool isNullable = false,
        Models.UnixTimestampFormat format = Models.UnixTimestampFormat.Seconds)
    {
        var attributes = new List<AttributeInfo>
        {
            new UnixTimestampAttributeInfo { Format = format }
        };
        
        return CreatePropertyInfo(
            name, 
            type, 
            isNullable, 
            attributes: attributes);
    }
    
    private static bool IsValueType(ITypeSymbol type)
    {
        // Common value types that should be wrapped in Nullable<T>
        var valueTypeNames = new[] { "DateOnly", "TimeOnly", "DateTime", "Int32", "Boolean", "Double", "Decimal", "Guid" };
        return valueTypeNames.Contains(type.Name);
    }
    
    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol)
            return true;
            
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.Name;
            var collectionTypeNames = new[]
            {
                "List", "IList", "ICollection", "IEnumerable",
                "HashSet", "ISet", "IReadOnlySet",
                "Collection", "IReadOnlyCollection", "IReadOnlyList"
            };
            return collectionTypeNames.Contains(typeName);
        }
        
        return false;
    }
    
    private static ITypeSymbol? ExtractElementType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol arrayType)
            return arrayType.ElementType;
            
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.TypeArguments.Length > 0)
            return namedType.TypeArguments[0];
            
        return null;
    }
    
    /// <summary>
    /// Creates a DynamoTypeInfo for testing code generation.
    /// </summary>
    public static DynamoTypeInfo CreateDynamoTypeInfo(
        string name,
        string fullName,
        string namespaceName = "TestNamespace",
        bool isAbstract = false,
        bool isRecord = false,
        List<PropertyInfo>? properties = null,
        List<AttributeInfo>? attributes = null,
        DynamoTypeInfo? baseType = null,
        INamedTypeSymbol? symbol = null)
    {
        return new DynamoTypeInfo
        {
            Name = name,
            FullName = fullName,
            Namespace = namespaceName,
            IsAbstract = isAbstract,
            IsRecord = isRecord,
            Properties = properties ?? new List<PropertyInfo>(),
            Attributes = attributes ?? new List<AttributeInfo>(),
            BaseType = baseType,
            Symbol = symbol ?? MockSymbolFactory.CreateNamedTypeSymbol(name, fullName, namespaceName, isAbstract, isRecord).Object
        };
    }
    
    /// <summary>
    /// Creates a DynamoModelAttributeInfo for testing.
    /// </summary>
    public static DynamoModelAttributeInfo CreateDynamoModelAttribute(
        string pk = "ENTITY#<Id>",
        string sk = "METADATA",
        string pkName = "PK",
        string skName = "SK")
    {
        return new DynamoModelAttributeInfo
        {
            PK = pk,
            SK = sk,
            PKName = pkName,
            SKName = skName
        };
    }
    
    /// <summary>
    /// Creates a GSIAttributeInfo for testing.
    /// </summary>
    public static GSIAttributeInfo CreateGSIAttribute(
        string name = "TestIndex",
        string pk = "GSI#<Field>",
        string sk = "ENTITY#<Id>",
        string? pkName = null,
        string? skName = null)
    {
        return new GSIAttributeInfo
        {
            IndexName = name,
            PK = pk,
            SK = sk,
            PKName = pkName,
            SKName = skName
        };
    }
    
    /// <summary>
    /// Creates a UnixTimestampAttributeInfo for testing.
    /// </summary>
    public static UnixTimestampAttributeInfo CreateUnixTimestampAttribute()
    {
        return new UnixTimestampAttributeInfo();
    }
    
    /// <summary>
    /// Builder for creating complex test scenarios.
    /// </summary>
    public static class Scenarios
    {
        /// <summary>
        /// Creates a BaseEntity-like type with DynamoModel attribute.
        /// </summary>
        public static DynamoTypeInfo CreateBaseEntity()
        {
            var properties = new List<PropertyInfo>
            {
                CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
                CreatePropertyInfo("CreatedAt", MockSymbolFactory.PrimitiveTypes.DateTime),
                CreatePropertyInfo("UpdatedAt", MockSymbolFactory.PrimitiveTypes.DateTime, isNullable: true),
                CreatePropertyInfo("CreatedBy", MockSymbolFactory.PrimitiveTypes.String),
                CreatePropertyInfo("UpdatedBy", MockSymbolFactory.PrimitiveTypes.String, isNullable: true),
                CreatePropertyInfo("IsActive", MockSymbolFactory.PrimitiveTypes.Boolean)
            };
            
            var attributes = new List<AttributeInfo>
            {
                CreateDynamoModelAttribute()
            };
            
            return CreateDynamoTypeInfo(
                "BaseEntity",
                "TestNamespace.BaseEntity", 
                properties: properties,
                attributes: attributes,
                isAbstract: true);
        }
        
        /// <summary>
        /// Creates a UserProfile-like type with GSI attributes that inherits from BaseEntity.
        /// </summary>
        public static DynamoTypeInfo CreateUserProfile(DynamoTypeInfo? baseEntity = null)
        {
            baseEntity ??= CreateBaseEntity();
            
            var properties = new List<PropertyInfo>
            {
                CreatePropertyInfo("Email", MockSymbolFactory.PrimitiveTypes.String),
                CreatePropertyInfo("FirstName", MockSymbolFactory.PrimitiveTypes.String),
                CreatePropertyInfo("LastName", MockSymbolFactory.PrimitiveTypes.String),
                CreatePropertyInfo("Status", CreateEnumType("UserStatus")),
                CreatePropertyInfo("LoginCount", MockSymbolFactory.PrimitiveTypes.Int32),
                CreatePropertyInfo("LastLoginAt", MockSymbolFactory.PrimitiveTypes.DateTime, isNullable: true)
            };
            
            var attributes = new List<AttributeInfo>
            {
                CreateGSIAttribute("EmailIndex", "EMAIL#<Email>", "USER#<Id>"),
                CreateGSIAttribute("StatusIndex", "STATUS#<Status>", "USER#<Id>", "StatusPK", "StatusSK")
            };
            
            return CreateDynamoTypeInfo(
                "UserProfile",
                "TestNamespace.UserProfile",
                properties: properties,
                attributes: attributes,
                baseType: baseEntity);
        }
        
        /// <summary>
        /// Creates a ComplexTestModel-like type with comprehensive property types.
        /// </summary>
        public static DynamoTypeInfo CreateComplexTestModel()
        {
            var properties = new List<PropertyInfo>
            {
                // Primary identifiers
                CreatePropertyInfo("Id", MockSymbolFactory.PrimitiveTypes.String),
                CreatePropertyInfo("Category", MockSymbolFactory.PrimitiveTypes.String),
                
                // Primitive types
                CreatePropertyInfo("BoolValue", MockSymbolFactory.PrimitiveTypes.Boolean),
                CreatePropertyInfo("ByteValue", MockSymbolFactory.PrimitiveTypes.Byte),
                CreatePropertyInfo("SByteValue", MockSymbolFactory.PrimitiveTypes.SByte),
                CreatePropertyInfo("CharValue", MockSymbolFactory.PrimitiveTypes.Char),
                CreatePropertyInfo("ShortValue", MockSymbolFactory.PrimitiveTypes.Int16),
                CreatePropertyInfo("UShortValue", MockSymbolFactory.PrimitiveTypes.UInt16),
                CreatePropertyInfo("IntValue", MockSymbolFactory.PrimitiveTypes.Int32),
                CreatePropertyInfo("UIntValue", MockSymbolFactory.PrimitiveTypes.UInt32),
                CreatePropertyInfo("LongValue", MockSymbolFactory.PrimitiveTypes.Int64),
                CreatePropertyInfo("ULongValue", MockSymbolFactory.PrimitiveTypes.UInt64),
                CreatePropertyInfo("FloatValue", MockSymbolFactory.PrimitiveTypes.Single),
                CreatePropertyInfo("DoubleValue", MockSymbolFactory.PrimitiveTypes.Double),
                CreatePropertyInfo("DecimalValue", MockSymbolFactory.PrimitiveTypes.Decimal),
                
                // Nullable primitive types
                CreatePropertyInfo("NullableBoolValue", MockSymbolFactory.PrimitiveTypes.Boolean, isNullable: true),
                CreatePropertyInfo("NullableIntValue", MockSymbolFactory.PrimitiveTypes.Int32, isNullable: true),
                
                // Date/Time types
                CreatePropertyInfo("CreatedDate", MockSymbolFactory.PrimitiveTypes.DateTime),
                CreatePropertyInfo("UpdatedDate", MockSymbolFactory.PrimitiveTypes.DateTime, isNullable: true),
                CreatePropertyInfo("DateOnlyValue", MockSymbolFactory.PrimitiveTypes.DateOnly),
                CreatePropertyInfo("TimeOnlyValue", MockSymbolFactory.PrimitiveTypes.TimeOnly),
                
                // String types
                CreatePropertyInfo("Name", MockSymbolFactory.PrimitiveTypes.String),
                CreatePropertyInfo("Description", MockSymbolFactory.PrimitiveTypes.String, isNullable: true),
                
                // Enum types
                CreatePropertyInfo("Priority", CreateEnumType("Priority")),
                CreatePropertyInfo("OptionalPriority", CreateEnumType("Priority"), isNullable: true),
                
                // Collection types
                CreatePropertyInfo("Tags", CreateCollectionType(MockSymbolFactory.PrimitiveTypes.String), 
                    isCollection: true, elementType: MockSymbolFactory.PrimitiveTypes.String),
                CreatePropertyInfo("Numbers", CreateCollectionType(MockSymbolFactory.PrimitiveTypes.Int32), 
                    isCollection: true, elementType: MockSymbolFactory.PrimitiveTypes.Int32),
                
                // Guid
                CreatePropertyInfo("UniqueId", MockSymbolFactory.PrimitiveTypes.Guid),
                CreatePropertyInfo("OptionalId", MockSymbolFactory.PrimitiveTypes.Guid, isNullable: true)
            };
            
            var attributes = new List<AttributeInfo>
            {
                CreateDynamoModelAttribute("COMPLEX#<Id>", "DATA#<Category>", "CustomPK", "CustomSK"),
                CreateGSIAttribute("TypeIndex", "TYPE#<ModelType>", "COMPLEX#<Id>"),
                CreateGSIAttribute("StatusIndex", "STATUS#<Status>", "PRIORITY#<Priority>"),
                CreateGSIAttribute("DateIndex", "DATE#<CreatedDate>", "COMPLEX#<Id>", "DatePK", "DateSK")
            };
            
            return CreateDynamoTypeInfo(
                "ComplexTestModel",
                "TestNamespace.ComplexTestModel",
                properties: properties,
                attributes: attributes,
                isRecord: true);
        }
        
        /// <summary>
        /// Creates an enum type for testing.
        /// </summary>
        private static INamedTypeSymbol CreateEnumType(string name)
        {
            return MockSymbolFactory.CreateNamedTypeSymbol(
                name, 
                $"TestNamespace.{name}", 
                "TestNamespace", 
                typeKind: TypeKind.Enum).Object;
        }
        
        /// <summary>
        /// Creates a collection type (IEnumerable&lt;T&gt;) for testing.
        /// </summary>
        private static INamedTypeSymbol CreateCollectionType(ITypeSymbol elementType)
        {
            var collectionMock = MockSymbolFactory.CreateNamedTypeSymbol(
                "IEnumerable",
                $"System.Collections.Generic.IEnumerable<{elementType.ToDisplayString()}>",
                "System.Collections.Generic");
                
            // Setup as generic type with type arguments
            collectionMock.Setup(x => x.IsGenericType).Returns(true);
            collectionMock.Setup(x => x.TypeArguments).Returns(ImmutableArray.Create(elementType));
            
            return collectionMock.Object;
        }
    }
}