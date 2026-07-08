namespace PlaylistApi.Application.DTOs;

// What the client sends to create a playlist.
public record CreatePlaylistDto(string Name, string UserId);

// What we send back - includes the songs so the client gets a full
// picture of the playlist in one call.
public record PlaylistDto(
    Guid Id,
    string Name,
    string UserId,
    DateTime CreatedAt,
    List<SongDto> Songs);
