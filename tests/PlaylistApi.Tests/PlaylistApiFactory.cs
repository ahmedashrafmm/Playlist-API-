using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlaylistApi.Infrastructure.Data;

namespace PlaylistApi.Tests;

// WebApplicationFactory<Program> boots the ENTIRE real app - Program.cs,
// DI container, middleware pipeline, routing - in memory, and gives us
// an HttpClient to make real HTTP calls against it. This is what makes
// these "integration" tests rather than unit tests: we're exercising
// the whole stack (Controller -> Service -> Repository -> DbContext),
// just swapping the real SQLite file for an isolated one so tests don't
// pollute (or depend on) playlists.db.
public class PlaylistApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            // :memory: SQLite requires the connection to stay open for
            // the lifetime of the database - closing it deletes the data.
            _connection.Open();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        _connection.Dispose();
        base.Dispose(disposing);
    }
}
