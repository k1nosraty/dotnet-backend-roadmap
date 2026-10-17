using SupportDesk.Application;
namespace SupportDesk.Infrastructure;

internal static class QueryProjection
{
    public static TicketPage Page(IEnumerable<TicketSnapshot> snapshots, TicketListQuery query)
    {
        if (!query.IsValid) throw new ArgumentException("Invalid page/filter.", nameof(query));
        string search = query.Q?.Trim() ?? "";
        var matching = snapshots.Select(s => s.Value)
            .Where(t => (query.Status is null || t.Status == query.Status) &&
                t.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).OrderBy(t => t.Id).ToArray();
        long skip = ((long)query.Page - 1) * query.PageSize;
        TicketResponse[] items = skip >= matching.Length ? [] : matching.Skip((int)skip)
            .Take(query.PageSize).Select(TicketMapping.ToResponse).ToArray();
        return new(items, matching.Length, query.Page, query.PageSize);
    }
}
