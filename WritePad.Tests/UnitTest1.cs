
using Token2Core.Validators;
using Writepad.Model;

namespace WritePad.Tests;

public class CreateNoteRequestValidatorTests
{
    private readonly CreateNoteRequestValidator _validator = new();

    // CREATE NOTE TESTS

    [Fact]
    public void Valid_Note_Should_Pass()
    {
        var request = new CreateNoteRequest
        {
            Title = "My First Note",
            Content = "This is my note content.",
            Topic = "Learning"
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_Title_Should_Fail()
    {
        var request = new CreateNoteRequest
        {
            Title = "",
            Content = "This is my note content."
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Title");
    }

    [Fact]
    public void Empty_Content_Should_Fail()
    {
        var request = new CreateNoteRequest
        {
            Title = "My First Note",
            Content = ""
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Content");
    }

    // UPDATE NOTE TESTS

    [Fact]
    public void Valid_Update_Note_Should_Pass()
    {
        var request = new UpdateNoteRequest
        {
            Title = "Updated Note",
            Content = "Updated note content.",
            Topic = "Learning"
        };

        var validator = new UpdateNoteRequestValidator();
        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_Update_Title_Should_Fail()
    {
        var request = new UpdateNoteRequest
        {
            Title = "",
            Content = "Updated note content."
        };

        var validator = new UpdateNoteRequestValidator();
        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Title");
    }

    [Fact]
    public void Empty_Update_Content_Should_Fail()
    {
        var request = new UpdateNoteRequest
        {
            Title = "Updated Note",
            Content = ""
        };

        var validator = new UpdateNoteRequestValidator();
        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Content");
    }
    // CREATE NOTE - MAXIMUM LENGTH TESTS

    [Fact]
    public void Title_Exceeding_100_Characters_Should_Fail()
    {
        var request = new CreateNoteRequest
        {
            Title = new string('A', 101),
            Content = "Valid content"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Title");
    }

    [Fact]
    public void Content_Exceeding_5000_Characters_Should_Fail()
    {
        var request = new CreateNoteRequest
        {
            Title = "Valid title",
            Content = new string('A', 5001)
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Content");
    }

    [Fact]
    public void Topic_Exceeding_50_Characters_Should_Fail()
    {
        var request = new CreateNoteRequest
        {
            Title = "Valid title",
            Content = "Valid content",
            Topic = new string('A', 51)
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Topic");
    }


    // UPDATE NOTE - MAXIMUM LENGTH TESTS

    [Fact]
    public void Update_Title_Exceeding_100_Characters_Should_Fail()
    {
        var request = new UpdateNoteRequest
        {
            Title = new string('A', 101),
            Content = "Valid content"
        };

        var validator = new UpdateNoteRequestValidator();
        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Title");
    }

    [Fact]
    public void Update_Content_Exceeding_5000_Characters_Should_Fail()
    {
        var request = new UpdateNoteRequest
        {
            Title = "Valid title",
            Content = new string('A', 5001)
        };

        var validator = new UpdateNoteRequestValidator();
        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Content");
    }

    [Fact]
    public void Update_Topic_Exceeding_50_Characters_Should_Fail()
    {
        var request = new UpdateNoteRequest
        {
            Title = "Valid title",
            Content = "Valid content",
            Topic = new string('A', 51)
        };

        var validator = new UpdateNoteRequestValidator();
        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors,
            error => error.PropertyName == "Topic");
    }
} 

