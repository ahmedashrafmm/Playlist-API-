namespace PlaylistApi.Domain.Entities;

public class Song
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;

    // Nullable because we don't want to force the client to know a
    // song's duration when adding it - it's optional metadata.
    public int? DurationSeconds { get; set; }

    // Foreign key back to the owning playlist.
    public Guid PlaylistId { get; set; }
    public Playlist? Playlist { get; set; }
}
