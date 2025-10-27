using System.Text.Json.Serialization;

namespace HackerNewsBestStories.Api.Models
{
    public record Item
    {
        [JsonPropertyName("by")]
        public string? By { get; init; }

        [JsonPropertyName("descendants")]
        public int? Descendants { get; init; }

        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("kids")]
        public List<int>? Kids { get; init; }

        [JsonPropertyName("score")]
        public int? Score { get; init; }

        [JsonPropertyName("text")]
        public string? Text { get; init; }

        [JsonPropertyName("time")]
        public long Time { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }
}
