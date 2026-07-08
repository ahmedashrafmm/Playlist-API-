namespace PlaylistApi.Application.DTOs;

// PATCH-like partial updates would need nullable fields on every property;
// here we keep it simple with a full replace of the editable fields (PUT semantics).
public record UpdatePlaylistDto(string Name);

public record UpdateSongDto(string Title, string Artist, int? DurationSeconds);
