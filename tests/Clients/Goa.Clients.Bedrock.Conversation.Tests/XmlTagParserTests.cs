using Goa.Clients.Bedrock.Conversation.Internal;

namespace Goa.Clients.Bedrock.Conversation.Tests;

public class XmlTagParserTests
{
    [Test]
    public async Task Parse_WithMultipleTagsOfSameType_GroupsContents()
    {
        // Arrange
        var input = "<thinking>a</thinking><thinking>b</thinking><reasoning>c</reasoning> response";

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo("response");
        await Assert.That(tags).ContainsKey("thinking");
        await Assert.That(tags).ContainsKey("reasoning");
        await Assert.That(tags["thinking"]).Count().IsEqualTo(2);
        await Assert.That(tags["thinking"][0]).IsEqualTo("a");
        await Assert.That(tags["thinking"][1]).IsEqualTo("b");
        await Assert.That(tags["reasoning"]).Count().IsEqualTo(1);
        await Assert.That(tags["reasoning"][0]).IsEqualTo("c");
    }

    [Test]
    public async Task Parse_WithNoTags_ReturnsOriginalTextAndEmptyDictionary()
    {
        // Arrange
        var input = "just some plain text";

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo("just some plain text");
        await Assert.That(tags).IsEmpty();
    }

    [Test]
    public async Task Parse_WithEmptyString_ReturnsEmptyResults()
    {
        // Arrange
        var input = string.Empty;

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo(string.Empty);
        await Assert.That(tags).IsEmpty();
    }

    [Test]
    public async Task Parse_WithNullString_ReturnsEmptyResults()
    {
        // Arrange
        string? input = null;

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input!);

        // Assert
        await Assert.That(cleanedText).IsEqualTo(string.Empty);
        await Assert.That(tags).IsEmpty();
    }

    [Test]
    public async Task Parse_WithOnlyTags_ReturnsEmptyCleanedText()
    {
        // Arrange
        var input = "<thinking>some thought</thinking>";

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo(string.Empty);
        await Assert.That(tags["thinking"][0]).IsEqualTo("some thought");
    }

    [Test]
    public async Task Parse_WithMultilineTagContent_CapturesFullContent()
    {
        // Arrange
        var input = """
            <thinking>
            This is a
            multiline thought
            </thinking>
            The response text
            """;

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo("The response text");
        await Assert.That(tags["thinking"][0]).IsEqualTo("""
            This is a
            multiline thought
            """);
    }

    [Test]
    public async Task Parse_TrimsTagContents()
    {
        // Arrange
        var input = "<tag>  content with spaces  </tag>";

        // Act
        var (_, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(tags["tag"][0]).IsEqualTo("content with spaces");
    }

    [Test]
    public async Task Parse_TrimsCleanedText()
    {
        // Arrange
        var input = "  <tag>content</tag>  response with spaces  ";

        // Act
        var (cleanedText, _) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo("response with spaces");
    }

    [Test]
    public async Task Parse_WithNestedSameTagNames_MatchesFirstClosingTag()
    {
        // Arrange - note: this tests the non-greedy regex behavior
        var input = "<tag>first</tag><tag>second</tag>";

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo(string.Empty);
        await Assert.That(tags["tag"]).Count().IsEqualTo(2);
        await Assert.That(tags["tag"][0]).IsEqualTo("first");
        await Assert.That(tags["tag"][1]).IsEqualTo("second");
    }

    [Test]
    public async Task Parse_WithMismatchedTags_DoesNotMatch()
    {
        // Arrange
        var input = "<thinking>content</reasoning> response";

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo("<thinking>content</reasoning> response");
        await Assert.That(tags).IsEmpty();
    }

    [Test]
    public async Task Parse_WithUnclosedTag_DoesNotMatch()
    {
        // Arrange
        var input = "<thinking>unclosed content response";

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo("<thinking>unclosed content response");
        await Assert.That(tags).IsEmpty();
    }

    [Test]
    public async Task Parse_WithEmptyTagContent_ReturnsEmptyString()
    {
        // Arrange
        var input = "<empty></empty> response";

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo("response");
        await Assert.That(tags["empty"][0]).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Parse_WithTagInMiddleOfText_RemovesTagOnly()
    {
        // Arrange
        var input = "before <tag>content</tag> after";

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo("before  after");
        await Assert.That(tags["tag"][0]).IsEqualTo("content");
    }

    [Test]
    public async Task Parse_WithSpecialCharactersInContent_PreservesCharacters()
    {
        // Arrange
        var input = "<code>var x = 1 < 2 && 3 > 2;</code> result";

        // Act
        var (cleanedText, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(cleanedText).IsEqualTo("result");
        await Assert.That(tags["code"][0]).IsEqualTo("var x = 1 < 2 && 3 > 2;");
    }

    [Test]
    public async Task Parse_ReturnsReadOnlyCollections()
    {
        // Arrange
        var input = "<tag>content</tag>";

        // Act
        var (_, tags) = XmlTagParser.Parse(input);

        // Assert
        await Assert.That(tags).IsAssignableTo<IReadOnlyDictionary<string, IReadOnlyList<string>>>();
        await Assert.That(tags["tag"]).IsAssignableTo<IReadOnlyList<string>>();
    }
}
