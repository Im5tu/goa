namespace Goa.Functions.Core.Bootstrapping;

/// <summary>
///     Represents the payload for reporting initialization errors to the AWS Lambda Runtime API.
/// </summary>
/// <param name="Category">The category of the error (e.g., "StartupError").</param>
/// <param name="Reason">The specific reason for the error (e.g., "Invalid configuration").</param>
/// <param name="Exception">Optional exception details, if applicable.</param>
public sealed record InitializationErrorPayload(string Category, string Reason, string? Exception);
