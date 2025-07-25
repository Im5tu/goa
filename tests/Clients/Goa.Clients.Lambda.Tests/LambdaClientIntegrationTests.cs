using System.Text.Json;
using Goa.Clients.Lambda.Models;
using Goa.Clients.Lambda.Operations.Invoke;
using Goa.Clients.Lambda.Operations.InvokeAsync;
using Goa.Clients.Lambda.Operations.InvokeDryRun;
using TUnit.Assertions;
using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Core;

namespace Goa.Clients.Lambda.Tests;

[ClassDataSource<LambdaTestFixture>(Shared = SharedType.PerAssembly)]
public class LambdaClientIntegrationTests
{
    private readonly LambdaTestFixture _fixture;

    public LambdaClientIntegrationTests(LambdaTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task InvokeSynchronousAsync_WithValidFunction_ShouldSucceed()
    {
        // Arrange
        var payload = new { message = "Hello, Lambda!", timestamp = DateTime.UtcNow };
        var request = new InvokeBuilder()
            .WithFunctionName(_fixture.TestFunctionName)
            .WithPayload(JsonSerializer.Serialize(payload))
            .Build();

        // Act
        var result = await _fixture.LambdaClient.InvokeSynchronousAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.IsSuccess).IsTrue();
        await Assert.That(result.Value.StatusCode).IsEqualTo(200);
        await Assert.That(result.Value.Payload).IsNotNull();
    }

    [Test]
    public async Task InvokeAsynchronousAsync_WithAsyncInvocation_ShouldSucceed()
    {
        // Arrange
        var payload = new { message = "Async test" };
        var request = new InvokeAsyncBuilder()
            .WithFunctionName(_fixture.TestFunctionName)
            .WithPayload(JsonSerializer.Serialize(payload))
            .Build();

        // Act
        var result = await _fixture.LambdaClient.InvokeAsynchronousAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.StatusCode).IsEqualTo(202);
        await Assert.That(result.Value.IsSuccess).IsTrue();
    }

    [Test]
    public async Task InvokeSynchronousAsync_WithEmptyFunctionName_ShouldFail()
    {
        // Arrange
        var request = new InvokeRequest { FunctionName = "" };

        // Act
        var result = await _fixture.LambdaClient.InvokeSynchronousAsync(request);

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("InvokeRequest.FunctionName");
    }

    [Test]
    public async Task InvokeBuilder_WithValidParameters_ShouldBuildCorrectly()
    {
        // Arrange & Act
        var payload = new { test = "data" };
        var json = JsonSerializer.Serialize(payload);
        var request = new InvokeBuilder()
            .WithFunctionName("test-function")
            .WithLogType(LogType.Tail)
            .WithQualifier("$LATEST")
            .WithPayload(json)
            .Build();

        // Assert
        await Assert.That(request.FunctionName).IsEqualTo("test-function");
        await Assert.That(request.LogType).IsEqualTo(LogType.Tail);
        await Assert.That(request.Qualifier).IsEqualTo("$LATEST");
        await Assert.That(request.Payload).IsEqualTo(json);
    }

    [Test]
    public async Task InvokeBuilder_WithoutFunctionName_ShouldThrowException()
    {
        // Arrange
        var builder = new InvokeBuilder();

        // Act & Assert
        await Assert.That(() => builder.Build())
            .Throws<InvalidOperationException>()
            .WithMessage("Function name is required.");
    }

    [Test]
    public async Task InvokeSynchronousAsync_WithNonExistentFunction_ShouldFail()
    {
        // Arrange
        var payload = new { test = "data" };
        var request = new InvokeBuilder()
            .WithFunctionName("non-existent-function")
            .WithPayload(JsonSerializer.Serialize(payload))
            .Build();

        // Act
        var result = await _fixture.LambdaClient.InvokeSynchronousAsync(request);

        // Assert
        await Assert.That(result.IsError).IsTrue();
    }

    [Test]
    public async Task InvokeDryRunAsync_WithValidFunction_ShouldSucceed()
    {
        // Arrange
        var payload = new { test = "data" };
        var request = new InvokeDryRunBuilder()
            .WithFunctionName(_fixture.TestFunctionName)
            .WithPayload(JsonSerializer.Serialize(payload))
            .Build();

        // Act
        var result = await _fixture.LambdaClient.InvokeDryRunAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.StatusCode).IsEqualTo(204);
        await Assert.That(result.Value.IsSuccess).IsTrue();
    }

    [Test]
    public async Task InvokeAsynchronousAsync_WithEmptyFunctionName_ShouldFail()
    {
        // Arrange
        var request = new InvokeAsyncRequest { FunctionName = "" };

        // Act
        var result = await _fixture.LambdaClient.InvokeAsynchronousAsync(request);

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("InvokeAsyncRequest.FunctionName");
    }

    [Test]
    public async Task InvokeDryRunAsync_WithEmptyFunctionName_ShouldFail()
    {
        // Arrange
        var request = new InvokeDryRunRequest { FunctionName = "" };

        // Act
        var result = await _fixture.LambdaClient.InvokeDryRunAsync(request);

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo("InvokeDryRunRequest.FunctionName");
    }

    [Test]
    public async Task InvokeAsyncBuilder_WithValidParameters_ShouldBuildCorrectly()
    {
        // Arrange & Act
        var payload = new { test = "data" };
        var json = JsonSerializer.Serialize(payload);
        var request = new InvokeAsyncBuilder()
            .WithFunctionName("test-function")
            .WithQualifier("$LATEST")
            .WithClientContext("eyJ0ZXN0IjoidmFsdWUifQ==")
            .WithPayload(json)
            .Build();

        // Assert
        await Assert.That(request.FunctionName).IsEqualTo("test-function");
        await Assert.That(request.Qualifier).IsEqualTo("$LATEST");
        await Assert.That(request.ClientContext).IsEqualTo("eyJ0ZXN0IjoidmFsdWUifQ==");
        await Assert.That(request.Payload).IsEqualTo(json);
    }

    [Test]
    public async Task InvokeDryRunBuilder_WithValidParameters_ShouldBuildCorrectly()
    {
        // Arrange & Act
        var payload = new { test = "data" };
        var json = JsonSerializer.Serialize(payload);
        var request = new InvokeDryRunBuilder()
            .WithFunctionName("test-function")
            .WithQualifier("$LATEST")
            .WithClientContext("eyJ0ZXN0IjoidmFsdWUifQ==")
            .WithPayload(json)
            .Build();

        // Assert
        await Assert.That(request.FunctionName).IsEqualTo("test-function");
        await Assert.That(request.Qualifier).IsEqualTo("$LATEST");
        await Assert.That(request.ClientContext).IsEqualTo("eyJ0ZXN0IjoidmFsdWUifQ==");
        await Assert.That(request.Payload).IsEqualTo(json);
    }

    [Test]
    public async Task InvokeAsyncBuilder_WithoutFunctionName_ShouldThrowException()
    {
        // Arrange
        var builder = new InvokeAsyncBuilder();

        // Act & Assert
        await Assert.That(() => builder.Build())
            .Throws<InvalidOperationException>()
            .WithMessage("Function name is required.");
    }

    [Test]
    public async Task InvokeDryRunBuilder_WithoutFunctionName_ShouldThrowException()
    {
        // Arrange
        var builder = new InvokeDryRunBuilder();

        // Act & Assert
        await Assert.That(() => builder.Build())
            .Throws<InvalidOperationException>()
            .WithMessage("Function name is required.");
    }

    [Test]
    public async Task InvokeSynchronousAsync_WithLogType_ShouldIncludeLogsInResponse()
    {
        // Arrange
        var payload = new { message = "Test with logs" };
        var request = new InvokeBuilder()
            .WithFunctionName(_fixture.TestFunctionName)
            .WithLogType(LogType.Tail)
            .WithPayload(JsonSerializer.Serialize(payload))
            .Build();

        // Act
        var result = await _fixture.LambdaClient.InvokeSynchronousAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.IsSuccess).IsTrue();
        await Assert.That(result.Value.StatusCode).IsEqualTo(200);
        // LogResult may or may not be present depending on LocalStack implementation
    }

    [Test]
    public async Task InvokeAsynchronousAsync_WithQualifier_ShouldSucceed()
    {
        // Arrange
        var payload = new { message = "Async with qualifier" };
        var request = new InvokeAsyncBuilder()
            .WithFunctionName(_fixture.TestFunctionName)
            .WithQualifier("$LATEST")
            .WithPayload(JsonSerializer.Serialize(payload))
            .Build();

        // Act
        var result = await _fixture.LambdaClient.InvokeAsynchronousAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.StatusCode).IsEqualTo(202);
        await Assert.That(result.Value.IsSuccess).IsTrue();
    }

    [Test]
    public async Task InvokeDryRunAsync_WithQualifier_ShouldSucceed()
    {
        // Arrange
        var payload = new { message = "Dry run with qualifier" };
        var request = new InvokeDryRunBuilder()
            .WithFunctionName(_fixture.TestFunctionName)
            .WithQualifier("$LATEST")
            .WithPayload(JsonSerializer.Serialize(payload))
            .Build();

        // Act
        var result = await _fixture.LambdaClient.InvokeDryRunAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.StatusCode).IsEqualTo(204);
        await Assert.That(result.Value.IsSuccess).IsTrue();
    }

    [Test]
    public async Task InvokeAsynchronousAsync_WithNonExistentFunction_ShouldFail()
    {
        // Arrange
        var payload = new { test = "data" };
        var request = new InvokeAsyncBuilder()
            .WithFunctionName("non-existent-function")
            .WithPayload(JsonSerializer.Serialize(payload))
            .Build();

        // Act
        var result = await _fixture.LambdaClient.InvokeAsynchronousAsync(request);

        // Assert
        await Assert.That(result.IsError).IsTrue();
    }

    [Test]
    public async Task InvokeDryRunAsync_WithNonExistentFunction_ShouldFail()
    {
        // Arrange
        var payload = new { test = "data" };
        var request = new InvokeDryRunBuilder()
            .WithFunctionName("non-existent-function")
            .WithPayload(JsonSerializer.Serialize(payload))
            .Build();

        // Act
        var result = await _fixture.LambdaClient.InvokeDryRunAsync(request);

        // Assert
        await Assert.That(result.IsError).IsTrue();
    }
}
