using Microsoft.EntityFrameworkCore;
using LiteratureClub.Models;

namespace LiteratureClub.Extensions
{
    public static class PaginationExtensions
    {
        public static async Task<PagedResponse<T>> ToPagedListAsync<T>(
        this IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
        {
            // 1. Hit the database once to get total count
            var totalRecords = await source.CountAsync(cancellationToken);

            // 2. Hit the database again to grab only the slice of data needed
            var items = await source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResponse<T>(items, pageNumber, pageSize, totalRecords);
        }
    }
}
