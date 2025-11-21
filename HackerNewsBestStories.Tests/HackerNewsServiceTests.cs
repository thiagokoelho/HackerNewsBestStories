using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using HackerNewsBestStories.Api.Services;
using HackerNewsBestStories.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Moq;

public class HackerNewsServiceTests
{
    private static (IHackerNewsService Svc, StubHttpMessageHandler Handler) CreateService(
        string bestIdsJson,
        Dictionary<int, string> itemsJson)
    {
        // Build a single stub handler that serves both endpoints based on URL
        var handler = new StubHttpMessageHandler(req =>
        {
            var path = req.RequestUri!.AbsolutePath;

            if (path.EndsWith("/v0/beststories.json"))
                return StubHttpMessageHandler.Json(HttpStatusCode.OK, bestIdsJson);

            var itemMatch = System.Text.RegularExpressions.Regex.Match(path, @"\/item\/(\d+)\.json");
            if (itemMatch.Success)
            {
                int id = int.Parse(itemMatch.Groups[1].Value);
                if (itemsJson.TryGetValue(id, out var json))
                    return StubHttpMessageHandler.Json(HttpStatusCode.OK, json);
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/") };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("hackerNews")).Returns(httpClient);

        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        var svc = new HackerNewsService(factory.Object, memoryCache);
        return (svc, handler);
    }

    [Fact]
    public async Task GetBestStoriesAsync_Should_Return_TopN_Ordered_By_Score_Desc()
    {
        // Arrange
        var bestIds = "[111,222,333,444]";
        var items = new Dictionary<int, string>
        {
            [111] = """{"id":111,"title":"A","by":"u1","time":1761424000,"score":50,"descendants":5,"type":"story","url":"https://a"}""",
            [222] = """{"id":222,"title":"B","by":"u2","time":1761424001,"score":70,"descendants":7,"type":"story","url":"https://b"}""",
            [333] = """{"id":333,"title":"C","by":"u3","time":1761424002,"score":60,"descendants":6,"type":"story","url":"https://c"}""",
            [444] = """{"id":444,"title":"D","by":"u4","time":1761424003,"score":40,"descendants":4,"type":"story","url":"https://d"}"""
        };

        var (svc, handler) = CreateService(bestIds, items);

        // Act
        var result = await svc.GetBestStoriesAsync(3);

        // Assert
        result.Should().HaveCount(3);
        result.Select(s => s.Title).Should().ContainInOrder("B", "C", "A"); // 70,60,50
    }

    [Fact]
    public async Task GetBestStoriesAsync_Should_Map_Fields_Correctly()
    {
        // Arrange
        var bestIds = "[10]";
        var items = new Dictionary<int, string>
        {
            [10] = """{"id":10,"title":"Hello","by":"alice","time":1761424738,"score":248,"descendants":57,"type":"story","url":"https://example"}"""
        };
        var (svc, handler) = CreateService(bestIds, items);

        // Act
        var result = await svc.GetBestStoriesAsync(1);

        // Assert
        result.Should().ContainSingle();
        var s = result.First();
        s.Title.Should().Be("Hello");
        s.Uri.Should().Be("https://example");
        s.PostedBy.Should().Be("alice");
        s.Score.Should().Be(248);
        s.CommentCount.Should().Be(57);
        s.Time.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\+00:00$");
    }

    [Fact]
    public async Task GetBestStoriesAsync_Should_Limit_To_N()
    {
        // Arrange
        var bestIds = "[1,2,3,4,5]";
        var items = Enumerable.Range(1, 5).ToDictionary(
            i => i,
            i => $$"""{"id":{{i}},"title":"T{{i}}","by":"u","time":1761424000,"score":{{i}},"descendants":0,"type":"story"}"""
        );
        var (svc, handler) = CreateService(bestIds, items);

        // Act
        var result = await svc.GetBestStoriesAsync(2);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBestStoriesAsync_Should_Use_Cache_For_BestIds_And_Items()
    {
        // Arrange
        var bestIds = "[42]";
        var items = new Dictionary<int, string>
        {
            [42] = """{"id":42,"title":"Cached","by":"u","time":1761424000,"score":1,"descendants":0,"type":"story"}"""
        };
        var (svc, handler) = CreateService(bestIds, items);

        // Act
        var r1 = await svc.GetBestStoriesAsync(1);
        var callsAfterFirst = handler.Calls;
        var r2 = await svc.GetBestStoriesAsync(1);
        var callsAfterSecond = handler.Calls;

        // Assert
        r1.Should().HaveCount(1);
        r2.Should().HaveCount(1);

        // 1 call for beststories + 1 call for item on first run; second run should hit cache
        (callsAfterSecond - callsAfterFirst).Should().Be(0);
    }
}
