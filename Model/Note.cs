using WritePad.Model;

namespace Writepad.Model
{
    
        public class Note
        {
            public int Id { get; set; }

            public string Title { get; set; } = string.Empty;

            public string Content { get; set; } = string.Empty;

            public string? Topic { get; set; }

            public DateTime CreatedAt { get; set; }

            public DateTime? UpdatedAt { get; set; }

            // Foreign Key
            public int UserId { get; set; }

            // Navigation property
            public User User { get; set; } = null!;
        }
    }

