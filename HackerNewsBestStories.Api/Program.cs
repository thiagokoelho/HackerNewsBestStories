using HackerNewsBestStories.Api.Models;
using HackerNewsBestStories.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();

builder.Services.AddHttpClient("hackerNews", c =>
{
    c.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
    c.DefaultRequestHeaders.UserAgent.ParseAdd("HackerNewsBestStories/1.0 (+https://example)");
    c.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(Policies.TimeoutPolicy)
.AddPolicyHandler(Policies.RetryJitterPolicy)
.AddPolicyHandler(Policies.CircuitBreakerPolicy);

builder.Services.AddSingleton<IHackerNewsService, HackerNewsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/stories/best", async (int? count, IHackerNewsService service, CancellationToken ct) =>
{
    int n = Math.Clamp(count ?? 10, 1, 500); //use default n of 10
    var stories = await service.GetBestStoriesAsync(n, ct);
    return Results.Ok(stories);
})
.WithName("GetBestStories")
.Produces<List<StoryDto>>(StatusCodes.Status200OK);

app.Run();