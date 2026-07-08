namespace PlaylistApi.Application.DTOs;

// "record" instead of "class": these are immutable data carriers with
// built-in value equality, which is exactly what a DTO should be -
// nobody should be mutating a request object after it arrives.

// What the client sends us when adding a song.
public record AddSongDto(string Title, string Artist, int? DurationSeconds);

// What we send back to the client.
public record SongDto(Guid Id, string Title, string Artist, int? DurationSeconds);
