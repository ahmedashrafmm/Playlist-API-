using Moq;
using PlaylistApi.Application.DTOs;
using PlaylistApi.Application.Interfaces;
using PlaylistApi.Application.Services;
using PlaylistApi.Domain.Entities;
using Xunit;

namespace PlaylistApi.Tests;

// These are true UNIT tests: IPlaylistRepository is mocked (faked), so
// no database, no file system, nothing but the PlaylistService logic
// itself is under test. That's the payoff of the Repository pattern -
// this whole file runs in milliseconds.
public class PlaylistServiceTests
{
    private readonly Mock<IPlaylistRepository> _repositoryMock;
    private readonly PlaylistService _sut; // "sut" = System Under Test

    public PlaylistServiceTests()
    {
        _repositoryMock = new Mock<IPlaylistRepository>();
        _sut = new PlaylistService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreatePlaylistAsync_WithValidData_ReturnsPlaylistDtoWithMatchingFields()
    {
        // Arrange
        var dto = new CreatePlaylistDto("Road Trip", "user-123");

        // Act
        var result = await _sut.CreatePlaylistAsync(dto);

        // Assert
        Assert.Equal("Road Trip", result.Name);
        Assert.Equal("user-123", result.UserId);
        Assert.Empty(result.Songs);

        // Verify the service actually asked the repository to persist
        // the playlist, exactly once.
        _repositoryMock.Verify(r => r.AddPlaylistAsync(It.IsAny<Playlist>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePlaylistAsync_WithEmptyName_ThrowsArgumentException(string invalidName)
    {
        var dto = new CreatePlaylistDto(invalidName, "user-123");

        await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreatePlaylistAsync(dto));

        // The repository should never be touched if validation fails -
        // this proves validation happens BEFORE any persistence attempt.
        _repositoryMock.Verify(r => r.AddPlaylistAsync(It.IsAny<Playlist>()), Times.Never);
    }

    [Fact]
    public async Task GetPlaylistByIdAsync_WhenPlaylistDoesNotExist_ReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Playlist?)null);

        var result = await _sut.GetPlaylistByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task AddSongToPlaylistAsync_WhenPlaylistDoesNotExist_ReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Playlist?)null);

        var result = await _sut.AddSongToPlaylistAsync(
            Guid.NewGuid(),
            new AddSongDto("Song Title", "Some Artist", 200));

        Assert.Null(result);
        // Should never attempt to add a song to a playlist that doesn't exist.
        _repositoryMock.Verify(r => r.AddSongAsync(It.IsAny<Song>()), Times.Never);
    }

    [Fact]
    public async Task AddSongToPlaylistAsync_WithValidSong_AddsSongToExistingPlaylist()
    {
        var existingPlaylist = new Playlist { Name = "Chill Vibes", UserId = "user-123" };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(existingPlaylist.Id))
            .ReturnsAsync(existingPlaylist);

        var result = await _sut.AddSongToPlaylistAsync(
            existingPlaylist.Id,
            new AddSongDto("Weightless", "Marconi Union", 485));

        Assert.NotNull(result);
        Assert.Single(result!.Songs);
        Assert.Equal("Weightless", result.Songs[0].Title);
        _repositoryMock.Verify(r => r.AddSongAsync(It.IsAny<Song>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddSongToPlaylistAsync_WithEmptyTitle_ThrowsArgumentException()
    {
        var existingPlaylist = new Playlist { Name = "Chill Vibes", UserId = "user-123" };
        _repositoryMock
            .Setup(r => r.GetByIdAsync(existingPlaylist.Id))
            .ReturnsAsync(existingPlaylist);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.AddSongToPlaylistAsync(existingPlaylist.Id, new AddSongDto("", "Artist", null)));
    }

    [Fact]
    public async Task UpdatePlaylistAsync_WhenPlaylistExists_RenamesIt()
    {
        var playlist = new Playlist { Name = "Old Name", UserId = "user-123" };
        _repositoryMock.Setup(r => r.GetByIdAsync(playlist.Id)).ReturnsAsync(playlist);

        var result = await _sut.UpdatePlaylistAsync(playlist.Id, new UpdatePlaylistDto("New Name"));

        Assert.Equal("New Name", result!.Name);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdatePlaylistAsync_WhenPlaylistDoesNotExist_ReturnsNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Playlist?)null);

        var result = await _sut.UpdatePlaylistAsync(Guid.NewGuid(), new UpdatePlaylistDto("New Name"));

        Assert.Null(result);
    }

    [Fact]
    public async Task DeletePlaylistAsync_WhenPlaylistExists_ReturnsTrueAndRemovesIt()
    {
        var playlist = new Playlist { Name = "To Delete", UserId = "user-123" };
        _repositoryMock.Setup(r => r.GetByIdAsync(playlist.Id)).ReturnsAsync(playlist);

        var result = await _sut.DeletePlaylistAsync(playlist.Id);

        Assert.True(result);
        _repositoryMock.Verify(r => r.RemovePlaylist(playlist), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeletePlaylistAsync_WhenPlaylistDoesNotExist_ReturnsFalse()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Playlist?)null);

        var result = await _sut.DeletePlaylistAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSongAsync_WithSongFromADifferentPlaylist_ReturnsNull()
    {
        // Regression test for the ownership-check bug class: a songId
        // that's valid but belongs to a DIFFERENT playlist than the one
        // in the URL must not be deletable.
        var playlist = new Playlist { Name = "Playlist A", UserId = "user-123" };
        _repositoryMock.Setup(r => r.GetByIdAsync(playlist.Id)).ReturnsAsync(playlist);

        var songFromAnotherPlaylist = Guid.NewGuid();
        var result = await _sut.DeleteSongAsync(playlist.Id, songFromAnotherPlaylist);

        Assert.Null(result);
        _repositoryMock.Verify(r => r.RemoveSong(It.IsAny<Song>()), Times.Never);
    }
}
