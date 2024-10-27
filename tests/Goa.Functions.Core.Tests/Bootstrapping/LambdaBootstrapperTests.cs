using Goa.Functions.Core.Bootstrapping;
using Moq;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using TUnit;

namespace Goa.Functions.Core.Tests.Bootstrapping;

public class LambdaBootstrapperTests
{
    [Test]
    public async Task OnRunAsync_WhenInitializationError_CallsInitializeErrorOnRuntimeApi()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var hasHandledInitializationError = false;
        var lambdaRuntime = new Mock<ILambdaRuntimeClient>();
        lambdaRuntime.Setup(x => x.ReportInitializationErrorAsync(It.IsAny<InitializationErrorPayload>(), It.IsAny<CancellationToken>()))
            .Callback((InitializationErrorPayload _, CancellationToken _) => { hasHandledInitializationError = true; })
            .ReturnsAsync(Result.Success());
        var sut = new LambdaBootstrapper<InitializationErrorFunction, Data, Data>(DataSerializationContext.Default, () => new(), lambdaRuntimeClient: lambdaRuntime.Object);

        await Assert.ThrowsAsync(() => sut.RunAsync(cts.Token));

        await Assert.That(hasHandledInitializationError).IsTrue();
        lambdaRuntime.Verify(x => x.ReportInitializationErrorAsync(It.IsAny<InitializationErrorPayload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnRunAsync_InvokesGetNextFunction()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var lambdaRuntime = new Mock<ILambdaRuntimeClient>();
        lambdaRuntime.Setup(x => x.GetNextInvocationAsync(It.IsAny<CancellationToken>()))
            .Callback((CancellationToken ct) =>
            {
                cts.Cancel();
            })
            .ReturnsAsync(Result<InvocationRequest>.Failure("Test"));
        var sut = new LambdaBootstrapper<SafeFunction, Data, Data>(DataSerializationContext.Default, () => new(), lambdaRuntimeClient: lambdaRuntime.Object);

        await sut.RunAsync(cts.Token);

        lambdaRuntime.Verify(x => x.GetNextInvocationAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnRunAsync_WhenSuccessfullyProcessed_CallsSendResponse()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var invocationId = Guid.NewGuid().ToString();
        var lambdaRuntime = new Mock<ILambdaRuntimeClient>();
        lambdaRuntime.Setup(x => x.GetNextInvocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvocationRequest>.Success(new InvocationRequest(invocationId, JsonSerializer.Serialize(new Data("test")), DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds().ToString(), "test")));
        lambdaRuntime.Setup(x => x.SendResponseAsync(invocationId, It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .Callback((string _, HttpContent _, CancellationToken _) =>
            {
                cts.Cancel();
            })
            .Returns(Task.FromResult(Result.Success()));
        var sut = new LambdaBootstrapper<SafeFunction, Data, Data>(DataSerializationContext.Default, () => new(), lambdaRuntimeClient: lambdaRuntime.Object);

        await sut.RunAsync(cts.Token);

        lambdaRuntime.Verify(x => x.GetNextInvocationAsync(It.IsAny<CancellationToken>()), Times.Once);
        lambdaRuntime.Verify(x => x.SendResponseAsync(invocationId, It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnRunAsync_WhenExceptionDuringProcessing_CallsReportInvocationError()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var invocationId = Guid.NewGuid().ToString();
        var lambdaRuntime = new Mock<ILambdaRuntimeClient>();
        lambdaRuntime.Setup(x => x.GetNextInvocationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<InvocationRequest>.Success(new InvocationRequest(invocationId, JsonSerializer.Serialize(new Data("test")), DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds().ToString(), "test")));
        lambdaRuntime.Setup(x => x.ReportInvocationErrorAsync(invocationId, It.IsAny<InvocationErrorPayload>(), It.IsAny<CancellationToken>()))
            .Callback((string _, InvocationErrorPayload _, CancellationToken _) =>
            {
                cts.Cancel();
            })
            .Returns(Task.FromResult(Result.Success()));
        var sut = new LambdaBootstrapper<InvocationErrorFunction, Data, Data>(DataSerializationContext.Default, () => new(), lambdaRuntimeClient: lambdaRuntime.Object);

        await sut.RunAsync(cts.Token);

        lambdaRuntime.Verify(x => x.GetNextInvocationAsync(It.IsAny<CancellationToken>()), Times.Once);
        lambdaRuntime.Verify(x => x.ReportInvocationErrorAsync(invocationId, It.IsAny<InvocationErrorPayload>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task OnRunAsync_CanLoopThroughInvocations()
    {
        var invocations = new Queue<InvocationRequest>();
        invocations.Enqueue(new InvocationRequest(Guid.NewGuid().ToString(), JsonSerializer.Serialize(new Data("test")), DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds().ToString(), "test"));
        invocations.Enqueue(new InvocationRequest(Guid.NewGuid().ToString(), JsonSerializer.Serialize(new Data("fail")), DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds().ToString(), "test"));
        invocations.Enqueue(new InvocationRequest(Guid.NewGuid().ToString(), JsonSerializer.Serialize(new Data("test")), DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds().ToString(), "test"));
        invocations.Enqueue(new InvocationRequest(Guid.NewGuid().ToString(), JsonSerializer.Serialize(new Data("fail")), DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds().ToString(), "test"));
        invocations.Enqueue(new InvocationRequest(Guid.NewGuid().ToString(), JsonSerializer.Serialize(new Data("test")), DateTimeOffset.UtcNow.AddSeconds(10).ToUnixTimeMilliseconds().ToString(), "test"));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var lambdaRuntime = new Mock<ILambdaRuntimeClient>();
        lambdaRuntime.Setup(x => x.GetNextInvocationAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                if (invocations.Count == 0)
                    cts.Cancel();
            })
            .ReturnsAsync(() =>
            {
                if (invocations.TryDequeue(out var invocation))
                    return Result<InvocationRequest>.Success(invocation);

                return Result<InvocationRequest>.Failure("Test");
            });
        lambdaRuntime.Setup(x => x.SendResponseAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Result.Success()));
        var sut = new LambdaBootstrapper<LoopInvocationFunction, Data, Data>(DataSerializationContext.Default, () => new(), lambdaRuntimeClient: lambdaRuntime.Object);

        await sut.RunAsync(cts.Token);

        lambdaRuntime.Verify(x => x.GetNextInvocationAsync(It.IsAny<CancellationToken>()), Times.Exactly(6));
        lambdaRuntime.Verify(x => x.SendResponseAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    // Test Functions
    public class SafeFunction : ILambdaFunction<Data, Data>
    {
        public Task<Data> InvokeAsync(Data request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request);
        }
    }

    public class InitializationErrorFunction : ILambdaFunction<Data, Data>
    {
        public InitializationErrorFunction()
        {
            throw new Exception("Test Initialization Error");
        }

        public Task<Data> InvokeAsync(Data request, CancellationToken cancellationToken)
        {
            return Task.FromResult(request);
        }
    }

    public class InvocationErrorFunction : ILambdaFunction<Data, Data>
    {
        public Task<Data> InvokeAsync(Data request, CancellationToken cancellationToken)
        {
            throw new Exception("Test Initialization Error");
        }
    }

    public class LoopInvocationFunction : ILambdaFunction<Data, Data>
    {
        public Task<Data> InvokeAsync(Data request, CancellationToken cancellationToken)
        {
            if (request.name.Equals("test"))
                return Task.FromResult(request);

            throw new Exception("Test Loop Error");
        }
    }
}

public record Data(string name);

[JsonSourceGenerationOptions(WriteIndented = false, UseStringEnumConverter = true, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Data))]
public partial class DataSerializationContext : JsonSerializerContext
{
}
