using Goa.Clients.Dynamo.Extensions;
using Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Tests;

public class DynamoRecordExtensionsTests
{
    [Test]
    public async Task TryGetString_EmptyString_ShouldReturnTrueWithEmptyString()
    {
        var record = new DynamoRecord
        {
            ["Name"] = new AttributeValue { S = "" }
        };

        var result = record.TryGetString("Name", out var value);

        await Assert.That(result).IsTrue()
            .Because("TryGetString should succeed for empty string values");
        await Assert.That(value).IsEqualTo("")
            .Because("TryGetString should return the empty string, not reject it");
    }

    [Test]
    public async Task TryGetNullableString_EmptyString_ShouldReturnTrueWithEmptyString()
    {
        var record = new DynamoRecord
        {
            ["Name"] = new AttributeValue { S = "" }
        };

        var result = record.TryGetNullableString("Name", out var value);

        await Assert.That(result).IsTrue()
            .Because("TryGetNullableString should succeed for empty string values");
        await Assert.That(value).IsEqualTo("")
            .Because("TryGetNullableString should return the empty string, not reject it");
    }

    [Test]
    public async Task TryGetString_NullS_ShouldReturnFalse()
    {
        var record = new DynamoRecord
        {
            ["Name"] = new AttributeValue { NULL = true }
        };

        var result = record.TryGetString("Name", out _);

        await Assert.That(result).IsFalse()
            .Because("TryGetString should return false for NULL attribute values");
    }

    [Test]
    public async Task TryGetNullableString_NullS_ShouldReturnTrueWithNull()
    {
        var record = new DynamoRecord
        {
            ["Name"] = new AttributeValue { NULL = true }
        };

        var result = record.TryGetNullableString("Name", out var value);

        await Assert.That(result).IsTrue()
            .Because("TryGetNullableString should succeed for NULL attribute values");
        await Assert.That(value).IsNull()
            .Because("TryGetNullableString should return null for NULL attribute values");
    }
}
