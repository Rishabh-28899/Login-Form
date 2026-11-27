namespace WritePad.DTOs
{
    public class AdminDashboardResponseDto
    {
        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int RegularUsers { get; set; }
        public int TotalNotes { get; set; }
    }
}