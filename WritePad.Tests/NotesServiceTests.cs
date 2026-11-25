
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Token2Core.Data;
using WritePad.Model;
using Writepad.Model;
using WritePad.Services;

namespace WritePad.Tests;

public class NotesServiceTests
{
    // CREATE IN-MEMORY DATABASE
    private static TokenDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TokenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TokenDbContext(options);
    }

    // CREATE SERVICE USING UNIT OF WORK
    private static NotesService CreateService(TokenDbContext context)
    {
        var repository = new NotesRepository(context);

        var unitOfWork = new UnitOfWork(
            context,
            repository);

        return new NotesService(
            unitOfWork,
            NullLogger<NotesService>.Instance);
    }

    // TEST 1: CREATE NOTE
    [Fact]
    public async Task CreateNoteAsync_ShouldCreateNote()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var request = new CreateNoteRequest
        {
            Title = "Test Title",
            Content = "Test Content",
            Topic = "Testing"
        };

        var result = await service.CreateNoteAsync(request, 1);

        Assert.NotNull(result);
        Assert.Equal("Test Title", result.Title);
        Assert.Equal("Test Content", result.Content);
        Assert.Equal(1, result.UserId);
        Assert.Single(context.Notes);
    }

    // TEST 2: GET NOTES BY USER
    [Fact]
    public async Task GetMyNotesAsync_ShouldReturnOnlyCurrentUserNotes()
    {
        using var context = CreateContext();

        context.Notes.AddRange(
            new Note
            {
                Title = "User 1 Note",
                Content = "Content 1",
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            },
            new Note
            {
                Title = "User 2 Note",
                Content = "Content 2",
                UserId = 2,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetMyNotesAsync(1);

        Assert.Single(result);
        Assert.Equal("User 1 Note", result[0].Title);
        Assert.Equal(1, result[0].UserId);
    }

    // TEST 3: GET NOTE BY ID AND USER
    [Fact]
    public async Task GetNoteAsync_ShouldReturnNoteForCorrectUser()
    {
        using var context = CreateContext();

        var note = new Note
        {
            Title = "My Note",
            Content = "My Content",
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetNoteAsync(note.Id, 1);

        Assert.NotNull(result);
        Assert.Equal("My Note", result.Title);
    }

    // TEST 4: GET NOTE FOR WRONG USER
    [Fact]
    public async Task GetNoteAsync_ShouldReturnNullForWrongUser()
    {
        using var context = CreateContext();

        var note = new Note
        {
            Title = "My Note",
            Content = "My Content",
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetNoteAsync(note.Id, 2);

        Assert.Null(result);
    }

    // TEST 5: UPDATE NOTE
    [Fact]
    public async Task UpdateNoteAsync_ShouldUpdateNote()
    {
        using var context = CreateContext();

        var note = new Note
        {
            Title = "Old Title",
            Content = "Old Content",
            Topic = "Old Topic",
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new UpdateNoteRequest
        {
            Title = "New Title",
            Content = "New Content",
            Topic = "New Topic"
        };

        var result = await service.UpdateNoteAsync(note.Id, request, 1);

        Assert.True(result);

        var updatedNote = await context.Notes.FindAsync(note.Id);

        Assert.NotNull(updatedNote);
        Assert.Equal("New Title", updatedNote.Title);
        Assert.Equal("New Content", updatedNote.Content);
        Assert.Equal("New Topic", updatedNote.Topic);
    }

    // TEST 6: UPDATE NOTE FOR WRONG USER
    [Fact]
    public async Task UpdateNoteAsync_ShouldReturnFalseForWrongUser()
    {
        using var context = CreateContext();

        var note = new Note
        {
            Title = "Old Title",
            Content = "Old Content",
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new UpdateNoteRequest
        {
            Title = "New Title",
            Content = "New Content",
            Topic = "New Topic"
        };

        var result = await service.UpdateNoteAsync(note.Id, request, 2);

        Assert.False(result);

        var unchangedNote = await context.Notes.FindAsync(note.Id);

        Assert.NotNull(unchangedNote);
        Assert.Equal("Old Title", unchangedNote.Title);
    }

    // TEST 7: DELETE NOTE
    [Fact]
    public async Task DeleteNoteAsync_ShouldDeleteNote()
    {
        using var context = CreateContext();

        var note = new Note
        {
            Title = "Delete Me",
            Content = "Content",
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.DeleteNoteAsync(note.Id, 1);

        Assert.True(result);
        Assert.Empty(context.Notes);
    }

    // TEST 8: DELETE NOTE FOR WRONG USER
    [Fact]
    public async Task DeleteNoteAsync_ShouldReturnFalseForWrongUser()
    {
        using var context = CreateContext();

        var note = new Note
        {
            Title = "Do Not Delete",
            Content = "Content",
            UserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.DeleteNoteAsync(note.Id, 2);

        Assert.False(result);
        Assert.Single(context.Notes);
    }
}
