namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// An image block in a message.
/// </summary>
public class ImageBlock
{
    /// <summary>
    /// The format of the image (e.g., "png", "jpeg", "gif", "webp").
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// The source of the image.
    /// </summary>
    public ImageSource Source { get; set; } = new();
}

/// <summary>
/// The source of an image.
/// </summary>
public class ImageSource
{
    /// <summary>
    /// Base64-encoded image bytes.
    /// </summary>
    public string? Bytes { get; set; }

    /// <summary>
    /// S3 location of the image.
    /// </summary>
    public S3Location? S3Location { get; set; }
}

/// <summary>
/// S3 location reference for content.
/// </summary>
public class S3Location
{
    /// <summary>
    /// The S3 URI of the content.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The S3 bucket name.
    /// </summary>
    public string? BucketOwner { get; set; }
}
