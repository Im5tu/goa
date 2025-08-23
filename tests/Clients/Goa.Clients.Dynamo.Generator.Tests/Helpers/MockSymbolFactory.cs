using Microsoft.CodeAnalysis;
using Moq;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Goa.Clients.Dynamo.Generator.Tests.Helpers;

/// <summary>
/// Factory for creating mock Roslyn symbols for testing purposes.
/// </summary>
public static class MockSymbolFactory
{
    /// <summary>
    /// Creates a mock INamedTypeSymbol with the specified properties.
    /// </summary>
    public static Mock<INamedTypeSymbol> CreateNamedTypeSymbol(
        string name,
        string fullName,
        string namespaceName = "",
        bool isAbstract = false,
        bool isRecord = false,
        TypeKind typeKind = TypeKind.Class,
        SpecialType specialType = SpecialType.None,
        INamedTypeSymbol? baseType = null,
        ImmutableArray<AttributeData>? attributes = null,
        ImmutableArray<ISymbol>? members = null)
    {
        var mock = new Mock<INamedTypeSymbol>();
        
        mock.Setup(x => x.Name).Returns(name);
        mock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat?>())).Returns(fullName);
        mock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>())).Returns(fullName);
        mock.Setup(x => x.IsAbstract).Returns(isAbstract);
        mock.Setup(x => x.IsRecord).Returns(isRecord);
        mock.Setup(x => x.TypeKind).Returns(typeKind);
        mock.Setup(x => x.SpecialType).Returns(specialType);
        mock.Setup(x => x.BaseType).Returns(baseType);
        
        // Setup OriginalDefinition to return self for non-generic types
        mock.Setup(x => x.OriginalDefinition).Returns(mock.Object);
        
        // Setup other commonly used properties
        mock.Setup(x => x.IsGenericType).Returns(false);
        mock.Setup(x => x.TypeArguments).Returns(ImmutableArray<ITypeSymbol>.Empty);
        mock.Setup(x => x.CanBeReferencedByName).Returns(true);
        
        // Create mock namespace
        var namespaceMock = new Mock<INamespaceSymbol>();
        namespaceMock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat?>())).Returns(namespaceName);
        mock.Setup(x => x.ContainingNamespace).Returns(namespaceMock.Object);
        
        // Setup attributes
        mock.Setup(x => x.GetAttributes()).Returns(attributes ?? ImmutableArray<AttributeData>.Empty);
        
        // Setup members
        mock.Setup(x => x.GetMembers()).Returns(members ?? ImmutableArray<ISymbol>.Empty);
        
        // Setup constructors (empty by default)
        mock.Setup(x => x.Constructors).Returns(ImmutableArray<IMethodSymbol>.Empty);
        
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IPropertySymbol with the specified properties.
    /// </summary>
    public static Mock<IPropertySymbol> CreatePropertySymbol(
        string name,
        ITypeSymbol type,
        NullableAnnotation nullableAnnotation = NullableAnnotation.NotAnnotated,
        ImmutableArray<AttributeData>? attributes = null,
        bool hasGetter = true,
        bool hasSetter = true,
        Location? location = null)
    {
        var mock = new Mock<IPropertySymbol>();
        
        mock.Setup(x => x.Name).Returns(name);
        mock.Setup(x => x.Type).Returns(type);
        mock.Setup(x => x.NullableAnnotation).Returns(nullableAnnotation);
        mock.Setup(x => x.GetAttributes()).Returns(attributes ?? ImmutableArray<AttributeData>.Empty);
        
        // Setup getter and setter
        if (hasGetter)
        {
            var getterMock = new Mock<IMethodSymbol>();
            mock.Setup(x => x.GetMethod).Returns(getterMock.Object);
        }
        
        if (hasSetter)
        {
            var setterMock = new Mock<IMethodSymbol>();
            mock.Setup(x => x.SetMethod).Returns(setterMock.Object);
        }
        
        // Setup locations
        var locations = location != null ? ImmutableArray.Create(location) : ImmutableArray<Location>.Empty;
        mock.Setup(x => x.Locations).Returns(locations);
        
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IParameterSymbol with the specified properties.
    /// </summary>
    public static Mock<IParameterSymbol> CreateParameterSymbol(
        string name,
        ITypeSymbol type,
        NullableAnnotation nullableAnnotation = NullableAnnotation.NotAnnotated,
        bool hasExplicitDefaultValue = false,
        object? explicitDefaultValue = null)
    {
        var mock = new Mock<IParameterSymbol>();
        
        mock.Setup(x => x.Name).Returns(name);
        mock.Setup(x => x.Type).Returns(type);
        mock.Setup(x => x.NullableAnnotation).Returns(nullableAnnotation);
        mock.Setup(x => x.HasExplicitDefaultValue).Returns(hasExplicitDefaultValue);
        mock.Setup(x => x.ExplicitDefaultValue).Returns(explicitDefaultValue);
        
        return mock;
    }
    
    /// <summary>
    /// Creates a mock IMethodSymbol for a constructor with the specified parameters.
    /// </summary>
    public static Mock<IMethodSymbol> CreateConstructorSymbol(
        INamedTypeSymbol containingType,
        params IParameterSymbol[] parameters)
    {
        var mock = new Mock<IMethodSymbol>();
        
        mock.Setup(x => x.MethodKind).Returns(MethodKind.Constructor);
        mock.Setup(x => x.ContainingType).Returns(containingType);
        mock.Setup(x => x.DeclaredAccessibility).Returns(Accessibility.Public);
        mock.Setup(x => x.Parameters).Returns(ImmutableArray.Create(parameters));
        
        return mock;
    }
    
    /// <summary>
    /// Creates a simple AttributeData implementation for testing.
    /// Since AttributeData is abstract and has non-virtual members, we create a minimal implementation.
    /// </summary>
    public static AttributeData CreateAttributeData(
        string attributeClassName,
        object[]? constructorArgs = null,
        Dictionary<string, object?>? namedArgs = null)
    {
        // Create a simple implementation that extends AttributeData
        return new TestAttributeData(attributeClassName, constructorArgs, namedArgs);
    }
    
    /// <summary>
    /// Simple test implementation of AttributeData for mocking purposes.
    /// </summary>
    private class TestAttributeData : AttributeData
    {
        private readonly INamedTypeSymbol _attributeClass;
        private readonly ImmutableArray<TypedConstant> _constructorArguments;
        private readonly ImmutableArray<KeyValuePair<string, TypedConstant>> _namedArguments;
        
        public TestAttributeData(string attributeClassName, object[]? constructorArgs = null, Dictionary<string, object?>? namedArgs = null)
        {
            _attributeClass = CreateNamedTypeSymbol(
                attributeClassName.Split('.').Last(),
                attributeClassName).Object;
            
            // Convert constructor arguments to TypedConstants
            var constructorConstants = constructorArgs?.Select(arg => CreateTypedConstant(arg)).ToArray() ?? [];
            _constructorArguments = ImmutableArray.Create(constructorConstants);
            
            // Convert named arguments to TypedConstants
            var namedConstants = namedArgs?.Select(kvp => new KeyValuePair<string, TypedConstant>(
                kvp.Key, CreateTypedConstant(kvp.Value))).ToArray() ?? [];
            _namedArguments = ImmutableArray.Create(namedConstants);
        }
        
        private static TypedConstant CreateTypedConstant(object? value)
        {
            if (value == null)
                return default(TypedConstant);
                
            var typeSymbol = GetTypeSymbolForValue(value);
            
            // Create a mock TypedConstant implementation that returns the value
            return new MockTypedConstant(typeSymbol, value);
        }
        
        protected override INamedTypeSymbol? CommonAttributeClass => _attributeClass;
        
        protected override SyntaxReference? CommonApplicationSyntaxReference => null;
        
        protected override IMethodSymbol? CommonAttributeConstructor => null;
        
        protected override ImmutableArray<TypedConstant> CommonConstructorArguments => _constructorArguments;
        
        protected override ImmutableArray<KeyValuePair<string, TypedConstant>> CommonNamedArguments => _namedArguments;
    }
    
    /// <summary>
    /// A mock implementation of TypedConstant that properly exposes values
    /// </summary>
    internal struct MockTypedConstant
    {
        private readonly ITypeSymbol _type;
        private readonly object? _value;
        
        public MockTypedConstant(ITypeSymbol type, object? value)
        {
            _type = type;
            _value = value;
        }
        
        public static implicit operator TypedConstant(MockTypedConstant mock)
        {
            // Create the TypedConstant using the available static factory methods
            var kind = mock._value switch
            {
                null => TypedConstantKind.Primitive,
                string => TypedConstantKind.Primitive,
                int => TypedConstantKind.Primitive,
                bool => TypedConstantKind.Primitive,
                Enum => TypedConstantKind.Enum,
                _ => TypedConstantKind.Primitive
            };
            
            // Use reflection to create a TypedConstant with proper values
            var typedConstantType = typeof(TypedConstant);
            var constructors = typedConstantType.GetConstructors(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);
            
            // Try to find a constructor that accepts (ITypeSymbol, TypedConstantKind, object)
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 3 &&
                    parameters[0].ParameterType == typeof(ITypeSymbol) &&
                    parameters[1].ParameterType == typeof(TypedConstantKind) &&
                    parameters[2].ParameterType == typeof(object))
                {
                    return (TypedConstant)constructor.Invoke(new object[] { mock._type, kind, mock._value! });
                }
            }
            
            // If no constructor found, try using the struct fields directly
            var result = default(TypedConstant);
            
            // Get the backing fields
            var fields = typedConstantType.GetFields(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            object boxed = result;
            foreach (var field in fields)
            {
                if (field.Name.Contains("Kind", StringComparison.OrdinalIgnoreCase))
                {
                    field.SetValue(boxed, kind);
                }
                else if (field.Name.Contains("Type", StringComparison.OrdinalIgnoreCase) && field.FieldType == typeof(ITypeSymbol))
                {
                    field.SetValue(boxed, mock._type);
                }
                else if (field.Name.Contains("Value", StringComparison.OrdinalIgnoreCase) && field.FieldType == typeof(object))
                {
                    field.SetValue(boxed, mock._value);
                }
            }
            
            return (TypedConstant)boxed;
        }
    }
    
    /// <summary>
    /// Creates primitive type symbols for common .NET types.
    /// </summary>
    public static class PrimitiveTypes
    {
        public static INamedTypeSymbol String => CreateNamedTypeSymbol(
            "String", "string", "System", specialType: SpecialType.System_String).Object;
            
        public static INamedTypeSymbol Int32 => CreateNamedTypeSymbol(
            "Int32", "int", "System", specialType: SpecialType.System_Int32).Object;
            
        public static INamedTypeSymbol Boolean => CreateNamedTypeSymbol(
            "Boolean", "bool", "System", specialType: SpecialType.System_Boolean).Object;
            
        public static INamedTypeSymbol DateTime => CreateNamedTypeSymbol(
            "DateTime", "System.DateTime", "System", specialType: SpecialType.System_DateTime).Object;
            
        public static INamedTypeSymbol DateTimeOffset => CreateNamedTypeSymbol(
            "DateTimeOffset", "System.DateTimeOffset", "System").Object;
            
        public static INamedTypeSymbol DateOnly => CreateNamedTypeSymbol(
            "DateOnly", "System.DateOnly", "System").Object;
            
        public static INamedTypeSymbol TimeOnly => CreateNamedTypeSymbol(
            "TimeOnly", "System.TimeOnly", "System").Object;
            
        public static INamedTypeSymbol Guid => CreateNamedTypeSymbol(
            "Guid", "System.Guid", "System").Object;
            
        public static INamedTypeSymbol Decimal => CreateNamedTypeSymbol(
            "Decimal", "decimal", "System", specialType: SpecialType.System_Decimal).Object;
            
        public static INamedTypeSymbol Double => CreateNamedTypeSymbol(
            "Double", "double", "System", specialType: SpecialType.System_Double).Object;
            
        public static INamedTypeSymbol Single => CreateNamedTypeSymbol(
            "Single", "float", "System", specialType: SpecialType.System_Single).Object;
            
        public static INamedTypeSymbol Byte => CreateNamedTypeSymbol(
            "Byte", "byte", "System", specialType: SpecialType.System_Byte).Object;
            
        public static INamedTypeSymbol SByte => CreateNamedTypeSymbol(
            "SByte", "sbyte", "System", specialType: SpecialType.System_SByte).Object;
            
        public static INamedTypeSymbol Int16 => CreateNamedTypeSymbol(
            "Int16", "short", "System", specialType: SpecialType.System_Int16).Object;
            
        public static INamedTypeSymbol UInt16 => CreateNamedTypeSymbol(
            "UInt16", "ushort", "System", specialType: SpecialType.System_UInt16).Object;
            
        public static INamedTypeSymbol UInt32 => CreateNamedTypeSymbol(
            "UInt32", "uint", "System", specialType: SpecialType.System_UInt32).Object;
            
        public static INamedTypeSymbol Int64 => CreateNamedTypeSymbol(
            "Int64", "long", "System", specialType: SpecialType.System_Int64).Object;
            
        public static INamedTypeSymbol UInt64 => CreateNamedTypeSymbol(
            "UInt64", "ulong", "System", specialType: SpecialType.System_UInt64).Object;
            
        public static INamedTypeSymbol Char => CreateNamedTypeSymbol(
            "Char", "char", "System", specialType: SpecialType.System_Char).Object;
    }
    
    /// <summary>
    /// Creates a mock IArrayTypeSymbol with the specified element type.
    /// </summary>
    public static Mock<IArrayTypeSymbol> CreateArrayType(ITypeSymbol elementType)
    {
        var mock = new Mock<IArrayTypeSymbol>();
        mock.Setup(x => x.ElementType).Returns(elementType);
        mock.Setup(x => x.TypeKind).Returns(TypeKind.Array);
        return mock;
    }
    
    /// <summary>
    /// Creates a mock generic type (like List&lt;T&gt; or Dictionary&lt;TKey, TValue&gt;).
    /// </summary>
    public static Mock<INamedTypeSymbol> CreateGenericType(string typeName, string namespaceName, params ITypeSymbol[] typeArguments)
    {
        var fullTypeName = typeArguments.Length > 0 
            ? $"{namespaceName}.{typeName}<{string.Join(", ", typeArguments.Select(t => t.Name))}>"
            : $"{namespaceName}.{typeName}";
            
        var mock = CreateNamedTypeSymbol(typeName, fullTypeName, namespaceName);
        mock.Setup(x => x.IsGenericType).Returns(true);
        mock.Setup(x => x.TypeArguments).Returns(ImmutableArray.Create(typeArguments));
        
        // For nullable types, set up OriginalDefinition to have System_Nullable_T
        if (typeName == "Nullable" && namespaceName == "System" && typeArguments.Length == 1)
        {
            var originalDef = new Mock<INamedTypeSymbol>();
            originalDef.Setup(x => x.SpecialType).Returns(SpecialType.System_Nullable_T);
            originalDef.Setup(x => x.Name).Returns("Nullable");
            mock.Setup(x => x.OriginalDefinition).Returns(originalDef.Object);
        }
        
        return mock;
    }
    
    /// <summary>
    /// Creates a nullable value type (like int?, DateOnly?, etc.)
    /// </summary>
    public static Mock<INamedTypeSymbol> CreateNullableValueType(ITypeSymbol underlyingType)
    {
        return CreateGenericType("Nullable", "System", underlyingType);
    }
    
    /// <summary>
    /// Creates a nullable type (alias for CreateNullableValueType for consistency)
    /// </summary>
    public static Mock<INamedTypeSymbol> CreateNullableType(ITypeSymbol underlyingType)
    {
        return CreateNullableValueType(underlyingType);
    }
    
    private static ITypeSymbol GetTypeSymbolForValue(object? value)
    {
        return value switch
        {
            null => PrimitiveTypes.String, // Default for null
            string => PrimitiveTypes.String,
            int => PrimitiveTypes.Int32,
            bool => PrimitiveTypes.Boolean,
            _ => PrimitiveTypes.String // Default fallback
        };
    }
}