using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Timeout;

public static class Policies
{
    public static IAsyncPolicy<HttpResponseMessage> TimeoutPolicy =>
        Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30), TimeoutStrategy.Optimistic);

    public static IAsyncPolicy<HttpResponseMessage> RetryJitterPolicy
    {
        get
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 3);
            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TimeoutRejectedException>()
                .OrResult(r => (int)r.StatusCode is >= 500 or 429) // 5xx e 429
                .WaitAndRetryAsync(delay);
        }
    }

    public static IAsyncPolicy<HttpResponseMessage> CircuitBreakerPolicy =>
        Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult(r => (int)r.StatusCode is >= 500 or 429)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 8,
                durationOfBreak: TimeSpan.FromSeconds(30));
}