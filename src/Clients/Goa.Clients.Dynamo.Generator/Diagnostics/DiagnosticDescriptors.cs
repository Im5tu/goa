using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.Diagnostics;

/// <summary>
/// Central location for all diagnostic descriptors used by the DynamoDB generator.
/// </summary>
public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor TooManyGSIAttributes = new(
        id: "DYNAMO001",
        title: "Too many GlobalSecondaryIndex attributes",
        messageFormat: "Model '{0}' has {1} GlobalSecondaryIndex attributes - Maximum allowed is 5",
        category: "DynamoDB",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "DynamoDB supports a maximum of 5 Global Secondary Indexes per table.");

    public static readonly DiagnosticDescriptor InvalidPKPattern = new(
        id: "DYNAMO002",
        title: "Invalid PK pattern",
        messageFormat: "PK pattern cannot be null or empty on type '{0}'",
        category: "DynamoDB",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidSKPattern = new(
        id: "DYNAMO003",
        title: "Invalid SK pattern",
        messageFormat: "SK pattern cannot be null or empty on type '{0}'",
        category: "DynamoDB",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingTypeDiscriminator = new(
        id: "DYNAMO004",
        title: "Missing type discriminator",
        messageFormat: "Abstract type '{0}' requires a Type discriminator field for inheritance.",
        category: "DynamoDB",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PropertyNotFound = new(
        id: "DYNAMO005",
        title: "Property not found in placeholder",
        messageFormat: "Property '{0}' referenced in key pattern '{1}' does not exist on type '{2}' or any of its base classes.",
        category: "DynamoDB",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All placeholders in PK/SK patterns must reference existing properties on the model or its base classes.");

    public static readonly DiagnosticDescriptor InvalidUnixTimestampUsage = new(
        id: "DYNAMO006",
        title: "Invalid UnixTimestamp usage",
        messageFormat: "UnixTimestamp attribute can only be applied to DateTime or DateTimeOffset properties. Property '{0}' has type '{1}'",
        category: "DynamoDB",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidGSIIndexName = new(
        id: "DYNAMO007",
        title: "Invalid GSI IndexName",
        messageFormat: "GSI IndexName cannot be null or empty on type '{0}'",
        category: "DynamoDB",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidGSIPKPattern = new(
        id: "DYNAMO008",
        title: "Invalid GSI PK pattern",
        messageFormat: "GSI PK pattern cannot be null or empty on type '{0}'",
        category: "DynamoDB",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidGSISKPattern = new(
        id: "DYNAMO009",
        title: "Invalid GSI SK pattern",
        messageFormat: "GSI SK pattern cannot be null or empty on type '{0}'",
        category: "DynamoDB",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor CodeGenerationError = new(
        id: "DYNAMO_GEN_001",
        title: "Code generation error",
        messageFormat: "An error occurred during code generation: {0}",
        category: "DynamoDB.Generator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
