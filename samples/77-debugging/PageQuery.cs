using System.Diagnostics;
namespace SupportDesk;
public static class PageQuery
{
    public const string SourceName = "SupportDesk.Paging";
    private static readonly ActivitySource Source = new(SourceName);
    public static int[] Read(IReadOnlyList<int> tickets, int page, int size)
    {
        using var activity = Source.StartActivity("tickets.page");
        if (page < 1)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "invalid_paging");
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be positive.");
        }
        if (size is < 1 or > 100)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "invalid_paging");
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be 1..100.");
        }
        // Convert BEFORE multiplying. int multiplication silently overflows in an unchecked build.
        long offset = ((long)page - 1) * size;
        activity?.SetTag("paging.page", page);
        activity?.SetTag("paging.size", size);
        if (offset >= tickets.Count) return [];
        var result = tickets.Skip((int)offset).Take(size).ToArray();
        activity?.SetTag("paging.returned", result.Length);
        return result;
    }
}
// An isolated historical defect, never called by Read. Kept only for a reproducible debugging lab.
public static class OverflowRepro
{
    public static int[] ReadBroken(IReadOnlyList<int> tickets, int page, int size)
    {
        int offset = unchecked((page - 1) * size);
        return tickets.Skip(offset).Take(size).ToArray();
    }
}
