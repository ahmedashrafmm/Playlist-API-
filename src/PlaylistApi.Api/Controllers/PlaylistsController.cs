using Microsoft.AspNetCore.Mvc;
using PlaylistApi.Application.DTOs;
using PlaylistApi.Application.Interfaces;

namespace PlaylistApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlaylistsController : ControllerBase
{
    private readonly IPlaylistService _service;

    // The controller only depends on IPlaylistService - it has never
    // heard of EF Core, SQLite, or the Playlist entity. Its whole job
    // is: parse HTTP request -> call service -> translate result to
    // an HTTP response.
    public PlaylistsController(IPlaylistService service)
    {
        _service = service;
    }

    // POST /api/playlists
    [HttpPost]
    public async Task<ActionResult<PlaylistDto>> CreatePlaylist([FromBody] CreatePlaylistDto dto)
    {
        try
        {
            var playlist = await _service.CreatePlaylistAsync(dto);
            // 201 Created + a Location header pointing at GET /api/playlists/{id}
            // is the correct REST response for a successful creation.
            return CreatedAtAction(nameof(GetPlaylistById), new { id = playlist.Id }, playlist);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // GET /api/playlists/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PlaylistDto>> GetPlaylistById(Guid id)
    {
        var playlist = await _service.GetPlaylistByIdAsync(id);
        return playlist is null ? NotFound() : Ok(playlist);
    }

    // GET /api/playlists?userId=abc
    [HttpGet]
    public async Task<ActionResult<List<PlaylistDto>>> GetPlaylists([FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest(new { error = "userId query parameter is required." });

        var playlists = await _service.GetPlaylistsForUserAsync(userId);
        return Ok(playlists);
    }

    // PUT /api/playlists/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PlaylistDto>> UpdatePlaylist(Guid id, [FromBody] UpdatePlaylistDto dto)
    {
        try
        {
            var playlist = await _service.UpdatePlaylistAsync(id, dto);
            return playlist is null ? NotFound(new { error = $"Playlist {id} not found." }) : Ok(playlist);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE /api/playlists/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePlaylist(Guid id)
    {
        var deleted = await _service.DeletePlaylistAsync(id);
        // 204 No Content is the conventional REST response for a
        // successful DELETE - there's no body to return.
        return deleted ? NoContent() : NotFound(new { error = $"Playlist {id} not found." });
    }

    // POST /api/playlists/{id}/songs
    [HttpPost("{id:guid}/songs")]
    public async Task<ActionResult<PlaylistDto>> AddSong(Guid id, [FromBody] AddSongDto dto)
    {
        try
        {
            var playlist = await _service.AddSongToPlaylistAsync(id, dto);
            return playlist is null ? NotFound(new { error = $"Playlist {id} not found." }) : Ok(playlist);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // PUT /api/playlists/{id}/songs/{songId}
    [HttpPut("{id:guid}/songs/{songId:guid}")]
    public async Task<ActionResult<PlaylistDto>> UpdateSong(Guid id, Guid songId, [FromBody] UpdateSongDto dto)
    {
        try
        {
            var playlist = await _service.UpdateSongAsync(id, songId, dto);
            return playlist is null
                ? NotFound(new { error = $"Song {songId} not found in playlist {id}." })
                : Ok(playlist);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // DELETE /api/playlists/{id}/songs/{songId}
    [HttpDelete("{id:guid}/songs/{songId:guid}")]
    public async Task<ActionResult<PlaylistDto>> DeleteSong(Guid id, Guid songId)
    {
        var playlist = await _service.DeleteSongAsync(id, songId);
        return playlist is null
            ? NotFound(new { error = $"Song {songId} not found in playlist {id}." })
            : Ok(playlist);
    }
}
