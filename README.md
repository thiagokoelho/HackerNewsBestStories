# HackerNewsBestStories

# 📰 Hacker News Best Stories API

A  **.NET 8 Minimal API**  that returns the  **best N**  Hacker News stories ordered by score.  
Designed for  **high concurrency**  and  **resilience**  using  **Polly**,  `IHttpClientFactory`, in-memory caching, and bounded parallelism.

----------

## ✨ Highlights

-   Minimal API (no controllers; fast, clean)
-   Resilience with  **Polly**: Timeout, Retry with Jitter and Circuit Breaker
-   **IMemoryCache**  for IDs (30s) and items (1 min)
-   **Parallel fetching**  (`Parallel.ForEachAsync`) with  `MaxDegreeOfParallelism = 8`
-   JSON  **camelCase**  output
-   Docker support
-   Bruno  `.bru`  request files (Postman alternative)

----------
## 📡 API

`GET /stories/best?count={n}`

-   `count`  is optional; defaults to  `10`
-   Min =  `1`, Max =  `500`

**Example:**

`GET http://localhost:8080/stories/best?count=3]`

**Response:**
```json
[
  {
    "title": "It's insulting to read AI-generated blog posts",
    "uri": "https://blog.pabloecortez.com/its-insulting-to-read-your-ai-generated-blog-post/",
    "postedBy": "speckx",
    "time": "2025-10-27T15:27:38+00:00",
    "score": 794,
    "commentCount": 389
  },
  {
    "title": "A worker fell into a nuclear reactor pool",
    "uri": "https://www.nrc.gov/reading-rm/doc-collections/event-status/event/2025/20251022en?brid=vscAjql9kZL1FfGE7TYHVw#en57996:~:text=TRANSPORT%20OF%20CONTAMINATED%20PERSON%20OFFSITE",
    "postedBy": "nvahalik",
    "time": "2025-10-26T01:15:43+00:00",
    "score": 676,
    "commentCount": 486
  },
  {
    "title": "You already have a Git server",
    "uri": "https://maurycyz.com/misc/easy_git/",
    "postedBy": "chmaynard",
    "time": "2025-10-26T10:53:37+00:00",
    "score": 623,
    "commentCount": 415
  }
]
```
## 🧠 Technical Decisions

### Polly (Resilience)

We use **Polly** to protect both our API and Hacker News from overload and transient failures:

-   **Timeout**: fail fast if upstream is slow
    
-   **Retry (with decorrelated jitter)**: smooths traffic spikes (prevents thundering herd)
    
-   **Circuit Breaker**: stop hammering when HackerNews is unhealthy
    
    

### Parallelism & Throughput

HackerNews returns a list of story IDs; item details are fetched **concurrently** with `Parallel.ForEachAsync` and a cap (`MaxDegreeOfParallelism = 8`). This dramatically reduces latency while respecting upstream capacity.

### Caching

-   IDs (`beststories.json`) cached for **30 seconds**
    
-   Item payloads cached for **1 minute**
    
-   Reduces repeated calls for popular stories, crucial under high read loads

## ▶️ Run Locally (without Docker)

**Prerequisites**

-   .NET SDK 8+
    
**Steps**

   ```
   git clone https://github.com/<your-username>/<your-repo>.git
   cd <your-repo>
   
   dotnet restore
   dotnet build

# Run the API (project folder)
dotnet run --project HackerNewsBestStories.Api
   ```
The API will print the listening URL (e.g., `http://localhost:8080` or `http://localhost:5000`).  
Use: `http://localhost:8080/stories/best?count=3`

To force a port:
```
# Windows
set ASPNETCORE_URLS=http://localhost:8080

# Linux/macOS
export ASPNETCORE_URLS=http://localhost:8080
```
## 🐳 Run with Docker

**Dockerfile** is in `HackerNewsBestStories.Api/` (multi-stage build).

```
# from HackerNewsBestStories.Api folder (where the Dockerfile is)
docker build -t hackernews-api .
docker run --rm -p 8080:8080 hackernews-api
```
Open:
`http://localhost:8080/stories/best?count=5`

## 🧪 Tests

We provide **unit tests (Moq)** and **integration-style tests** with `WebApplicationFactory`.

Run all tests:

    dotnet test
-  Unit tests mock `IHttpClientFactory` with a **stub `HttpMessageHandler`** and use a real `MemoryCache`. 
-  Endpoint tests spin up the app in-memory and override the named HttpClient `"hackerNews"`.

## 🧰 Bruno collections (`.bru`)

This repo includes **Bruno** request files (`*.bru`) to quickly exercise endpoints.

-   Install Bruno: https://www.usebruno.com/
    
-   Open the folder containing `.bru` files (e.g., `Bruno/`)
    
-   Adjust host (if needed) and run:
    
    -   `GET /stories/best?count=3`
        
> If you prefer Postman or REST Client in VS Code, the `.bru` files are straightforward to translate.

## 🔭 Future Improvements

-   **Structured logging** 
    
-   **Health checks** (`/health`)
    
-   **Rate limiting** (`Microsoft.AspNetCore.RateLimiting`) to protect against abuse
    
-   **OpenTelemetry** (traces, metrics, logs) 
    
-   **Redis distributed cache** for multi-instance scale-out
    
-   **CI/CD** (GitHub Actions: build, test, containerize, push)
    
-   **Configurable parallelism & TTLs** via appsettings
