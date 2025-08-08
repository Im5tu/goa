namespace Goa.Functions.Dynamo;

/// <summary>
/// Determines what information is written to the stream for the table
/// </summary>
public enum StreamViewType
{
    /// <summary>
    /// Only the key attributes of the modified item
    /// </summary>
    KEYS_ONLY,

    /// <summary>
    /// The entire item, after it was modified
    /// </summary>
    NEW_IMAGE,

    /// <summary>
    /// The entire item, before it was modified
    /// </summary>
    OLD_IMAGE,

    /// <summary>
    /// Both the new and the old images of the item
    /// </summary>
    NEW_AND_OLD_IMAGES
}
