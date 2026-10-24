using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Polly.Timeout;
using SupportDesk;
using Xunit;

public sealed class PartnerTests
{
    private static ServiceProvider Build(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPartner().ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }
    [Fact]
    public async Task Get_retries_transient_failure_then_succeeds()
    {
        var handler = new DemoPartnerHandler();
        using var services = Build(handler);
        Assert.Equal(HttpStatusCode.OK, await services.GetRequiredService<PartnerClient>().CheckAsync(default));
        Assert.Equal(2, handler.Attempts);
    }
    [Theory]
    [InlineData(400, 1)]
    [InlineData(404, 1)]
    [InlineData(503, 3)]
    public async Task Status_policy_has_a_bounded_attempt_count(int status, int attempts)
    {
        var handler = new Stub((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)));
        using var services = Build(handler);
        Assert.Equal(status, (int)await services.GetRequiredService<PartnerClient>().CheckAsync(default));
        Assert.Equal(attempts, handler.Attempts);
    }
    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task Unsafe_method_is_not_retried(string method)
    {
        var handler = new Stub((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        using var services = Build(handler);
        using var client = services.GetRequiredService<IHttpClientFactory>().CreateClient(PartnerRegistration.Name);
        using var request = new HttpRequestMessage(new HttpMethod(method), "commands");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, handler.Attempts);
    }
    [Fact]
    public async Task Caller_cancellation_reaches_the_transport_and_does_not_retry()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new Stub(async (_, ct) => { entered.SetResult(); await Task.Delay(Timeout.Infinite, ct); return new(HttpStatusCode.OK); });
        using var services = Build(handler);
        using var cancellation = new CancellationTokenSource();
        var task = services.GetRequiredService<PartnerClient>().CheckAsync(cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.Equal(1, handler.Attempts);
    }
    [Fact]
    public async Task Hanging_transport_is_cancelled_by_pipeline_timeout()
    {
        var cancelled = 0;
        var handler = new Stub(async (_, ct) =>
        {
            try { await Task.Delay(Timeout.Infinite, ct); }
            catch (OperationCanceledException) { Interlocked.Increment(ref cancelled); throw; }
            return new(HttpStatusCode.OK);
        });
        using var services = Build(handler);
        await Assert.ThrowsAsync<TimeoutRejectedException>(async () =>
            await services.GetRequiredService<PartnerClient>().CheckAsync(default).WaitAsync(TimeSpan.FromSeconds(15)));
        Assert.InRange(handler.Attempts, 1, 3);
        Assert.Equal(handler.Attempts, cancelled);
    }
    private sealed class Stub(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Attempts;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        { Interlocked.Increment(ref Attempts); return send(request, ct); }
    }
}
