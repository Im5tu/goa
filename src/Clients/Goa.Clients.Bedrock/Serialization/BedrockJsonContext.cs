using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;
using Goa.Clients.Bedrock.Operations.ApplyGuardrail;
using Goa.Clients.Bedrock.Operations.Converse;
using Goa.Clients.Bedrock.Operations.CountTokens;
using Goa.Clients.Bedrock.Operations.InvokeModel;
using Goa.Clients.Core.Http;

namespace Goa.Clients.Bedrock.Serialization;

/// <summary>
/// JSON source generator context for all Bedrock types to enable AOT compilation and improved performance.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ApiError))]
// Enums
[JsonSerializable(typeof(StopReason))]
[JsonSerializable(typeof(ConversationRole))]
[JsonSerializable(typeof(LatencyMode))]
[JsonSerializable(typeof(ServiceTier))]
// Models
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(ContentBlock))]
[JsonSerializable(typeof(ImageBlock))]
[JsonSerializable(typeof(ImageSource))]
[JsonSerializable(typeof(S3Location))]
[JsonSerializable(typeof(DocumentBlock))]
[JsonSerializable(typeof(DocumentSource))]
[JsonSerializable(typeof(ToolUseBlock))]
[JsonSerializable(typeof(ToolResultBlock))]
[JsonSerializable(typeof(Tool))]
[JsonSerializable(typeof(ToolSpec))]
[JsonSerializable(typeof(ToolInputSchema))]
[JsonSerializable(typeof(ToolConfiguration))]
[JsonSerializable(typeof(ToolChoice))]
[JsonSerializable(typeof(AutoToolChoice))]
[JsonSerializable(typeof(AnyToolChoice))]
[JsonSerializable(typeof(SpecificToolChoice))]
[JsonSerializable(typeof(InferenceConfiguration))]
[JsonSerializable(typeof(GuardrailConfiguration))]
[JsonSerializable(typeof(PerformanceConfiguration))]
[JsonSerializable(typeof(TokenUsage))]
[JsonSerializable(typeof(ConverseMetrics))]
// Operations - Converse
[JsonSerializable(typeof(ConverseRequest))]
[JsonSerializable(typeof(ConverseResponse))]
[JsonSerializable(typeof(ConverseOutput))]
[JsonSerializable(typeof(SystemContentBlock))]
[JsonSerializable(typeof(RequestMetadata))]
// Operations - InvokeModel
[JsonSerializable(typeof(InvokeModelRequest))]
[JsonSerializable(typeof(InvokeModelResponse))]
// Operations - ApplyGuardrail
[JsonSerializable(typeof(ApplyGuardrailRequest))]
[JsonSerializable(typeof(ApplyGuardrailResponse))]
[JsonSerializable(typeof(GuardrailContentBlock))]
[JsonSerializable(typeof(GuardrailTextBlock))]
[JsonSerializable(typeof(GuardrailTextQualifier))]
[JsonSerializable(typeof(GuardrailOutputContent))]
[JsonSerializable(typeof(GuardrailAssessment))]
[JsonSerializable(typeof(GuardrailTopicPolicyAssessment))]
[JsonSerializable(typeof(GuardrailTopic))]
[JsonSerializable(typeof(GuardrailContentPolicyAssessment))]
[JsonSerializable(typeof(GuardrailContentFilter))]
[JsonSerializable(typeof(GuardrailWordPolicyAssessment))]
[JsonSerializable(typeof(GuardrailCustomWord))]
[JsonSerializable(typeof(GuardrailManagedWord))]
[JsonSerializable(typeof(GuardrailSensitiveInformationPolicyAssessment))]
[JsonSerializable(typeof(GuardrailPiiEntity))]
[JsonSerializable(typeof(GuardrailRegexEntity))]
[JsonSerializable(typeof(GuardrailUsage))]
[JsonSerializable(typeof(List<GuardrailContentBlock>))]
[JsonSerializable(typeof(List<GuardrailOutputContent>))]
[JsonSerializable(typeof(List<GuardrailAssessment>))]
[JsonSerializable(typeof(List<GuardrailTopic>))]
[JsonSerializable(typeof(List<GuardrailContentFilter>))]
[JsonSerializable(typeof(List<GuardrailCustomWord>))]
[JsonSerializable(typeof(List<GuardrailManagedWord>))]
[JsonSerializable(typeof(List<GuardrailPiiEntity>))]
[JsonSerializable(typeof(List<GuardrailRegexEntity>))]
[JsonSerializable(typeof(List<GuardrailTextQualifier>))]
// Operations - CountTokens
[JsonSerializable(typeof(CountTokensRequest))]
[JsonSerializable(typeof(CountTokensResponse))]
// Collections
[JsonSerializable(typeof(List<Message>))]
[JsonSerializable(typeof(List<ContentBlock>))]
[JsonSerializable(typeof(List<Tool>))]
[JsonSerializable(typeof(List<SystemContentBlock>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(JsonElement))]
public partial class BedrockJsonContext : JsonSerializerContext
{
}
