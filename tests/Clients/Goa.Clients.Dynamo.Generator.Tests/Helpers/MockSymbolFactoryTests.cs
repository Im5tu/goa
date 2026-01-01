using Goa.Clients.Dynamo.Generator.Tests.Helpers;
using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Tests.Helpers;

/// <summary>
/// Tests for the MockSymbolFactory, specifically the open generic type fix.
/// Ensures that IEnumerable&lt;T&gt; interfaces are properly mocked with correct OriginalDefinition.
/// </summary>
public class MockSymbolFactoryTests
{
    [Test]
    public async Task CreateGenericType_ShouldHaveSeparateOriginalDefinition()
    {
        // Arrange & Act
        var listType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String);

        // Assert
        await Assert.That(listType.Object.OriginalDefinition)
            .IsNotSameReferenceAs(listType.Object)
            .Because("OriginalDefinition should be a separate mock representing the open generic type");
    }

    [Test]
    public async Task CreateGenericType_OriginalDefinition_ShouldBeOpenGeneric()
    {
        // Arrange & Act
        var listType = MockSymbolFactory.CreateGenericType(
            "IEnumerable",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String);

        var originalDefinition = listType.Object.OriginalDefinition as INamedTypeSymbol;

        // Assert
        await Assert.That(originalDefinition)
            .IsNotNull()
            .Because("OriginalDefinition should be an INamedTypeSymbol");

        await Assert.That(originalDefinition!.TypeArguments)
            .IsEmpty()
            .Because("Open generic types should have no type arguments");

        await Assert.That(originalDefinition.IsGenericType)
            .IsTrue()
            .Because("OriginalDefinition should still be marked as generic");
    }

    [Test]
    public async Task CreateGenericType_ToDisplayString_ShouldShowConstructedType()
    {
        // Arrange & Act
        var listType = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.Int32);

        var displayString = listType.Object.ToDisplayString();

        // Assert
        await Assert.That(displayString)
            .Contains("List<")
            .Because("Constructed type should show type arguments");

        await Assert.That(displayString)
            .Contains("System.Int32")
            .Or.Contains("int")
            .Because("Constructed type should show the element type");
    }

    [Test]
    public async Task CreateGenericType_OriginalDefinition_ToDisplayString_ShouldShowOpenGeneric()
    {
        // Arrange & Act
        var listType = MockSymbolFactory.CreateGenericType(
            "IEnumerable",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String);

        var originalDefinition = listType.Object.OriginalDefinition;
        var displayString = originalDefinition.ToDisplayString();

        // Assert
        await Assert.That(displayString)
            .Contains("IEnumerable<T>")
            .Because("Open generic should display with type parameter T");
    }

    [Test]
    public async Task IEnumerableInterface_IsDetectableByCollectionHandler()
    {
        // Arrange
        var collectionHandler = new CollectionTypeHandler();
        var enumerableType = MockSymbolFactory.CreateGenericType(
            "IEnumerable",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String);

        var property = TestModelBuilders.CreateCollectionPropertyInfo(
            "Items",
            enumerableType.Object,
            MockSymbolFactory.PrimitiveTypes.String);

        // Act
        var canHandle = collectionHandler.CanHandle(property);

        // Assert
        await Assert.That(canHandle)
            .IsTrue()
            .Because("CollectionTypeHandler should recognize IEnumerable<T> as a collection type");
    }

    [Test]
    public async Task IEnumerableInterface_AllInterfaces_ShouldIncludeSelf()
    {
        // Arrange & Act
        var enumerableType = MockSymbolFactory.CreateGenericType(
            "IEnumerable",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String);

        var allInterfaces = enumerableType.Object.AllInterfaces;

        // Assert
        await Assert.That(allInterfaces)
            .Count().IsGreaterThanOrEqualTo(1)
            .Because("IEnumerable<T> should include itself in AllInterfaces");

        var hasIEnumerableInterface = allInterfaces.Any(i =>
            i.Name == "IEnumerable" &&
            i.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic");

        await Assert.That(hasIEnumerableInterface)
            .IsTrue()
            .Because("AllInterfaces should contain IEnumerable<T> from System.Collections.Generic");
    }

    [Test]
    public async Task IEnumerableInterface_OriginalDefinition_ShouldMatchExpectedPattern()
    {
        // Arrange & Act
        var enumerableType = MockSymbolFactory.CreateGenericType(
            "IEnumerable",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.Int32);

        var interfaceInAllInterfaces = enumerableType.Object.AllInterfaces.FirstOrDefault(i => i.Name == "IEnumerable");
        var originalDef = interfaceInAllInterfaces?.OriginalDefinition;

        // Assert
        await Assert.That(originalDef)
            .IsNotNull()
            .Because("IEnumerable<T> in AllInterfaces should have an OriginalDefinition");

        var displayString = originalDef!.ToDisplayString();

        await Assert.That(displayString)
            .Contains("System.Collections.Generic.IEnumerable<T>")
            .Because("OriginalDefinition should be the open generic IEnumerable<T>");
    }

    [Test]
    public async Task GenericTypeComparison_ShouldWorkWithOriginalDefinition()
    {
        // Arrange
        var listOfStrings = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String);

        var listOfInts = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.Int32);

        // Act
        var stringsOriginal = listOfStrings.Object.OriginalDefinition;
        var intsOriginal = listOfInts.Object.OriginalDefinition;

        // Assert - Both constructed types should have the same OriginalDefinition name
        await Assert.That(stringsOriginal.Name)
            .IsEqualTo(intsOriginal.Name)
            .Because("List<string> and List<int> both have List`1 as OriginalDefinition");

        await Assert.That(stringsOriginal.ToDisplayString())
            .IsEqualTo(intsOriginal.ToDisplayString())
            .Because("Both should display as the same open generic type");
    }

    [Test]
    public async Task ConstructedType_ShouldHaveTypeArguments()
    {
        // Arrange & Act
        var dictionaryType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Int32);

        // Assert
        await Assert.That(dictionaryType.Object.TypeArguments)
            .Count().IsEqualTo(2)
            .Because("Dictionary<string, int> should have 2 type arguments");

        await Assert.That(dictionaryType.Object.IsGenericType)
            .IsTrue()
            .Because("Constructed type should be marked as generic");
    }

    [Test]
    public async Task OriginalDefinition_ShouldNotHaveTypeArguments()
    {
        // Arrange & Act
        var dictionaryType = MockSymbolFactory.CreateGenericType(
            "Dictionary",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String,
            MockSymbolFactory.PrimitiveTypes.Int32);

        var originalDef = dictionaryType.Object.OriginalDefinition as INamedTypeSymbol;

        // Assert
        await Assert.That(originalDef)
            .IsNotNull()
            .Because("OriginalDefinition should be an INamedTypeSymbol");

        await Assert.That(originalDef!.TypeArguments)
            .IsEmpty()
            .Because("Open generic Dictionary<TKey, TValue> should have no concrete type arguments");
    }

    [Test]
    public async Task CreateGenericTypeForInterface_ShouldUseTypeArgumentDisplayString()
    {
        // Arrange & Act
        var customType = MockSymbolFactory.CreateNamedTypeSymbol(
            "ProductInfo",
            "TestNamespace.ProductInfo",
            "TestNamespace").Object;

        var enumerableType = MockSymbolFactory.CreateGenericType(
            "IEnumerable",
            "System.Collections.Generic",
            customType);

        var displayString = enumerableType.Object.ToDisplayString();

        // Assert
        await Assert.That(displayString)
            .Contains("TestNamespace.ProductInfo")
            .Because("Constructed type should use the full type name from ToDisplayString(), not just Name");

        await Assert.That(displayString)
            .DoesNotContain("ProductInfo>")
            .Or.Contains("TestNamespace.ProductInfo>")
            .Because("Should use ToDisplayString() for better qualified names");
    }

    [Test]
    public async Task NestedGenericType_ShouldMaintainOriginalDefinitionChain()
    {
        // Arrange & Act
        var innerList = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            MockSymbolFactory.PrimitiveTypes.String);

        var outerList = MockSymbolFactory.CreateGenericType(
            "List",
            "System.Collections.Generic",
            innerList.Object);

        // Assert
        await Assert.That(outerList.Object.TypeArguments[0])
            .IsSameReferenceAs(innerList.Object)
            .Because("Nested generic type should reference the inner constructed type");

        var innerOriginal = innerList.Object.OriginalDefinition as INamedTypeSymbol;
        var outerOriginal = outerList.Object.OriginalDefinition as INamedTypeSymbol;

        await Assert.That(innerOriginal!.TypeArguments)
            .IsEmpty()
            .Because("Inner List<T> OriginalDefinition should be open generic");

        await Assert.That(outerOriginal!.TypeArguments)
            .IsEmpty()
            .Because("Outer List<T> OriginalDefinition should be open generic");
    }
}
