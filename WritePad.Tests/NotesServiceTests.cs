
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Token2Core.Data;
using WritePad.Model;
using Writepad.Model;
using WritePad.Services;

namespace WritePad.Tests;

public class NotesServiceTests
{
    private static TokenDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TokenDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TokenDbContext(options);
    }

    // CREATE SERVICE USING REPOSITORY
    private static NotesService CreateService(TokenDbContext context)
    {
        var repository = new NotesRepository(context);

        return new NotesService(
            repository,
            NullLogger<NotesService>.Instance);
    }

    // 1. CREATE NOTE
    [Fact]
    public async Task CreateNote_Should_Save_Note_With_UserId()
    {
        await using var context = CreateContext();
        var service = CreateService(context);

        var request = new CreateNoteRequest
        {
            Title = "Test Note",
            Content = "Test Content",
            Topic = "Testing"
        };

        var result = await service.CreateNoteAsync(request, 1001);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("Test Note", result.Title);
        Assert.Equal(1001, result.UserId);
        Assert.Single(context.Notes);
    }

    // 2. GET MY NOTES
    [Fact]
    public async Task GetMyNotes_Should_Return_Only_Current_User_Notes()
    {
        await using var context = CreateContext();

        context.Notes.AddRange(
            new Note
            {
                Title = "User 1 Note",
                Content = "Content 1",
                UserId = 1001,
                CreatedAt = DateTime.UtcNow
            },
            new Note
            {
                Title = "User 2 Note",
                Content = "Content 2",
                UserId = 1002,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetMyNotesAsync(1001);

        Assert.Single(result);
        Assert.All(result, note => Assert.Equal(1001, note.UserId));
    }

    // 3. GET NOTE BY ID
    [Fact]
    public async Task GetNote_Should_Return_Note_When_User_Owns_It()
    {
        await using var context = CreateContext();

        var note = new Note
        {
            Title = "My Note",
            Content = "My Content",
            UserId = 1001,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetNoteAsync(note.Id, 1001);

        Assert.NotNull(result);
        Assert.Equal(note.Id, result.Id);
    }

    // 4. GET NOTE - USER OWNERSHIP
    [Fact]
    public async Task GetNote_Should_Return_Null_When_Note_Belongs_To_Another_User()
    {
        await using var context = CreateContext();

        var note = new Note
        {
            Title = "Private Note",
            Content = "Private Content",
            UserId = 1001,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.GetNoteAsync(note.Id, 1002);

        Assert.Null(result);
    }

    // 5. UPDATE NOTE
    [Fact]
    public async Task UpdateNote_Should_Update_Note_When_User_Owns_It()
    {
        await using var context = CreateContext();

        var note = new Note
        {
            Title = "Old Title",
            Content = "Old Content",
            UserId = 1001,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new UpdateNoteRequest
        {
            Title = "New Title",
            Content = "New Content",
            Topic = "Updated"
        };

        var result = await service.UpdateNoteAsync(
            note.Id, request, 1001);

        Assert.True(result);
        Assert.Equal("New Title", note.Title);
        Assert.Equal("New Content", note.Content);
    }

    // 6. UPDATE NOTE - USER OWNERSHIP
    [Fact]
    public async Task UpdateNote_Should_Return_False_When_Note_Belongs_To_Another_User()
    {
        await using var context = CreateContext();

        var note = new Note
        {
            Title = "Original Title",
            Content = "Original Content",
            UserId = 1001,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var request = new UpdateNoteRequest
        {
            Title = "Hacked Title",
            Content = "Hacked Content"
        };

        var result = await service.UpdateNoteAsync(
            note.Id, request, 1002);

        Assert.False(result);
        Assert.Equal("Original Title", note.Title);
    }

    // 7. DELETE NOTE
    [Fact]
    public async Task DeleteNote_Should_Delete_Note_When_User_Owns_It()
    {
        await using var context = CreateContext();

        var note = new Note
        {
            Title = "Delete Me",
            Content = "Test Content",
            UserId = 1001,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.DeleteNoteAsync(note.Id, 1001);

        Assert.True(result);
        Assert.Empty(context.Notes);
    }

    // 8. DELETE NOTE - USER OWNERSHIP
    [Fact]
    public async Task DeleteNote_Should_Return_False_When_Note_Belongs_To_Another_User()
    {
        await using var context = CreateContext();

        var note = new Note
        {
            Title = "Protected Note",
            Content = "Protected Content",
            UserId = 1001,
            CreatedAt = DateTime.UtcNow
        };

        context.Notes.Add(note);
        await context.SaveChangesAsync();

        var service = CreateService(context);

        var result = await service.DeleteNoteAsync(note.Id, 1002);

        Assert.False(result);
        Assert.Single(context.Notes);
    }
}

