using PlaylistApi.Domain.Entities;

namespace PlaylistApi.Application.Interfaces;

// This interface lives in the Application layer, NOT Infrastructure.
// That's the key to Dependency Inversion: Application defines what it
// NEEDS, and Infrastructure provides it. Application never references
// Infrastructure, EF Core, or SQLite directly.
public interface IPlaylistRepository
{
    Task AddPlaylistAsync(Playlist playlist);
    Task<Playlist?> GetByIdAsync(Guid id);
    Task<List<Playlist>> GetByUserIdAsync(string userId);
    Task AddSongAsync(Song song);
    Task SaveChangesAsync();
}
