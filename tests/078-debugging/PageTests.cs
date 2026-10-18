using System.Diagnostics;
using SupportDesk;
using Xunit;

public sealed class PageTests
{
    [Fact]
    public void Historical_repro_returns_first_page_instead_of_empty()
    {
        int[] tickets = [101, 102, 103];
        Assert.Equal(new[] {101, 102}, OverflowRepro.ReadBroken(tickets, int.MaxValue, 2));
        Assert.Empty(PageQuery.Read(tickets, int.MaxValue, 2));
    }
    [Theory]
    [InlineData(1, 2, new[] {101, 102})]
    [InlineData(2, 2, new[] {103})]
    [InlineData(3, 2, new int[] {})]
    public void Normal_pages_keep_their_contract(int page, int size, int[] expected) =>
        Assert.Equal(expected, PageQuery.Read(new[] {101, 102, 103}, page, size));
    [Theory]
    [InlineData(0, 2)] [InlineData(-1, 2)] [InlineData(1, 0)] [InlineData(1, 101)]
    public void Invalid_paging_is_rejected(int page, int size) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PageQuery.Read(Array.Empty<int>(), page, size));
    [Fact]
    public void Child_span_keeps_parent_trace_and_records_safe_numeric_tags()
    {
        Activity? captured = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == PageQuery.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => captured = activity
        };
        ActivitySource.AddActivityListener(listener);
        using var parent = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
        PageQuery.Read(new[] {101, 102}, 1, 2);
        Assert.NotNull(captured);
        Assert.Equal(parent.TraceId, captured.TraceId);
        Assert.Equal(parent.SpanId, captured.ParentSpanId);
        Assert.Equal(1, captured.GetTagItem("paging.page"));
        Assert.Equal(2, captured.GetTagItem("paging.returned"));
        Assert.DoesNotContain(captured.TagObjects, tag => tag.Key.Contains("body"));
    }
}
