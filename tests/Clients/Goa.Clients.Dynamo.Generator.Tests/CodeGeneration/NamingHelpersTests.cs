using Goa.Clients.Dynamo.Generator.CodeGeneration;

namespace Goa.Clients.Dynamo.Generator.Tests.CodeGeneration;

public class NamingHelpersTests
{
    [Test]
    public async Task NormalizeTypeName_WithSimpleName_ShouldReturnUnchanged()
    {
        // Arrange
        var typeName = "User";

        // Act
        var result = NamingHelpers.NormalizeTypeName(typeName);

        // Assert
        await Assert.That(result)
            .IsEqualTo("User");
    }

    [Test]
    public async Task NormalizeTypeName_WithNamespaceDots_ShouldReplaceWithUnderscore()
    {
        // Arrange
        var typeName = "MyNamespace.SubNamespace.User";

        // Act
        var result = NamingHelpers.NormalizeTypeName(typeName);

        // Assert
        await Assert.That(result)
            .IsEqualTo("MyNamespace_SubNamespace_User");
    }

    [Test]
    public async Task NormalizeTypeName_WithGenericBackticks_ShouldReplaceWithUnderscore()
    {
        // Arrange
        var typeName = "List`1";

        // Act
        var result = NamingHelpers.NormalizeTypeName(typeName);

        // Assert
        await Assert.That(result)
            .IsEqualTo("List_1");
    }

    [Test]
    public async Task NormalizeTypeName_WithBothDotsAndBackticks_ShouldReplaceAll()
    {
        // Arrange
        var typeName = "System.Collections.Generic.Dictionary`2";

        // Act
        var result = NamingHelpers.NormalizeTypeName(typeName);

        // Assert
        await Assert.That(result)
            .IsEqualTo("System_Collections_Generic_Dictionary_2");
    }

    [Test]
    public async Task ExtractPlaceholders_WithNoPlaceholders_ShouldReturnEmptyList()
    {
        // Arrange
        var pattern = "STATIC_KEY";

        // Act
        var result = NamingHelpers.ExtractPlaceholders(pattern);

        // Assert
        await Assert.That(result)
            .Count().IsEqualTo(0);
    }

    [Test]
    public async Task ExtractPlaceholders_WithSinglePlaceholder_ShouldReturnSingleItem()
    {
        // Arrange
        var pattern = "USER#<Id>";

        // Act
        var result = NamingHelpers.ExtractPlaceholders(pattern);

        // Assert
        await Assert.That(result)
            .Count().IsEqualTo(1);
        await Assert.That(result[0])
            .IsEqualTo("Id");
    }

    [Test]
    public async Task ExtractPlaceholders_WithMultiplePlaceholders_ShouldReturnAllPlaceholders()
    {
        // Arrange
        var pattern = "ENTITY#<Type>#<Id>#<Category>";

        // Act
        var result = NamingHelpers.ExtractPlaceholders(pattern);

        // Assert
        await Assert.That(result)
            .Count().IsEqualTo(3);
        await Assert.That(result[0])
            .IsEqualTo("Type");
        await Assert.That(result[1])
            .IsEqualTo("Id");
        await Assert.That(result[2])
            .IsEqualTo("Category");
    }

    [Test]
    public async Task ExtractPlaceholders_WithComplexNames_ShouldExtractCorrectly()
    {
        // Arrange
        var pattern = "PREFIX#<PropertyName>#<AnotherProperty>";

        // Act
        var result = NamingHelpers.ExtractPlaceholders(pattern);

        // Assert
        await Assert.That(result)
            .Count().IsEqualTo(2);
        await Assert.That(result[0])
            .IsEqualTo("PropertyName");
        await Assert.That(result[1])
            .IsEqualTo("AnotherProperty");
    }

    [Test]
    public async Task ExtractPlaceholders_WithEmptyPattern_ShouldReturnEmptyList()
    {
        // Arrange
        var pattern = "";

        // Act
        var result = NamingHelpers.ExtractPlaceholders(pattern);

        // Assert
        await Assert.That(result)
            .Count().IsEqualTo(0);
    }

    [Test]
    public async Task FormatKeyPattern_WithNoPlaceholders_ShouldReturnOriginal()
    {
        // Arrange
        var pattern = "STATIC_KEY";
        var replacements = new Dictionary<string, string>();

        // Act
        var result = NamingHelpers.FormatKeyPattern(pattern, replacements);

        // Assert
        await Assert.That(result)
            .IsEqualTo("STATIC_KEY");
    }

    [Test]
    public async Task FormatKeyPattern_WithSingleReplacement_ShouldReplacePlaceholder()
    {
        // Arrange
        var pattern = "USER#<Id>";
        var replacements = new Dictionary<string, string>
        {
            { "Id", "123" }
        };

        // Act
        var result = NamingHelpers.FormatKeyPattern(pattern, replacements);

        // Assert
        await Assert.That(result)
            .IsEqualTo("USER#123");
    }

    [Test]
    public async Task FormatKeyPattern_WithMultipleReplacements_ShouldReplaceAllPlaceholders()
    {
        // Arrange
        var pattern = "ENTITY#<Type>#<Id>#<Category>";
        var replacements = new Dictionary<string, string>
        {
            { "Type", "USER" },
            { "Id", "123" },
            { "Category", "ADMIN" }
        };

        // Act
        var result = NamingHelpers.FormatKeyPattern(pattern, replacements);

        // Assert
        await Assert.That(result)
            .IsEqualTo("ENTITY#USER#123#ADMIN");
    }

    [Test]
    public async Task FormatKeyPattern_WithMissingReplacement_ShouldLeavePlaceholder()
    {
        // Arrange
        var pattern = "USER#<Id>#<Category>";
        var replacements = new Dictionary<string, string>
        {
            { "Id", "123" }
            // Missing Category replacement
        };

        // Act
        var result = NamingHelpers.FormatKeyPattern(pattern, replacements);

        // Assert
        await Assert.That(result)
            .IsEqualTo("USER#123#<Category>");
    }

    [Test]
    public async Task FormatKeyPattern_WithEmptyReplacement_ShouldReplaceWithEmpty()
    {
        // Arrange
        var pattern = "USER#<Id>";
        var replacements = new Dictionary<string, string>
        {
            { "Id", "" }
        };

        // Act
        var result = NamingHelpers.FormatKeyPattern(pattern, replacements);

        // Assert
        await Assert.That(result)
            .IsEqualTo("USER#");
    }

    [Test]
    public async Task ToVariableName_WithUppercaseStart_ShouldMakeLowercase()
    {
        // Arrange
        var propertyName = "UserId";

        // Act
        var result = NamingHelpers.ToVariableName(propertyName);

        // Assert
        await Assert.That(result)
            .IsEqualTo("userId");
    }

    [Test]
    public async Task ToVariableName_WithLowercaseStart_ShouldRemainLowercase()
    {
        // Arrange
        var propertyName = "userId";

        // Act
        var result = NamingHelpers.ToVariableName(propertyName);

        // Assert
        await Assert.That(result)
            .IsEqualTo("userId");
    }

    [Test]
    public async Task ToVariableName_WithSingleCharacter_ShouldMakeLowercase()
    {
        // Arrange
        var propertyName = "X";

        // Act
        var result = NamingHelpers.ToVariableName(propertyName);

        // Assert
        await Assert.That(result)
            .IsEqualTo("x");
    }

    [Test]
    public async Task ToVariableName_WithComplexName_ShouldOnlyChangFirstChar()
    {
        // Arrange
        var propertyName = "EmailAddressProperty";

        // Act
        var result = NamingHelpers.ToVariableName(propertyName);

        // Assert
        await Assert.That(result)
            .IsEqualTo("emailAddressProperty");
    }

    [Test]
    public async Task IsValidIdentifier_WithEmptyString_ShouldReturnFalse()
    {
        // Arrange
        var identifier = "";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task IsValidIdentifier_WithNull_ShouldReturnFalse()
    {
        // Arrange
        string? identifier = null;

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier!);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task IsValidIdentifier_WithValidName_ShouldReturnTrue()
    {
        // Arrange
        var identifier = "ValidIdentifier";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task IsValidIdentifier_WithUnderscore_ShouldReturnTrue()
    {
        // Arrange
        var identifier = "_validIdentifier";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task IsValidIdentifier_WithNumbers_ShouldReturnTrue()
    {
        // Arrange
        var identifier = "identifier123";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task IsValidIdentifier_WithUnderscoreInMiddle_ShouldReturnTrue()
    {
        // Arrange
        var identifier = "valid_identifier_name";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task IsValidIdentifier_StartingWithNumber_ShouldReturnFalse()
    {
        // Arrange
        var identifier = "123invalid";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task IsValidIdentifier_WithInvalidCharacters_ShouldReturnFalse()
    {
        // Arrange
        var identifier = "invalid-identifier";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task IsValidIdentifier_WithSpaces_ShouldReturnFalse()
    {
        // Arrange
        var identifier = "invalid identifier";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task IsValidIdentifier_WithSpecialCharacters_ShouldReturnFalse()
    {
        // Arrange
        var identifier = "invalid@identifier";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsFalse();
    }

    [Test]
    public async Task IsValidIdentifier_WithJustUnderscore_ShouldReturnTrue()
    {
        // Arrange
        var identifier = "_";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task IsValidIdentifier_WithKeywords_ShouldReturnTrue()
    {
        // Arrange - Even though these are C# keywords, they are valid identifiers syntactically
        var identifier = "class";

        // Act
        var result = NamingHelpers.IsValidIdentifier(identifier);

        // Assert
        await Assert.That(result)
            .IsTrue();
    }

    [Test]
    public async Task ExtractPlaceholders_WithNestedBrackets_ShouldHandleCorrectly()
    {
        // Arrange
        var pattern = "COMPLEX#<Outer<Inner>>";

        // Act
        var result = NamingHelpers.ExtractPlaceholders(pattern);

        // Assert
        await Assert.That(result)
            .Count().IsEqualTo(1);
        await Assert.That(result[0])
            .IsEqualTo("Outer<Inner");
    }

    [Test]
    public async Task FormatKeyPattern_WithDuplicatePlaceholders_ShouldReplaceAll()
    {
        // Arrange
        var pattern = "PREFIX#<Id>#MIDDLE#<Id>#SUFFIX";
        var replacements = new Dictionary<string, string>
        {
            { "Id", "123" }
        };

        // Act
        var result = NamingHelpers.FormatKeyPattern(pattern, replacements);

        // Assert
        await Assert.That(result)
            .IsEqualTo("PREFIX#123#MIDDLE#123#SUFFIX");
    }

    [Test]
    public async Task ToVariableName_WithNumberPrefix_ShouldOnlyChangeCasing()
    {
        // Arrange
        var propertyName = "Property1Name";

        // Act
        var result = NamingHelpers.ToVariableName(propertyName);

        // Assert
        await Assert.That(result)
            .IsEqualTo("property1Name");
    }
}
