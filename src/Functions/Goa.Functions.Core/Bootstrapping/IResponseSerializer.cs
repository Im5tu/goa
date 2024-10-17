namespace Goa.Functions.Core.Bootstrapping;

/// <summary>
///     Interface for serializing a response object into an HTTP content format for sending to the AWS Lambda Runtime API.
/// </summary>
/// <typeparam name="T">The type of the response to be serialized.</typeparam>
public interface IResponseSerializer<in T>
{
    /// <summary>
    ///     Serializes the response into a HTTPContent instance
    /// </summary>
    /// <param name="response">The response object to be serialized.</param>
    HttpContent Serialize(T response);
}
