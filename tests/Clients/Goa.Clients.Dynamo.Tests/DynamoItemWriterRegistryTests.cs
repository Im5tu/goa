using System.Text.Json;

namespace Goa.Clients.Dynamo.Tests;

public class DynamoItemWriterRegistryTests
{
    private sealed class TestItem
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    private sealed class UnregisteredItem
    {
        public string? Id { get; set; }
    }

    private static void WriteTestItem(Utf8JsonWriter writer, TestItem item)
    {
        writer.WriteStartObject();
        if (item.Name is not null)
        {
            writer.WritePropertyName("name");
            writer.WriteStartObject();
            writer.WriteString("S", item.Name);
            writer.WriteEndObject();
        }
        writer.WritePropertyName("value");
        writer.WriteStartObject();
        writer.WriteNumber("N", item.Value);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    [Test]
    public async Task Register_and_Get_roundtrip_returns_registered_writer()
    {
        DynamoItemWriterRegistry.Register<TestItem>(WriteTestItem);

        var writer = DynamoItemWriterRegistry.Get<TestItem>();

        await Assert.That(writer).IsEqualTo((DynamoItemWriter<TestItem>)WriteTestItem);
    }

    [Test]
    public void Get_without_registration_throws_InvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => DynamoItemWriterRegistry.Get<UnregisteredItem>());
    }

    [Test]
    public async Task Re_registering_overwrites_previous_writer()
    {
        static void originalWriter(Utf8JsonWriter writer, TestItem item)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        DynamoItemWriterRegistry.Register<TestItem>(originalWriter);
        DynamoItemWriterRegistry.Register<TestItem>(WriteTestItem);

        var result = DynamoItemWriterRegistry.Get<TestItem>();

        await Assert.That(result).IsEqualTo((DynamoItemWriter<TestItem>)WriteTestItem);
    }
}
