namespace HackerNewsBestStories.Api.Models
{
    using System.Text.Json.Serialization;

    public record StoryDto
    {
        [JsonPropertyName("title")]
        public string Title { get; init; } = string.Empty;

        [JsonPropertyName("uri")]
        public string Uri { get; init; } = string.Empty;

        [JsonPropertyName("postedBy")]
        public string PostedBy { get; init; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; init; } = string.Empty;

        [JsonPropertyName("score")]
        public int Score { get; init; }

        [JsonPropertyName("commentCount")]
        public int CommentCount { get; init; }
    }
}
