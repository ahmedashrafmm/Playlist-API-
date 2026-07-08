using Microsoft.EntityFrameworkCore;
using PlaylistApi.Domain.Entities;

namespace PlaylistApi.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<Song> Songs => Set<Song>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One Playlist -> Many Songs. If a Playlist is deleted, its
        // Songs are deleted too (Cascade) - a song can't meaningfully
        // exist without its playlist in this design.
        modelBuilder.Entity<Playlist>()
            .HasMany(p => p.Songs)
            .WithOne(s => s.Playlist)
            .HasForeignKey(s => s.PlaylistId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Playlist>()
            .Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        modelBuilder.Entity<Song>()
            .Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Index because we'll frequently query "give me all playlists
        // for this user" - without this, SQLite does a full table scan.
        modelBuilder.Entity<Playlist>()
            .HasIndex(p => p.UserId);
    }
}
