namespace Goa.Clients.Dynamo.Operations.Batch;

/// <summary>
/// Individual write request within a BatchWriteItem operation.
/// </summary>
public class BatchWriteRequestItem
{
    /// <summary>
    /// A request to perform a PutItem operation.
    /// </summary>
    public PutRequest? PutRequest { get; set; }
    
    /// <summary>
    /// A request to perform a DeleteItem operation.
    /// </summary>
    public DeleteRequest? DeleteRequest { get; set; }
}