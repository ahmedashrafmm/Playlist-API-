using PlaylistApi.Application.DTOs;
using PlaylistApi.Application.Interfaces;
using PlaylistApi.Domain.Entities;

namespace PlaylistApi.Application.Services;

// This class holds the actual BUSINESS logic. Controllers should stay
// "thin" - they just translate HTTP <-> this service. That separation
// means we can unit test all the real logic without spinning up a web
// server, and we could reuse this service from a CLI or a background
// job later.
public class PlaylistService : IPlaylistService
{
    private readonly IPlaylistRepository _repository;

    // The service depends on the INTERFACE, not a concrete EF Core
    // class. ASP.NET's dependency injection container will hand us
    // whatever concrete implementation is registered (we'll wire that
    // up in Program.cs). This is what makes the service unit-testable
    // with a fake/mock repository.
    public PlaylistService(IPlaylistRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlaylistDto> CreatePlaylistAsync(CreatePlaylistDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Playlist name is required.");

        if (string.IsNullOrWhiteSpace(dto.UserId))
            throw new ArgumentException("UserId is required.");

        var playlist = new Playlist
        {
            Name = dto.Name.Trim(),
            UserId = dto.UserId.Trim()
        };

        await _repository.AddPlaylistAsync(playlist);
        await _repository.SaveChangesAsync();

        return ToDto(playlist);
    }

    public async Task<PlaylistDto?> GetPlaylistByIdAsync(Guid id)
    {
        var playlist = await _repository.GetByIdAsync(id);
        return playlist is null ? null : ToDto(playlist);
    }

    public async Task<List<PlaylistDto>> GetPlaylistsForUserAsync(string userId)
    {
        var playlists = await _repository.GetByUserIdAsync(userId);
        return playlists.Select(ToDto).ToList();
    }

    public async Task<PlaylistDto?> AddSongToPlaylistAsync(Guid playlistId, AddSongDto dto)
    {
        var playlist = await _repository.GetByIdAsync(playlistId);
        if (playlist is null)
            return null; // Controller will translate this into a 404.

        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ArgumentException("Song title is required.");

        var song = new Song
        {
            Title = dto.Title.Trim(),
            Artist = dto.Artist?.Trim() ?? string.Empty,
            DurationSeconds = dto.DurationSeconds,
            PlaylistId = playlist.Id
        };

        await _repository.AddSongAsync(song);
        await _repository.SaveChangesAsync();

        playlist.Songs.Add(song);
        return ToDto(playlist);
    }

    // Centralizing the Entity -> DTO mapping here (instead of copy-pasting
    // it in every method) keeps things DRY.
    private static PlaylistDto ToDto(Playlist playlist) => new(
        playlist.Id,
        playlist.Name,
        playlist.UserId,
        playlist.CreatedAt,
        playlist.Songs.Select(s => new SongDto(s.Id, s.Title, s.Artist, s.DurationSeconds)).ToList()
    );
}
