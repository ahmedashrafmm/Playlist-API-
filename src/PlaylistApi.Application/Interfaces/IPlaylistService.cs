using PlaylistApi.Application.DTOs;

namespace PlaylistApi.Application.Interfaces;

public interface IPlaylistService
{
    Task<PlaylistDto> CreatePlaylistAsync(CreatePlaylistDto dto);
    Task<PlaylistDto?> GetPlaylistByIdAsync(Guid id);
    Task<List<PlaylistDto>> GetPlaylistsForUserAsync(string userId);
    Task<PlaylistDto?> AddSongToPlaylistAsync(Guid playlistId, AddSongDto dto);
}
