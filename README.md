# Playlist API

A REST API for creating playlists and adding songs to them, built for the
Academy Backend Developer Test.

## Tech stack

- **ASP.NET Core 10** (Web API, controller-based)
- **Entity Framework Core 10** as the ORM
- **SQLite** as the database

## Why SQLite?

For a scoped test project like this, I prioritized three things: zero
setup friction for whoever grades it, cross-platform portability (a
single `.db` file, no server process to install/configure), and "good
enough" relational guarantees (foreign keys, indexes, ACID transactions)
for a small two-table schema. A client-server database like PostgreSQL
would be the right call in production — it handles concurrent writes
and larger data volumes far better than SQLite — but it would add a
"do you have Postgres running locally?" dependency that isn't worth it
here. Because the code only talks to `IPlaylistRepository` (see below),
swapping to PostgreSQL later is a one-line change in `Program.cs` plus
changing one NuGet package — not a rewrite.

## Architecture

The solution is split into four projects, following a simplified Clean
Architecture:

```
PlaylistApi.Domain           <- entities (Playlist, Song). No dependencies.
PlaylistApi.Application      <- DTOs, interfaces, business logic (PlaylistService)
PlaylistApi.Infrastructure   <- EF Core DbContext + repository (implements Application's interfaces)
PlaylistApi.Api              <- Controllers, DI wiring, Program.cs
```

Dependencies only point inward: `Api` depends on `Application` and
`Infrastructure`; `Infrastructure` depends on `Application`;
`Application` depends only on `Domain`. `Domain` depends on nothing.

### "Why not just query the database directly in the controller?"

This is the question most likely to come up, so here's the reasoning
in full:

**1. Single Responsibility.** A controller's job is to translate HTTP
into a method call and a result back into HTTP (status codes, JSON
shape, headers). The moment it also contains `_context.Playlists.Where(...)`,
it's doing two unrelated jobs: understanding HTTP *and* understanding
how to query a relational database. If either one changes — say, we
add gRPC support, or we change how playlists are looked up — you're
editing a file that has more reasons to change than it should.

**2. Testability.** `PlaylistService` (Application layer) depends on
`IPlaylistRepository`, an interface — not EF Core's `DbContext`
directly. In `PlaylistApi.Tests`, I test the business logic (e.g. "you
can't create a playlist with an empty name") against a **fake**
in-memory repository, with no real database involved. That test runs
in milliseconds and doesn't need SQLite installed. If the query logic
lived inside the controller, testing that same business rule would
require spinning up a real (or in-memory) EF Core context for every
test — slower, and it's really testing EF Core's plumbing, not my
logic.

**3. Dependency Inversion (the EF Core detail is "hidden").** The
`Application` layer defines *what it needs* (`IPlaylistRepository`)
without knowing *how* it's satisfied. `Infrastructure` provides the
EF Core implementation. Nothing above `Infrastructure` — not the
service, not the controller — imports `Microsoft.EntityFrameworkCore`.
That means: if a future requirement said "actually, use MongoDB
instead," only `PlaylistApi.Infrastructure` would need to change. The
controller and the business logic wouldn't need to be touched at all.

**4. It keeps business rules in one place.** Validation like "a
playlist needs a non-empty name" lives in `PlaylistService`, not
scattered across multiple controller actions that each remember to
check it. Centralizing it means it can't be forgotten in one endpoint
and not another.

The tradeoff is more files and more indirection for what is, admittedly,
a small API. For a two-endpoint throwaway script, this would be
overkill. For something meant to demonstrate SOLID and design patterns
to a grader — and for anything that's expected to grow — it's the
right tradeoff.

### Design patterns used

- **Repository pattern** (`IPlaylistRepository` / `PlaylistRepository`) —
  hides persistence details behind a simple interface.
- **Dependency Injection** — ASP.NET Core's built-in DI container wires
  interfaces to implementations in `Program.cs` (the single "composition
  root"). Every class asks for interfaces in its constructor rather than
  constructing its own dependencies.
- **DTO pattern** — API requests/responses use `CreatePlaylistDto`,
  `PlaylistDto`, etc. instead of exposing `Playlist`/`Song` entities
  directly, decoupling the public API contract from the internal data
  model.
- **Unit of Work** (informally, via `SaveChangesAsync`) — repository
  methods stage changes; nothing hits the database until `SaveChangesAsync`
  is called, so a service method that touches multiple entities commits
  them together.

## Database schema

Two tables, one relationship:

```
Playlists                          Songs
----------                         -----------
Id           GUID (PK)             Id                GUID (PK)
Name         TEXT, required        Title              TEXT, required
UserId       TEXT, indexed         Artist             TEXT
CreatedAt    DATETIME              DurationSeconds    INTEGER, nullable
                                   PlaylistId         GUID (FK -> Playlists.Id, cascade delete)
```

- One `Playlist` has many `Songs` (a `Song` belongs to exactly one
  playlist in this design).
- `UserId` is indexed because the main read query is "all playlists for
  this user."
- Deleting a playlist cascades and deletes its songs — a song has no
  meaning detached from its playlist here.

## API Endpoints

| Method | Route | Description |
|---|---|---|
| POST | `/api/playlists` | Create a playlist. Body: `{ "name": "...", "userId": "..." }` |
| GET | `/api/playlists/{id}` | Get a single playlist (with its songs) by id |
| GET | `/api/playlists?userId=abc` | Get all playlists for a user |
| PUT | `/api/playlists/{id}` | Rename a playlist. Body: `{ "name": "..." }` |
| DELETE | `/api/playlists/{id}` | Delete a playlist (cascades to its songs). Returns `204 No Content` |
| POST | `/api/playlists/{id}/songs` | Add a song to a playlist. Body: `{ "title": "...", "artist": "...", "durationSeconds": 210 }` |
| PUT | `/api/playlists/{id}/songs/{songId}` | Update a song's details |
| DELETE | `/api/playlists/{id}/songs/{songId}` | Remove a song from a playlist. Returns the updated playlist |

Note the asymmetry: deleting a *playlist* returns `204 No Content` (nothing
meaningful left to return), while deleting a *song* returns the *updated
playlist* with `200 OK` — the client typically wants to see the remaining
song list immediately without a follow-up GET.

## How to run this locally

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
# 1. Clone the repo, then from the solution root:
cd PlaylistApi

# 2. Restore NuGet packages
dotnet restore

# 3. Run the API (the database and schema are created automatically on first run)
dotnet run --project src/PlaylistApi.Api
```


The API will start at `http://localhost:5080` and open Swagger UI at
`http://localhost:5080/swagger`, where you can try every endpoint
interactively without needing Postman.

To run the tests:

```bash
cd PlaylistApi
dotnet test
```

## AI usage disclosure

This project was built with Claude (Anthropic) as a step-by-step pairing
partner. I directed the architecture decisions (stack, database, layering)
and Claude generated code file-by-file with inline reasoning, which I
reviewed, understood, and can defend line-by-line, including the tradeoffs
noted above — including diagnosing and fixing a real bug where the database
tables weren't being created on a fresh clone.

Full conversation: https://claude.ai/share/04c318da-6a08-459f-9cc1-ee1a6c9d7572
