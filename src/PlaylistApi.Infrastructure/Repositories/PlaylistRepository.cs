using Microsoft.EntityFrameworkCore;
using PlaylistApi.Application.Interfaces;
using PlaylistApi.Domain.Entities;
using PlaylistApi.Infrastructure.Data;

namespace PlaylistApi.Infrastructure.Repositories;

// This class implements the interface that Application defined.
// Only this file (and AppDbContext) know that EF Core / SQLite exist.
public class PlaylistRepository : IPlaylistRepository
{
    private readonly AppDbContext _context;

    public PlaylistRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddPlaylistAsync(Playlist playlist)
    {
        await _context.Playlists.AddAsync(playlist);
    }

    public async Task<Playlist?> GetByIdAsync(Guid id)
    {
        // Include() eager-loads the related Songs in the same query -
        // without it, playlist.Songs would come back empty because EF
        // doesn't load navigation properties automatically.
        return await _context.Playlists
            .Include(p => p.Songs)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Playlist>> GetByUserIdAsync(string userId)
    {
        return await _context.Playlists
            .Include(p => p.Songs)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public void RemovePlaylist(Playlist playlist)
    {
        _context.Playlists.Remove(playlist);
    }

    public async Task AddSongAsync(Song song)
    {
        await _context.Songs.AddAsync(song);
    }

    public async Task<Song?> GetSongByIdAsync(Guid songId)
    {
        return await _context.Songs.FirstOrDefaultAsync(s => s.Id == songId);
    }

    public void RemoveSong(Song song)
    {
        _context.Songs.Remove(song);
    }

    public async Task SaveChangesAsync()
    {
        // EF Core's "unit of work" pattern: AddAsync only stages the
        // change in memory. Nothing hits the database until we call
        // SaveChangesAsync. This lets the Service layer batch multiple
        // operations into a single transaction if needed.
        await _context.SaveChangesAsync();
    }
}
