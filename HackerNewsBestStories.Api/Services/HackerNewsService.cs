using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

public sealed class HackerNewsService : IHackerNewsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan IdsTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ItemTtl = TimeSpan.FromMinutes(1);
    private const int MaxParallelism = 8;

    public HackerNewsService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public async Task<List<StoryDto>> GetBestStoriesAsync(int n, CancellationToken ct = default)
    {
        var ids = await GetBestStoryIdsAsync(ct);
        var topIds = ids.Take(n).ToArray(); //Gets the nth stories IDs

        var bag = new ConcurrentBag<StoryDto>();

        await Parallel.ForEachAsync(
            topIds,
            new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism, CancellationToken = ct },
            async (id, token) =>
            {
                var item = await GetItemAsync(id, token);

                var storyDto = ItemToStoryDto(item);
                if (storyDto is not null) 
                    bag.Add(storyDto);
            });

        return bag
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.CommentCount)
            .ToList();
    }

    private async Task<int[]> GetBestStoryIdsAsync(CancellationToken ct)
    {
        const string cacheKey = "hackernews_best_ids";
        if (_cache.TryGetValue(cacheKey, out int[] cached))
            return cached;

        var client = _httpClientFactory.CreateClient("hackerNews");
        var ids = await client.GetFromJsonAsync<int[]>("beststories.json", cancellationToken: ct)
                  ?? Array.Empty<int>();

        _cache.Set(cacheKey, ids, IdsTtl);
        return ids;
    }

    private async Task<Item?> GetItemAsync(int id, CancellationToken ct)
    {
        string key = $"hackernews_item_{id}";
        if (_cache.TryGetValue(key, out Item cached))
            return cached;

        var client = _httpClientFactory.CreateClient("hackerNews");
        var item = await client.GetFromJsonAsync<Item>($"item/{id}.json", cancellationToken: ct);

        if (item is not null)
            _cache.Set(key, item, ItemTtl);

        return item;
    }

    private static StoryDto? ItemToStoryDto(Item item)
    {
        if (item.Title is null) 
            return null;

        var uri = item.Url ?? string.Empty;
        var postedBy = item.By ?? "unknown";
        var timeIso = DateTimeOffset
            .FromUnixTimeSeconds(item.Time)
            .ToUniversalTime()
            .ToString("yyyy-MM-ddTHH:mm:sszzz");

        return new StoryDto()
        {
            Title = item.Title,
            Uri = uri,
            PostedBy = postedBy,
            Time = timeIso,
            Score = item.Score ?? 0,
            CommentCount = item.Descendants ?? 0
        };
    }
}
