using Goa.Clients.Dynamo.Exceptions;
using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using ErrorOr;
using Moq;

namespace Goa.Clients.Dynamo.Tests;

public class TypedExtensionTests
{
    // Tests for non-generic pagination
    [Test]
    public async Task QueryAllAsync_DynamoRecord_PaginatesAutomatically()
    {
        var mock = new Mock<IDynamoClient>();
        var lastKey = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = "lastPk" }
        };

        ErrorOr<QueryResponse> firstPage = new QueryResponse
        {
            Items = [new DynamoRecord { ["pk"] = new AttributeValue { S = "pk1" } }],
            LastEvaluatedKey = lastKey
        };
        ErrorOr<QueryResponse> secondPage = new QueryResponse
        {
            Items = [new DynamoRecord { ["pk"] = new AttributeValue { S = "pk2" } }],
            LastEvaluatedKey = null
        };

        mock.Setup(c => c.QueryAsync(It.Is<QueryRequest>(r => r.ExclusiveStartKey == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstPage);
        mock.Setup(c => c.QueryAsync(It.Is<QueryRequest>(r => r.ExclusiveStartKey != null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondPage);

        var items = await mock.Object.QueryAllAsync("TestTable", _ => { }).ToListAsync();

        await Assert.That(items).Count().IsEqualTo(2);
        await Assert.That(items[0]["pk"]?.S).IsEqualTo("pk1");
        await Assert.That(items[1]["pk"]?.S).IsEqualTo("pk2");
    }

    [Test]
    public async Task ScanAllAsync_DynamoRecord_PaginatesAutomatically()
    {
        var mock = new Mock<IDynamoClient>();
        var lastKey = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = "lastPk" }
        };

        ErrorOr<ScanResponse> firstPage = new ScanResponse
        {
            Items = [new DynamoRecord { ["pk"] = new AttributeValue { S = "pk1" } }],
            LastEvaluatedKey = lastKey
        };
        ErrorOr<ScanResponse> secondPage = new ScanResponse
        {
            Items = [new DynamoRecord { ["pk"] = new AttributeValue { S = "pk2" } }],
            LastEvaluatedKey = null
        };

        mock.Setup(c => c.ScanAsync(It.Is<ScanRequest>(r => r.ExclusiveStartKey == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstPage);
        mock.Setup(c => c.ScanAsync(It.Is<ScanRequest>(r => r.ExclusiveStartKey != null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondPage);

        var items = await mock.Object.ScanAllAsync("TestTable", _ => { }).ToListAsync();

        await Assert.That(items).Count().IsEqualTo(2);
        await Assert.That(items[0]["pk"]?.S).IsEqualTo("pk1");
        await Assert.That(items[1]["pk"]?.S).IsEqualTo("pk2");
    }
}
