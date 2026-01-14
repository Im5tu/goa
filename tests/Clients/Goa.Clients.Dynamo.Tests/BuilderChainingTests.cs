using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.DeleteItem;
using Goa.Clients.Dynamo.Operations.PutItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.UpdateItem;

namespace Goa.Clients.Dynamo.Tests;

public class BuilderChainingTests
{
    [Test]
    public async Task QueryBuilder_WithKey_SingleCondition_SetsExpression()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.Equals("pk", "value1"));

        var request = builder.Build();

        await Assert.That(request.KeyConditionExpression).IsEqualTo("#pk = :pk");
        await Assert.That(request.ExpressionAttributeNames!["#pk"]).IsEqualTo("pk");
        await Assert.That(request.ExpressionAttributeValues![":pk"].S).IsEqualTo("value1");
    }

    [Test]
    public async Task QueryBuilder_WithKey_MultipleConditions_CombinesWithAnd()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.Equals("pk", "value1"))
            .WithKey(Condition.Equals("sk", "value2"));

        var request = builder.Build();

        await Assert.That(request.KeyConditionExpression).IsEqualTo("#pk = :pk AND #sk = :sk");
        await Assert.That(request.ExpressionAttributeNames!["#pk"]).IsEqualTo("pk");
        await Assert.That(request.ExpressionAttributeNames!["#sk"]).IsEqualTo("sk");
        await Assert.That(request.ExpressionAttributeValues![":pk"].S).IsEqualTo("value1");
        await Assert.That(request.ExpressionAttributeValues![":sk"].S).IsEqualTo("value2");
    }

    [Test]
    public async Task QueryBuilder_WithKey_ThreeConditions_CombinesAllWithAnd()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.Equals("pk", "value1"))
            .WithKey(Condition.BeginsWith("sk", "prefix"))
            .WithKey(Condition.AttributeExists("data"));

        var request = builder.Build();

        await Assert.That(request.KeyConditionExpression)
            .IsEqualTo("#pk = :pk AND begins_with(#sk, :sk) AND attribute_exists(#data)");
    }

    [Test]
    public async Task ScanBuilder_WithFilter_SingleCondition_SetsExpression()
    {
        var builder = new ScanBuilder("TestTable")
            .WithFilter(Condition.Equals("status", "active"));

        var request = builder.Build();

        await Assert.That(request.FilterExpression).IsEqualTo("#status = :status");
        await Assert.That(request.ExpressionAttributeNames!["#status"]).IsEqualTo("status");
        await Assert.That(request.ExpressionAttributeValues![":status"].S).IsEqualTo("active");
    }

    [Test]
    public async Task ScanBuilder_WithFilter_MultipleConditions_CombinesWithAnd()
    {
        var builder = new ScanBuilder("TestTable")
            .WithFilter(Condition.Equals("status", "active"))
            .WithFilter(Condition.GreaterThan("count", 10));

        var request = builder.Build();

        await Assert.That(request.FilterExpression).IsEqualTo("#status = :status AND #count > :count");
        await Assert.That(request.ExpressionAttributeNames!["#status"]).IsEqualTo("status");
        await Assert.That(request.ExpressionAttributeNames!["#count"]).IsEqualTo("count");
        await Assert.That(request.ExpressionAttributeValues![":status"].S).IsEqualTo("active");
        await Assert.That(request.ExpressionAttributeValues![":count"].N).IsEqualTo("10");
    }

    [Test]
    public async Task DeleteItemBuilder_WithCondition_SingleCondition_SetsExpression()
    {
        var builder = new DeleteItemBuilder("TestTable")
            .WithKey("pk", "value1")
            .WithCondition(Condition.AttributeExists("lockToken"));

        var request = builder.Build();

        await Assert.That(request.ConditionExpression).IsEqualTo("attribute_exists(#lockToken)");
        await Assert.That(request.ExpressionAttributeNames!["#lockToken"]).IsEqualTo("lockToken");
    }

    [Test]
    public async Task DeleteItemBuilder_WithCondition_MultipleConditions_CombinesWithAnd()
    {
        var builder = new DeleteItemBuilder("TestTable")
            .WithKey("pk", "value1")
            .WithCondition(Condition.AttributeExists("lockToken"))
            .WithCondition(Condition.Equals("version", 1));

        var request = builder.Build();

        await Assert.That(request.ConditionExpression)
            .IsEqualTo("(attribute_exists(#lockToken)) AND (#version = :version)");
        await Assert.That(request.ExpressionAttributeNames!["#lockToken"]).IsEqualTo("lockToken");
        await Assert.That(request.ExpressionAttributeNames!["#version"]).IsEqualTo("version");
        await Assert.That(request.ExpressionAttributeValues![":version"].N).IsEqualTo("1");
    }

    [Test]
    public async Task QueryBuilder_WithExclusiveStartKey_SetsKey()
    {
        var startKey = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = "lastPk" },
            ["sk"] = new AttributeValue { S = "lastSk" }
        };

        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.Equals("pk", "value1"))
            .WithExclusiveStartKey(startKey);

        var request = builder.Build();

        await Assert.That(request.ExclusiveStartKey!["pk"].S).IsEqualTo("lastPk");
        await Assert.That(request.ExclusiveStartKey["sk"].S).IsEqualTo("lastSk");
    }

    [Test]
    public async Task QueryBuilder_WithFilter_SetsFilterExpression()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.Equals("pk", "value1"))
            .WithFilter(Condition.Equals("status", "active"));

        var request = builder.Build();

        await Assert.That(request.FilterExpression).IsEqualTo("#status = :status");
        await Assert.That(request.ExpressionAttributeNames!["#status"]).IsEqualTo("status");
        await Assert.That(request.ExpressionAttributeValues![":status"].S).IsEqualTo("active");
    }

    [Test]
    public async Task QueryBuilder_WithFilter_MultipleConditions_CombinesWithAnd()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.Equals("pk", "value1"))
            .WithFilter(Condition.Equals("status", "active"))
            .WithFilter(Condition.GreaterThan("count", 5));

        var request = builder.Build();

        await Assert.That(request.FilterExpression).IsEqualTo("#status = :status AND #count > :count");
    }

    [Test]
    public async Task ScanBuilder_WithExclusiveStartKey_SetsKey()
    {
        var startKey = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new AttributeValue { S = "lastPk" }
        };

        var builder = new ScanBuilder("TestTable")
            .WithExclusiveStartKey(startKey);

        var request = builder.Build();

        await Assert.That(request.ExclusiveStartKey!["pk"].S).IsEqualTo("lastPk");
    }

    [Test]
    public async Task BatchGetTableBuilder_WithProjection_SetsProjectionExpression()
    {
        var builder = new BatchGetItemBuilder()
            .WithTable("TestTable", table => table
                .WithKey("pk", "value1")
                .WithProjection("attr1", "attr2", "attr3")
                .WithConsistentRead());

        var request = builder.Build();

        await Assert.That(request.RequestItems["TestTable"].ProjectionExpression).IsEqualTo("attr1, attr2, attr3");
        await Assert.That(request.RequestItems["TestTable"].ConsistentRead).IsTrue();
    }

    [Test]
    public async Task BatchWriteItemBuilder_WithReturnConsumedCapacity_SetsCapacity()
    {
        var builder = new BatchWriteItemBuilder()
            .WithTable("TestTable", table => table
                .WithPut(new Dictionary<string, AttributeValue> { ["pk"] = new AttributeValue { S = "value1" } }))
            .WithReturnConsumedCapacity(ReturnConsumedCapacity.TOTAL)
            .WithReturnItemCollectionMetrics(ReturnItemCollectionMetrics.SIZE);

        var request = builder.Build();

        await Assert.That(request.ReturnConsumedCapacity).IsEqualTo(ReturnConsumedCapacity.TOTAL);
        await Assert.That(request.ReturnItemCollectionMetrics).IsEqualTo(ReturnItemCollectionMetrics.SIZE);
    }

    [Test]
    public async Task DeleteItemBuilder_WithReturnConsumedCapacity_SetsCapacity()
    {
        var builder = new DeleteItemBuilder("TestTable")
            .WithKey("pk", new AttributeValue { S = "value1" })
            .WithReturnConsumedCapacity(ReturnConsumedCapacity.INDEXES)
            .WithReturnItemCollectionMetrics(ReturnItemCollectionMetrics.SIZE);

        var request = builder.Build();

        await Assert.That(request.ReturnConsumedCapacity).IsEqualTo(ReturnConsumedCapacity.INDEXES);
        await Assert.That(request.ReturnItemCollectionMetrics).IsEqualTo(ReturnItemCollectionMetrics.SIZE);
    }

    [Test]
    public async Task DeleteItemBuilder_WithReturnValuesOnConditionCheckFailure_AllOld_SetsValue()
    {
        var builder = new DeleteItemBuilder("TestTable")
            .WithKey("pk", new AttributeValue { S = "value1" })
            .WithReturnValuesOnConditionCheckFailure(ReturnValuesOnConditionCheckFailure.ALL_OLD);

        var request = builder.Build();

        await Assert.That(request.ReturnValuesOnConditionCheckFailure).IsEqualTo(ReturnValuesOnConditionCheckFailure.ALL_OLD);
    }

    [Test]
    public async Task DeleteItemBuilder_WithReturnValuesOnConditionCheckFailure_None_SetsValue()
    {
        var builder = new DeleteItemBuilder("TestTable")
            .WithKey("pk", new AttributeValue { S = "value1" })
            .WithReturnValuesOnConditionCheckFailure(ReturnValuesOnConditionCheckFailure.NONE);

        var request = builder.Build();

        await Assert.That(request.ReturnValuesOnConditionCheckFailure).IsEqualTo(ReturnValuesOnConditionCheckFailure.NONE);
    }

    [Test]
    public async Task PutItemBuilder_WithReturnValuesOnConditionCheckFailure_AllOld_SetsValue()
    {
        var builder = new PutItemBuilder("TestTable")
            .WithItem(new Dictionary<string, AttributeValue> { ["pk"] = "value1" })
            .WithReturnValuesOnConditionCheckFailure(ReturnValuesOnConditionCheckFailure.ALL_OLD);

        var request = builder.Build();

        await Assert.That(request.ReturnValuesOnConditionCheckFailure).IsEqualTo(ReturnValuesOnConditionCheckFailure.ALL_OLD);
    }

    [Test]
    public async Task PutItemBuilder_WithReturnValuesOnConditionCheckFailure_None_SetsValue()
    {
        var builder = new PutItemBuilder("TestTable")
            .WithItem(new Dictionary<string, AttributeValue> { ["pk"] = "value1" })
            .WithReturnValuesOnConditionCheckFailure(ReturnValuesOnConditionCheckFailure.NONE);

        var request = builder.Build();

        await Assert.That(request.ReturnValuesOnConditionCheckFailure).IsEqualTo(ReturnValuesOnConditionCheckFailure.NONE);
    }

    [Test]
    public async Task UpdateItemBuilder_WithReturnValuesOnConditionCheckFailure_AllOld_SetsValue()
    {
        var builder = new UpdateItemBuilder("TestTable")
            .WithKey("pk", new AttributeValue { S = "value1" })
            .Set("status", "active")
            .WithReturnValuesOnConditionCheckFailure(ReturnValuesOnConditionCheckFailure.ALL_OLD);

        var request = builder.Build();

        await Assert.That(request.ReturnValuesOnConditionCheckFailure).IsEqualTo(ReturnValuesOnConditionCheckFailure.ALL_OLD);
    }

    [Test]
    public async Task UpdateItemBuilder_WithReturnValuesOnConditionCheckFailure_None_SetsValue()
    {
        var builder = new UpdateItemBuilder("TestTable")
            .WithKey("pk", new AttributeValue { S = "value1" })
            .Set("status", "active")
            .WithReturnValuesOnConditionCheckFailure(ReturnValuesOnConditionCheckFailure.NONE);

        var request = builder.Build();

        await Assert.That(request.ReturnValuesOnConditionCheckFailure).IsEqualTo(ReturnValuesOnConditionCheckFailure.NONE);
    }
}
