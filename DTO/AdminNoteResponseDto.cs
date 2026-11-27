namespace WritePad.DTOs
{
    public class AdminNoteResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }
}