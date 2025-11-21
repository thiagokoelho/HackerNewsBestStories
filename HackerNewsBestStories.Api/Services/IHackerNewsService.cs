using HackerNewsBestStories.Api.Models;

namespace HackerNewsBestStories.Api.Services
{
    public interface IHackerNewsService
    {
        Task<List<StoryDto>> GetBestStoriesAsync(int n, CancellationToken ct = default);
    }
}
