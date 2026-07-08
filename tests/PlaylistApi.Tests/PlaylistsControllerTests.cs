using System.Net;
using System.Net.Http.Json;
using PlaylistApi.Application.DTOs;
using Xunit;

namespace PlaylistApi.Tests;

// IClassFixture<PlaylistApiFactory> shares one running app instance
// across all tests in this class (cheaper than booting a new one per
// test), while each test still gets its own HttpClient.
public class PlaylistsControllerTests : IClassFixture<PlaylistApiFactory>
{
    private readonly HttpClient _client;

    public PlaylistsControllerTests(PlaylistApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePlaylist_ThenGetById_ReturnsTheSamePlaylist()
    {
        // Act 1: create a playlist via a real POST request
        var createResponse = await _client.PostAsJsonAsync(
            "/api/playlists",
            new CreatePlaylistDto("Integration Test Playlist", "test-user"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PlaylistDto>();
        Assert.NotNull(created);

        // Act 2: fetch it back via a real GET request
        var getResponse = await _client.GetAsync($"/api/playlists/{created!.Id}");
        getResponse.EnsureSuccessStatusCode();

        var fetched = await getResponse.Content.ReadFromJsonAsync<PlaylistDto>();
        Assert.Equal("Integration Test Playlist", fetched!.Name);
        Assert.Empty(fetched.Songs);
    }

    [Fact]
    public async Task AddSong_ToExistingPlaylist_ReturnsPlaylistWithSong()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/playlists",
            new CreatePlaylistDto("Songs Test Playlist", "test-user"));
        var playlist = await createResponse.Content.ReadFromJsonAsync<PlaylistDto>();

        var addSongResponse = await _client.PostAsJsonAsync(
            $"/api/playlists/{playlist!.Id}/songs",
            new AddSongDto("Test Song", "Test Artist", 180));

        addSongResponse.EnsureSuccessStatusCode();
        var updated = await addSongResponse.Content.ReadFromJsonAsync<PlaylistDto>();

        Assert.Single(updated!.Songs);
        Assert.Equal("Test Song", updated.Songs[0].Title);
    }

    [Fact]
    public async Task GetById_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/playlists/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePlaylist_WithEmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/playlists",
            new CreatePlaylistDto("", "test-user"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePlaylist_ChangesTheName()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/playlists", new CreatePlaylistDto("Original Name", "test-user"));
        var playlist = await createResponse.Content.ReadFromJsonAsync<PlaylistDto>();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/playlists/{playlist!.Id}", new UpdatePlaylistDto("Renamed"));

        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<PlaylistDto>();
        Assert.Equal("Renamed", updated!.Name);
    }

    [Fact]
    public async Task DeletePlaylist_ThenGetById_Returns404()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/playlists", new CreatePlaylistDto("To Be Deleted", "test-user"));
        var playlist = await createResponse.Content.ReadFromJsonAsync<PlaylistDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/playlists/{playlist!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/playlists/{playlist.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteSong_RemovesItFromThePlaylist()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/playlists", new CreatePlaylistDto("Playlist With Song", "test-user"));
        var playlist = await createResponse.Content.ReadFromJsonAsync<PlaylistDto>();

        var addSongResponse = await _client.PostAsJsonAsync(
            $"/api/playlists/{playlist!.Id}/songs", new AddSongDto("Doomed Song", "Artist", 100));
        var withSong = await addSongResponse.Content.ReadFromJsonAsync<PlaylistDto>();
        var songId = withSong!.Songs[0].Id;

        var deleteResponse = await _client.DeleteAsync($"/api/playlists/{playlist.Id}/songs/{songId}");
        deleteResponse.EnsureSuccessStatusCode();

        var afterDelete = await deleteResponse.Content.ReadFromJsonAsync<PlaylistDto>();
        Assert.Empty(afterDelete!.Songs);
    }

    [Fact]
    public async Task GetPlaylists_FilteredByUserId_OnlyReturnsThatUsersPlaylists()
    {
        await _client.PostAsJsonAsync("/api/playlists", new CreatePlaylistDto("User A Playlist", "user-a"));
        await _client.PostAsJsonAsync("/api/playlists", new CreatePlaylistDto("User B Playlist", "user-b"));

        var response = await _client.GetAsync("/api/playlists?userId=user-a");
        response.EnsureSuccessStatusCode();

        var playlists = await response.Content.ReadFromJsonAsync<List<PlaylistDto>>();
        Assert.All(playlists!, p => Assert.Equal("user-a", p.UserId));
    }
}
