namespace Goa.Functions.Core.Bootstrapping;

public sealed record InitializationErrorPayload(string Category, string Reason, string? Exception)
{
}
