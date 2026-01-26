namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// A document block in a message.
/// </summary>
public class DocumentBlock
{
    /// <summary>
    /// The format of the document (e.g., "pdf", "csv", "doc", "docx", "xls", "xlsx", "html", "txt", "md").
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// The name of the document.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The source of the document.
    /// </summary>
    public DocumentSource Source { get; set; } = new();
}

/// <summary>
/// The source of a document.
/// </summary>
public class DocumentSource
{
    /// <summary>
    /// Base64-encoded document bytes.
    /// </summary>
    public string? Bytes { get; set; }

    /// <summary>
    /// S3 location of the document.
    /// </summary>
    public S3Location? S3Location { get; set; }
}
