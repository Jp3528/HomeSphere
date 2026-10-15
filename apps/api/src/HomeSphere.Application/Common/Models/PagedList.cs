using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Common.Models;

public class PagedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PagedList(IReadOnlyList<T> items, int count, int pageNumber, int pageSize)
    {
        TotalCount = count;
        PageSize = pageSize > 0 ? pageSize : 10;
        PageNumber = pageNumber > 0 ? pageNumber : 1;
        Items = items;
    }

    public static PagedList<T> Create(IEnumerable<T> source, int pageNumber, int pageSize)
    {
        var count = source.Count();
        var page = pageNumber > 0 ? pageNumber : 1;
        var size = pageSize > 0 ? pageSize : 10;
        var items = source.Skip((page - 1) * size).Take(size).ToList();
        return new PagedList<T>(items, count, page, size);
    }

    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var page = pageNumber > 0 ? pageNumber : 1;
        var size = pageSize > 0 ? pageSize : 10;
        var items = await source.Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new PagedList<T>(items, count, page, size);
    }
}
