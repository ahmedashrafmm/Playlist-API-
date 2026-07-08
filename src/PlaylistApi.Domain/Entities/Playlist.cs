namespace PlaylistApi.Domain.Entities;

// This is a POCO (Plain Old CLR Object) - it knows nothing about
// EF Core, HTTP, or the database. That's intentional: the Domain layer
// should be the same whether we're talking to SQLite, Postgres, or a
// unit test's in-memory list.
public class Playlist
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    // Placeholder for ownership. In a real system this would come from
    // an authenticated user's claims (e.g. a JWT "sub" claim), but auth
    // is out of scope for this test, so it's passed explicitly instead.
    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property: EF Core uses this to load related Songs.
    // One playlist -> many songs.
    public ICollection<Song> Songs { get; set; } = new List<Song>();
}
