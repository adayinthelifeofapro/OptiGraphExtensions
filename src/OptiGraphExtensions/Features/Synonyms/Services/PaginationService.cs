using OptiGraphExtensions.Features.Synonyms.Services.Abstractions;

namespace OptiGraphExtensions.Features.Synonyms.Services
{
    public class PaginationService<T> : IPaginationService<T>
    {
        public PaginationResult<T> GetPage(IList<T> items, int currentPage, int pageSize)
        {
            var totalItems = items.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var startIndex = (currentPage - 1) * pageSize;
            var paginatedItems = items.Skip(startIndex).Take(pageSize).ToList();

            return new PaginationResult<T>
            {
                Items = paginatedItems,
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }
    }
}