using PlaylistApi.Application.DTOs;

namespace PlaylistApi.Application.Interfaces;

public interface IPlaylistService
{
    Task<PlaylistDto> CreatePlaylistAsync(CreatePlaylistDto dto);
    Task<PlaylistDto?> GetPlaylistByIdAsync(Guid id);
    Task<List<PlaylistDto>> GetPlaylistsForUserAsync(string userId);
    Task<PlaylistDto?> UpdatePlaylistAsync(Guid id, UpdatePlaylistDto dto);
    Task<bool> DeletePlaylistAsync(Guid id);

    Task<PlaylistDto?> AddSongToPlaylistAsync(Guid playlistId, AddSongDto dto);
    Task<PlaylistDto?> UpdateSongAsync(Guid playlistId, Guid songId, UpdateSongDto dto);
    Task<PlaylistDto?> DeleteSongAsync(Guid playlistId, Guid songId);
}
