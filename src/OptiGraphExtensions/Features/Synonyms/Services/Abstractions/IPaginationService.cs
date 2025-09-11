namespace OptiGraphExtensions.Features.Synonyms.Services.Abstractions
{
    public interface IPaginationService<T>
    {
        PaginationResult<T> GetPage(IList<T> items, int currentPage, int pageSize);
    }

    public class PaginationResult<T>
    {
        public IList<T> Items { get; set; } = new List<T>();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}