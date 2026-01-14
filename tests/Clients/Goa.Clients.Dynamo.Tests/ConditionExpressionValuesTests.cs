using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations;
using Goa.Clients.Dynamo.Operations.DeleteItem;
using Goa.Clients.Dynamo.Operations.PutItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.UpdateItem;

namespace Goa.Clients.Dynamo.Tests;

/// <summary>
/// Tests to ensure that ExpressionAttributeValues is properly handled for all Condition methods.
/// These tests prevent regression of the bug where empty ExpressionAttributeValues ({}) was sent to DynamoDB,
/// which causes a ValidationException.
/// </summary>
public class ConditionExpressionValuesTests
{
    #region PutItemBuilder Tests

    [Test]
    public async Task PutItemBuilder_WithCondition_AttributeExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new PutItemBuilder("TestTable")
            .WithItem(new Dictionary<string, AttributeValue> { ["pk"] = "value" })
            .WithCondition(Condition.AttributeExists("pk"));

        var request = builder.Build();

        await Assert.That(request.ConditionExpression).IsEqualTo("attribute_exists(#pk)");
        await Assert.That(request.ExpressionAttributeNames!["#pk"]).IsEqualTo("pk");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task PutItemBuilder_WithCondition_AttributeNotExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new PutItemBuilder("TestTable")
            .WithItem(new Dictionary<string, AttributeValue> { ["pk"] = "value" })
            .WithCondition(Condition.AttributeNotExists("pk"));

        var request = builder.Build();

        await Assert.That(request.ConditionExpression).IsEqualTo("attribute_not_exists(#pk)");
        await Assert.That(request.ExpressionAttributeNames!["#pk"]).IsEqualTo("pk");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task PutItemBuilder_WithCondition_Equals_SetsExpressionAttributeValues()
    {
        var builder = new PutItemBuilder("TestTable")
            .WithItem(new Dictionary<string, AttributeValue> { ["pk"] = "value" })
            .WithCondition(Condition.Equals("version", 1));

        var request = builder.Build();

        await Assert.That(request.ExpressionAttributeValues is not null).IsTrue();
        await Assert.That(request.ExpressionAttributeValues!.Count).IsEqualTo(1);
    }

    #endregion

    #region DeleteItemBuilder Tests

    [Test]
    public async Task DeleteItemBuilder_WithCondition_AttributeExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new DeleteItemBuilder("TestTable")
            .WithKey("pk", "value")
            .WithCondition(Condition.AttributeExists("lockToken"));

        var request = builder.Build();

        await Assert.That(request.ConditionExpression).IsEqualTo("attribute_exists(#lockToken)");
        await Assert.That(request.ExpressionAttributeNames!["#lockToken"]).IsEqualTo("lockToken");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task DeleteItemBuilder_WithCondition_AttributeNotExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new DeleteItemBuilder("TestTable")
            .WithKey("pk", "value")
            .WithCondition(Condition.AttributeNotExists("lockToken"));

        var request = builder.Build();

        await Assert.That(request.ConditionExpression).IsEqualTo("attribute_not_exists(#lockToken)");
        await Assert.That(request.ExpressionAttributeNames!["#lockToken"]).IsEqualTo("lockToken");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task DeleteItemBuilder_WithCondition_Equals_SetsExpressionAttributeValues()
    {
        var builder = new DeleteItemBuilder("TestTable")
            .WithKey("pk", "value")
            .WithCondition(Condition.Equals("version", 1));

        var request = builder.Build();

        await Assert.That(request.ExpressionAttributeValues is not null).IsTrue();
        await Assert.That(request.ExpressionAttributeValues!.Count).IsEqualTo(1);
    }

    #endregion

    #region UpdateItemBuilder Tests

    [Test]
    public async Task UpdateItemBuilder_WithCondition_AttributeExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new UpdateItemBuilder("TestTable")
            .WithKey("pk", "value")
            .WithCondition(Condition.AttributeExists("lockToken"));

        var request = builder.Build();

        await Assert.That(request.ConditionExpression).IsEqualTo("attribute_exists(#lockToken)");
        await Assert.That(request.ExpressionAttributeNames!["#lockToken"]).IsEqualTo("lockToken");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task UpdateItemBuilder_WithCondition_AttributeNotExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new UpdateItemBuilder("TestTable")
            .WithKey("pk", "value")
            .WithCondition(Condition.AttributeNotExists("lockToken"));

        var request = builder.Build();

        await Assert.That(request.ConditionExpression).IsEqualTo("attribute_not_exists(#lockToken)");
        await Assert.That(request.ExpressionAttributeNames!["#lockToken"]).IsEqualTo("lockToken");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task UpdateItemBuilder_WithCondition_Equals_SetsExpressionAttributeValues()
    {
        var builder = new UpdateItemBuilder("TestTable")
            .WithKey("pk", "value")
            .WithCondition(Condition.Equals("version", 1));

        var request = builder.Build();

        await Assert.That(request.ExpressionAttributeValues is not null).IsTrue();
        await Assert.That(request.ExpressionAttributeValues!.Count).IsEqualTo(1);
    }

    #endregion

    #region QueryBuilder WithKey Tests

    [Test]
    public async Task QueryBuilder_WithKey_AttributeExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.AttributeExists("pk"));

        var request = builder.Build();

        await Assert.That(request.KeyConditionExpression).IsEqualTo("attribute_exists(#pk)");
        await Assert.That(request.ExpressionAttributeNames!["#pk"]).IsEqualTo("pk");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task QueryBuilder_WithKey_AttributeNotExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.AttributeNotExists("pk"));

        var request = builder.Build();

        await Assert.That(request.KeyConditionExpression).IsEqualTo("attribute_not_exists(#pk)");
        await Assert.That(request.ExpressionAttributeNames!["#pk"]).IsEqualTo("pk");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task QueryBuilder_WithKey_Equals_SetsExpressionAttributeValues()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.Equals("pk", "value"));

        var request = builder.Build();

        await Assert.That(request.ExpressionAttributeValues is not null).IsTrue();
        await Assert.That(request.ExpressionAttributeValues!.Count).IsEqualTo(1);
    }

    #endregion

    #region QueryBuilder WithFilter Tests

    [Test]
    public async Task QueryBuilder_WithFilter_AttributeExists_DoesNotAddToExpressionAttributeValues()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.Equals("pk", "value"))
            .WithFilter(Condition.AttributeExists("data"));

        var request = builder.Build();

        await Assert.That(request.FilterExpression).IsEqualTo("attribute_exists(#data)");
        await Assert.That(request.ExpressionAttributeNames!["#data"]).IsEqualTo("data");
        // ExpressionAttributeValues should only have the key condition value, not filter
        await Assert.That(request.ExpressionAttributeValues!.Count).IsEqualTo(1);
    }

    [Test]
    public async Task QueryBuilder_WithFilter_AttributeNotExists_DoesNotAddToExpressionAttributeValues()
    {
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.Equals("pk", "value"))
            .WithFilter(Condition.AttributeNotExists("data"));

        var request = builder.Build();

        await Assert.That(request.FilterExpression).IsEqualTo("attribute_not_exists(#data)");
        await Assert.That(request.ExpressionAttributeNames!["#data"]).IsEqualTo("data");
        // ExpressionAttributeValues should only have the key condition value, not filter
        await Assert.That(request.ExpressionAttributeValues!.Count).IsEqualTo(1);
    }

    [Test]
    public async Task QueryBuilder_WithFilter_Only_AttributeExists_DoesNotSetExpressionAttributeValues()
    {
        // Use AttributeExists for both key and filter to test pure empty values scenario
        var builder = new QueryBuilder("TestTable")
            .WithKey(Condition.AttributeExists("pk"))
            .WithFilter(Condition.AttributeExists("data"));

        var request = builder.Build();

        await Assert.That(request.KeyConditionExpression).IsEqualTo("attribute_exists(#pk)");
        await Assert.That(request.FilterExpression).IsEqualTo("attribute_exists(#data)");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    #endregion

    #region ScanBuilder WithFilter Tests

    [Test]
    public async Task ScanBuilder_WithFilter_AttributeExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new ScanBuilder("TestTable")
            .WithFilter(Condition.AttributeExists("data"));

        var request = builder.Build();

        await Assert.That(request.FilterExpression).IsEqualTo("attribute_exists(#data)");
        await Assert.That(request.ExpressionAttributeNames!["#data"]).IsEqualTo("data");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task ScanBuilder_WithFilter_AttributeNotExists_DoesNotSetExpressionAttributeValues()
    {
        var builder = new ScanBuilder("TestTable")
            .WithFilter(Condition.AttributeNotExists("data"));

        var request = builder.Build();

        await Assert.That(request.FilterExpression).IsEqualTo("attribute_not_exists(#data)");
        await Assert.That(request.ExpressionAttributeNames!["#data"]).IsEqualTo("data");
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task ScanBuilder_WithFilter_Equals_SetsExpressionAttributeValues()
    {
        var builder = new ScanBuilder("TestTable")
            .WithFilter(Condition.Equals("status", "active"));

        var request = builder.Build();

        await Assert.That(request.ExpressionAttributeValues is not null).IsTrue();
        await Assert.That(request.ExpressionAttributeValues!.Count).IsEqualTo(1);
    }

    #endregion

    #region All Condition Methods Coverage Tests

    [Test]
    public async Task Condition_NotEquals_HasExpressionValues()
    {
        var condition = Condition.NotEquals("attr", "value");
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_GreaterThan_HasExpressionValues()
    {
        var condition = Condition.GreaterThan("attr", 10);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_GreaterThanOrEquals_HasExpressionValues()
    {
        var condition = Condition.GreaterThanOrEquals("attr", 10);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_LessThan_HasExpressionValues()
    {
        var condition = Condition.LessThan("attr", 10);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_LessThanOrEquals_HasExpressionValues()
    {
        var condition = Condition.LessThanOrEquals("attr", 10);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_Between_HasExpressionValues()
    {
        var condition = Condition.Between("attr", 1, 10);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Condition_BeginsWith_HasExpressionValues()
    {
        var condition = Condition.BeginsWith("attr", "prefix");
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_Contains_HasExpressionValues()
    {
        var condition = Condition.Contains("attr", "value");
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_NotContains_HasExpressionValues()
    {
        var condition = Condition.NotContains("attr", "value");
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_SizeEquals_HasExpressionValues()
    {
        var condition = Condition.SizeEquals("attr", 5);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_SizeNotEquals_HasExpressionValues()
    {
        var condition = Condition.SizeNotEquals("attr", 5);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_SizeGreaterThan_HasExpressionValues()
    {
        var condition = Condition.SizeGreaterThan("attr", 5);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_SizeGreaterThanOrEquals_HasExpressionValues()
    {
        var condition = Condition.SizeGreaterThanOrEquals("attr", 5);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_SizeLessThan_HasExpressionValues()
    {
        var condition = Condition.SizeLessThan("attr", 5);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_SizeLessThanOrEquals_HasExpressionValues()
    {
        var condition = Condition.SizeLessThanOrEquals("attr", 5);
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_AttributeType_HasExpressionValues()
    {
        var condition = Condition.AttributeType("attr", "S");
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_In_HasExpressionValues()
    {
        var condition = Condition.In("attr", "value1", "value2", "value3");
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Condition_AttributeExists_HasNoExpressionValues()
    {
        var condition = Condition.AttributeExists("attr");
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(0);
        await Assert.That(condition.ExpressionNames.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Condition_AttributeNotExists_HasNoExpressionValues()
    {
        var condition = Condition.AttributeNotExists("attr");
        await Assert.That(condition.ExpressionValues.Count).IsEqualTo(0);
        await Assert.That(condition.ExpressionNames.Count).IsEqualTo(1);
    }

    #endregion

    #region Composite Condition Tests

    [Test]
    public async Task Condition_And_TwoConditions_CombinesValues()
    {
        var left = Condition.Equals("a", "1");
        var right = Condition.Equals("b", "2");
        var combined = Condition.And(left, right);

        await Assert.That(combined.ExpressionValues.Count).IsEqualTo(2);
        await Assert.That(combined.ExpressionNames.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Condition_And_WithEmptyValueCondition_HasPartialValues()
    {
        var left = Condition.Equals("a", "1");
        var right = Condition.AttributeExists("b");
        var combined = Condition.And(left, right);

        await Assert.That(combined.ExpressionValues.Count).IsEqualTo(1);
        await Assert.That(combined.ExpressionNames.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Condition_And_TwoEmptyValueConditions_HasNoValues()
    {
        var left = Condition.AttributeExists("a");
        var right = Condition.AttributeNotExists("b");
        var combined = Condition.And(left, right);

        await Assert.That(combined.ExpressionValues.Count).IsEqualTo(0);
        await Assert.That(combined.ExpressionNames.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Condition_And_ParamsArray_CombinesAll()
    {
        var combined = Condition.And(
            Condition.Equals("a", "1"),
            Condition.Equals("b", "2"),
            Condition.AttributeExists("c")
        );

        await Assert.That(combined.ExpressionValues.Count).IsEqualTo(2);
        await Assert.That(combined.ExpressionNames.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Condition_Or_TwoConditions_CombinesValues()
    {
        var left = Condition.Equals("a", "1");
        var right = Condition.Equals("b", "2");
        var combined = Condition.Or(left, right);

        await Assert.That(combined.ExpressionValues.Count).IsEqualTo(2);
        await Assert.That(combined.ExpressionNames.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Condition_Or_WithEmptyValueCondition_HasPartialValues()
    {
        var left = Condition.Equals("a", "1");
        var right = Condition.AttributeExists("b");
        var combined = Condition.Or(left, right);

        await Assert.That(combined.ExpressionValues.Count).IsEqualTo(1);
        await Assert.That(combined.ExpressionNames.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Condition_Or_ParamsArray_CombinesAll()
    {
        var combined = Condition.Or(
            Condition.Equals("a", "1"),
            Condition.Equals("b", "2"),
            Condition.AttributeNotExists("c")
        );

        await Assert.That(combined.ExpressionValues.Count).IsEqualTo(2);
        await Assert.That(combined.ExpressionNames.Count).IsEqualTo(3);
    }

    #endregion

    #region Builder with Composite Conditions Tests

    [Test]
    public async Task PutItemBuilder_WithCondition_AndComposite_WithEmptyValues_DoesNotSetExpressionAttributeValues()
    {
        var builder = new PutItemBuilder("TestTable")
            .WithItem(new Dictionary<string, AttributeValue> { ["pk"] = "value" })
            .WithCondition(Condition.And(
                Condition.AttributeExists("a"),
                Condition.AttributeNotExists("b")
            ));

        var request = builder.Build();

        await Assert.That(request.ConditionExpression).IsEqualTo("attribute_exists(#a) AND attribute_not_exists(#b)");
        await Assert.That(request.ExpressionAttributeNames!.Count).IsEqualTo(2);
        await Assert.That(request.ExpressionAttributeValues is null).IsTrue();
    }

    [Test]
    public async Task PutItemBuilder_WithCondition_AndComposite_WithMixedValues_SetsOnlyNonEmptyValues()
    {
        var builder = new PutItemBuilder("TestTable")
            .WithItem(new Dictionary<string, AttributeValue> { ["pk"] = "value" })
            .WithCondition(Condition.And(
                Condition.AttributeNotExists("pk"),
                Condition.Equals("version", 1)
            ));

        var request = builder.Build();

        await Assert.That(request.ExpressionAttributeNames!.Count).IsEqualTo(2);
        await Assert.That(request.ExpressionAttributeValues is not null).IsTrue();
        await Assert.That(request.ExpressionAttributeValues!.Count).IsEqualTo(1);
    }

    #endregion
}
