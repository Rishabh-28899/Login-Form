
namespace WritePad.DTOs
{
    public class PagedResponse<T>
    {
        public List<T> Items { get; set; } = new();

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalRecords { get; set; }

        public int TotalPages { get; set; }

        public PagedResponse(
            List<T> items,
            int pageNumber,
            int pageSize,
            int totalRecords)
        {
            Items = items;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalRecords = totalRecords;

            TotalPages = (int)Math.Ceiling(
                (double)totalRecords / pageSize);
        }
    }
}
