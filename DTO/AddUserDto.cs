
namespace WritePad.DTOs
{
    public class AddUserDto
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "User";
    }

    public class NoteResponseDto
    {
        public int NoteId { get; set; }

        public int UserId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Topic { get; set; }
    }
}
