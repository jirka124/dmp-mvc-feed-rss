# FeedRSS

Simple RSS reader built as an ASP.NET Core MVC app (server-rendered Razor views) with EF Core and SQLite.

## Features

- add RSS feed (`name`, `url`)
- list/search feeds
- feed detail with article list
- filter articles by date and title
- reload articles from RSS source
- delete single or multiple feeds

## Tech Stack

- ASP.NET Core MVC (.NET 10)
- Entity Framework Core
- SQLite
- xUnit (tests)

## Run Locally

### Prerequisites

- .NET SDK 10

### Start app

```bash
dotnet run --project FeedRSS/FeedRSS.csproj
```

Then open the URL shown in the terminal (typically `http://localhost:xxxx`).

## Run Tests

```bash
dotnet test FeedRSS.Tests/FeedRSS.Tests.csproj
```

## Project Structure

- `FeedRSS/` - main MVC application
- `FeedRSS.Tests/` - automated tests
