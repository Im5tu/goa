using System.Text.Json;
using Goa.Clients.Bedrock.Operations.ApplyGuardrail;
using Goa.Clients.Bedrock.Serialization;

namespace Goa.Clients.Bedrock.Tests;

public class ApplyGuardrailSerializationTests
{
    [Test]
    public async Task ApplyGuardrailRequest_ShouldSerializeCorrectly()
    {
        // Arrange
        var request = new ApplyGuardrailRequest
        {
            GuardrailIdentifier = "my-guardrail",
            GuardrailVersion = "1",
            Source = "INPUT",
            Content =
            [
                new GuardrailContentBlock
                {
                    Text = new GuardrailTextBlock
                    {
                        Text = "Test content",
                        Qualifiers = [GuardrailTextQualifier.Query]
                    }
                }
            ]
        };

        // Act
        var json = JsonSerializer.Serialize(request, BedrockJsonContext.Default.ApplyGuardrailRequest);
        var deserialized = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.ApplyGuardrailRequest);

        // Assert
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.GuardrailIdentifier).IsEqualTo("my-guardrail");
        await Assert.That(deserialized.GuardrailVersion).IsEqualTo("1");
        await Assert.That(deserialized.Source).IsEqualTo("INPUT");
        await Assert.That(deserialized.Content).Count().IsEqualTo(1);
        await Assert.That(deserialized.Content[0].Text!.Text).IsEqualTo("Test content");
        await Assert.That(deserialized.Content[0].Text!.Qualifiers).Count().IsEqualTo(1);
    }

    [Test]
    public async Task ApplyGuardrailResponse_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "action": "GUARDRAIL_INTERVENED",
            "outputs": [
                { "text": "Blocked content" }
            ],
            "assessments": [
                {
                    "topicPolicy": {
                        "topics": [
                            { "name": "Harmful", "type": "DENY", "action": "BLOCKED" }
                        ]
                    },
                    "contentPolicy": {
                        "filters": [
                            { "type": "VIOLENCE", "confidence": "HIGH", "action": "BLOCKED" }
                        ]
                    },
                    "wordPolicy": {
                        "customWords": [
                            { "match": "badword", "action": "BLOCKED" }
                        ],
                        "managedWordLists": [
                            { "match": "profanity", "type": "PROFANITY", "action": "BLOCKED" }
                        ]
                    },
                    "sensitiveInformationPolicy": {
                        "piiEntities": [
                            { "type": "EMAIL", "match": "test@example.com", "action": "ANONYMIZED" }
                        ],
                        "regexes": [
                            { "name": "SSN", "match": "123-45-6789", "action": "BLOCKED" }
                        ]
                    }
                }
            ],
            "usage": {
                "topicPolicyUnits": 1,
                "contentPolicyUnits": 2,
                "wordPolicyUnits": 3,
                "sensitiveInformationPolicyUnits": 4,
                "sensitiveInformationPolicyFreeUnits": 5
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.ApplyGuardrailResponse);

        // Assert
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Action).IsEqualTo("GUARDRAIL_INTERVENED");
        await Assert.That(response.Outputs).Count().IsEqualTo(1);
        await Assert.That(response.Outputs[0].Text).IsEqualTo("Blocked content");
        
        // Assessments
        await Assert.That(response.Assessments).Count().IsEqualTo(1);
        var assessment = response.Assessments![0];
        
        // Topic policy
        await Assert.That(assessment.TopicPolicy!.Topics).Count().IsEqualTo(1);
        await Assert.That(assessment.TopicPolicy.Topics![0].Name).IsEqualTo("Harmful");
        await Assert.That(assessment.TopicPolicy.Topics[0].Type).IsEqualTo("DENY");
        await Assert.That(assessment.TopicPolicy.Topics[0].Action).IsEqualTo("BLOCKED");
        
        // Content policy
        await Assert.That(assessment.ContentPolicy!.Filters).Count().IsEqualTo(1);
        await Assert.That(assessment.ContentPolicy.Filters![0].Type).IsEqualTo("VIOLENCE");
        await Assert.That(assessment.ContentPolicy.Filters[0].Confidence).IsEqualTo("HIGH");
        
        // Word policy
        await Assert.That(assessment.WordPolicy!.CustomWords).Count().IsEqualTo(1);
        await Assert.That(assessment.WordPolicy.CustomWords![0].Match).IsEqualTo("badword");
        await Assert.That(assessment.WordPolicy.ManagedWordLists).Count().IsEqualTo(1);
        await Assert.That(assessment.WordPolicy.ManagedWordLists![0].Type).IsEqualTo("PROFANITY");
        
        // Sensitive information policy
        await Assert.That(assessment.SensitiveInformationPolicy!.PiiEntities).Count().IsEqualTo(1);
        await Assert.That(assessment.SensitiveInformationPolicy.PiiEntities![0].Type).IsEqualTo("EMAIL");
        await Assert.That(assessment.SensitiveInformationPolicy.Regexes).Count().IsEqualTo(1);
        await Assert.That(assessment.SensitiveInformationPolicy.Regexes![0].Name).IsEqualTo("SSN");
        
        // Usage
        await Assert.That(response.Usage!.TopicPolicyUnits).IsEqualTo(1);
        await Assert.That(response.Usage.ContentPolicyUnits).IsEqualTo(2);
        await Assert.That(response.Usage.WordPolicyUnits).IsEqualTo(3);
        await Assert.That(response.Usage.SensitiveInformationPolicyUnits).IsEqualTo(4);
        await Assert.That(response.Usage.SensitiveInformationPolicyFreeUnits).IsEqualTo(5);
    }

    [Test]
    public async Task ApplyGuardrailResponse_WithNoIntervention_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
            "action": "NONE",
            "outputs": [
                { "text": "Original content" }
            ]
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize(json, BedrockJsonContext.Default.ApplyGuardrailResponse);

        // Assert
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Action).IsEqualTo("NONE");
        await Assert.That(response.Outputs).Count().IsEqualTo(1);
        await Assert.That(response.Assessments).IsNull();
        await Assert.That(response.Usage).IsNull();
    }

    [Test]
    public async Task GuardrailTextQualifier_ShouldSerializeWithSnakeCase()
    {
        // Arrange
        var qualifiers = new List<GuardrailTextQualifier>
        {
            GuardrailTextQualifier.GroundingSource,
            GuardrailTextQualifier.Query,
            GuardrailTextQualifier.GuardContent
        };

        // Act
        var json = JsonSerializer.Serialize(qualifiers, BedrockJsonContext.Default.ListGuardrailTextQualifier);

        // Assert
        await Assert.That(json).Contains("grounding_source");
        await Assert.That(json).Contains("query");
        await Assert.That(json).Contains("guard_content");
    }
}
