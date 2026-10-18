using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using System.Net;

namespace SupportDesk;

public static class PartnerRegistration
{
    public const string Name = "partner";
    public static IHttpClientBuilder AddPartner(this IServiceCollection services)
    {
        var builder = services.AddHttpClient(Name, client =>
        {
            client.BaseAddress = new Uri("https://partner.invalid/");
            client.Timeout = Timeout.InfiniteTimeSpan; // One timeout owner: the resilience pipeline.
        });
        builder.AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
            options.Retry.MaxRetryAttempts = 2; // Initial call + at most two retries.
            options.Retry.Delay = TimeSpan.Zero; // Lab only; production needs backoff + jitter.
            options.Retry.UseJitter = false;
            options.Retry.DisableForUnsafeHttpMethods();
        });
        services.AddTransient<PartnerClient>();
        return builder;
    }
}

public sealed class PartnerClient(IHttpClientFactory factory)
{
    // Only a fixed GET; no arbitrary URL and no mutation to accidentally duplicate.
    public async Task<HttpStatusCode> CheckAsync(CancellationToken cancellationToken)
    {
        using var client = factory.CreateClient(PartnerRegistration.Name);
        using var request = new HttpRequestMessage(HttpMethod.Get, "health");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return response.StatusCode; // Status-only protocol: no response body is consumed.
    }
}

// Explicit demo transport. Real registrations omit ConfigurePrimaryHttpMessageHandler.
public sealed class DemoPartnerHandler : HttpMessageHandler
{
    private int attempts;
    public int Attempts => attempts;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var number = Interlocked.Increment(ref attempts);
        return Task.FromResult(new HttpResponseMessage(number == 1 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
    }
}
