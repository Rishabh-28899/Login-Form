namespace WritePad.Model
{
    public class RefreshTokem
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; }
      
    }
}
