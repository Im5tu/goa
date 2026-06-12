using ErrorOr;
using Goa.Clients.S3.Operations.DeleteObject;
using Goa.Clients.S3.Operations.GetObject;
using Goa.Clients.S3.Operations.HeadObject;
using Goa.Clients.S3.Operations.PutObject;

namespace Goa.Clients.S3;

/// <summary>
/// High-performance S3 client interface optimized for AWS Lambda usage.
/// All operations use strongly-typed request objects and return ErrorOr results.
/// </summary>
public interface IS3Client
{
    /// <summary>
    /// Uploads an object to the specified bucket.
    /// </summary>
    /// <param name="request">The put object request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The put object response, or an error if the operation failed.</returns>
    Task<ErrorOr<PutObjectResponse>> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an object from the specified bucket.
    /// </summary>
    /// <remarks>
    /// The entire object body is buffered into memory before the response is returned. For large
    /// objects, set <see cref="GetObjectRequest.Range"/> to fetch the object in bounded slices and
    /// avoid excessive memory pressure.
    /// </remarks>
    /// <param name="request">The get object request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The get object response, or an error if the operation failed.</returns>
    Task<ErrorOr<GetObjectResponse>> GetObjectAsync(GetObjectRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the metadata of an object without returning the object body.
    /// </summary>
    /// <param name="request">The head object request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The head object response, or an error if the operation failed.</returns>
    Task<ErrorOr<HeadObjectResponse>> HeadObjectAsync(HeadObjectRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an object from the specified bucket. Deleting a key that does not exist succeeds.
    /// </summary>
    /// <param name="request">The delete object request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The delete object response, or an error if the operation failed.</returns>
    Task<ErrorOr<DeleteObjectResponse>> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken = default);
}
