using System.Text.Json;
using Goa.Clients.Dynamo.Internal;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.Transactions;

namespace Goa.Clients.Dynamo.Tests;

public class DynamoResponseReaderRegistryTests
{
    [Test]
    public async Task GetReader_QueryResponse_ShouldDeserializeItems()
    {
        var json = """{"Items":[{"pk":{"S":"pk1"},"sk":{"S":"sk1"}},{"pk":{"S":"pk2"},"sk":{"S":"sk2"}}],"Count":2,"ScannedCount":5}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<QueryResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.Items.Count).IsEqualTo(2);
        await Assert.That(result.Items[0]["pk"]?.S).IsEqualTo("pk1");
        await Assert.That(result.Items[1]["pk"]?.S).IsEqualTo("pk2");
        await Assert.That(result.ScannedCount).IsEqualTo(5);
    }

    [Test]
    public async Task GetReader_QueryResponse_ShouldHandleEmptyItems()
    {
        var json = """{"Items":[],"Count":0,"ScannedCount":0}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<QueryResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.Items.Count).IsEqualTo(0);
        await Assert.That(result.ScannedCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetReader_QueryResponse_ShouldParsePagination()
    {
        var json = """{"Items":[{"pk":{"S":"pk1"}}],"Count":1,"ScannedCount":1,"LastEvaluatedKey":{"pk":{"S":"pk1"}}}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<QueryResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.HasMoreResults).IsTrue();
        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["pk"].S).IsEqualTo("pk1");
    }

    [Test]
    public async Task GetReader_QueryResponse_ShouldParseConsumedCapacity()
    {
        var json = """{"Items":[],"Count":0,"ScannedCount":0,"ConsumedCapacity":{"TableName":"TestTable","CapacityUnits":5.0}}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<QueryResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.ConsumedCapacity).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.TableName).IsEqualTo("TestTable");
        await Assert.That(result.ConsumedCapacity!.CapacityUnits).IsEqualTo(5.0);
    }

    [Test]
    public async Task GetReader_QueryResponse_ShouldHandleFullResponse()
    {
        var json = """{"Items":[{"pk":{"S":"pk1"},"data":{"S":"d1"}}],"Count":1,"ScannedCount":10,"LastEvaluatedKey":{"pk":{"S":"pk1"}},"ConsumedCapacity":{"TableName":"Full","CapacityUnits":7.5}}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<QueryResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(result.ScannedCount).IsEqualTo(10);
        await Assert.That(result.HasMoreResults).IsTrue();
        await Assert.That(result.ConsumedCapacity!.TableName).IsEqualTo("Full");
    }

    [Test]
    public async Task GetReader_QueryResponse_ShouldDeserializeComplexAttributeTypes()
    {
        var json = """{"Items":[{"str":{"S":"hello"},"num":{"N":"42"},"flag":{"BOOL":true},"empty":{"NULL":true},"tags":{"SS":["a","b"]},"scores":{"NS":["1","2"]},"list":{"L":[{"S":"x"}]},"nested":{"M":{"k":{"S":"v"}}}}],"Count":1,"ScannedCount":1}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<QueryResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        var item = result.Items[0];
        await Assert.That(item["str"]?.S).IsEqualTo("hello");
        await Assert.That(item["num"]?.N).IsEqualTo("42");
        await Assert.That(item["flag"]?.BOOL).IsEqualTo(true);
        await Assert.That(item["empty"]?.NULL).IsEqualTo(true);
        await Assert.That(item["tags"]?.SS).IsNotNull();
        await Assert.That(item["tags"]?.SS!.Count).IsEqualTo(2);
        await Assert.That(item["scores"]?.NS).IsNotNull();
        await Assert.That(item["scores"]?.NS!.Count).IsEqualTo(2);
        await Assert.That(item["list"]?.L).IsNotNull();
        await Assert.That(item["list"]?.L!.Count).IsEqualTo(1);
        await Assert.That(item["list"]?.L![0].S).IsEqualTo("x");
        await Assert.That(item["nested"]?.M).IsNotNull();
        await Assert.That(item["nested"]?.M!["k"].S).IsEqualTo("v");
    }

    [Test]
    public async Task GetReader_ScanResponse_ShouldDeserializeItems()
    {
        var json = """{"Items":[{"pk":{"S":"pk1"},"sk":{"S":"sk1"}}],"Count":1,"ScannedCount":3}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<ScanResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(result.Items[0]["pk"]?.S).IsEqualTo("pk1");
        await Assert.That(result.ScannedCount).IsEqualTo(3);
    }

    [Test]
    public async Task GetReader_GetItemResponse_ShouldDeserializeItem()
    {
        var json = """{"Item":{"pk":{"S":"pk1"},"sk":{"S":"sk1"},"data":{"S":"hello"}}}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<GetItemResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.Item).IsNotNull();
        await Assert.That(result.Item!["pk"]?.S).IsEqualTo("pk1");
        await Assert.That(result.Item!["data"]?.S).IsEqualTo("hello");
    }

    [Test]
    public async Task GetReader_GetItemResponse_ShouldReturnNullItem_WhenEmpty()
    {
        var json = """{}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<GetItemResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.Item).IsNull();
    }

    [Test]
    public async Task GetReader_BatchGetItemResponse_ShouldDeserializeMultiTableResponses()
    {
        var json = """{"Responses":{"Table1":[{"pk":{"S":"pk1"}}],"Table2":[{"pk":{"S":"pk2"}},{"pk":{"S":"pk3"}}]}}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<BatchGetItemResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.Responses.Count).IsEqualTo(2);
        await Assert.That(result.Responses["Table1"].Count).IsEqualTo(1);
        await Assert.That(result.Responses["Table2"].Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetReader_TransactGetItemResponse_ShouldDeserializeItems()
    {
        var json = """{"Responses":[{"Item":{"pk":{"S":"pk1"}}},{"Item":{"pk":{"S":"pk2"}}}]}"""u8;

        var reader = DynamoResponseReaderRegistry.GetReader<TransactGetItemResponse>();
        var jsonReader = new Utf8JsonReader(json);
        var result = reader(ref jsonReader);

        await Assert.That(result.Responses.Count).IsEqualTo(2);
        await Assert.That(result.Responses[0].Item).IsNotNull();
        await Assert.That(result.Responses[0].Item!["pk"]?.S).IsEqualTo("pk1");
    }
}
