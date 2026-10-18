using SupportDesk.Domain;
using Xunit;
namespace SupportDesk.Tests;
public sealed class DomainTests
{
    [Theory]
    [InlineData(null)] [InlineData("")] [InlineData("  ")]
    public void Rejects_missing_title(string? value) => Assert.Throws<ArgumentException>(() => TicketTitle.Create(value));
    [Fact]
    public void Rejects_too_short_title() => Assert.Throws<ArgumentException>(() => TicketTitle.Create("ab"));
    [Fact]
    public void Rejects_title_above_upper_boundary() => Assert.Throws<ArgumentException>(() => TicketTitle.Create(new string('x', 121)));
    [Theory] [InlineData(3)] [InlineData(120)]
    public void Accepts_exact_title_boundaries(int length) => Assert.Equal(new string('x', length), TicketTitle.Create(new string('x', length)).Value);
    [Fact]
    public void Trims_and_preserves_Persian_title() => Assert.Equal("اختلال شبکه", TicketTitle.Create("  اختلال شبکه  ").Value);
    [Fact]
    public void Title_boundary_counts_Unicode_scalars_consistently()
    {
        Assert.Throws<ArgumentException>(() => TicketTitle.Create("😀😀"));
        Assert.Equal("😀😀😀", TicketTitle.Create("😀😀😀").Value);
    }
    [Fact]
    public void Rejects_empty_identity() => Assert.Throws<ArgumentException>(() => Ticket.Open(Guid.Empty, TicketTitle.Create("Network")));
    [Fact]
    public void Resolve_requires_start_and_does_not_mutate_original()
    {
        var opened = Ticket.Open(Guid.NewGuid(), TicketTitle.Create("Network"));
        Assert.Throws<InvalidOperationException>(() => opened.Resolve("Cable fixed"));
        var started = opened.Start();
        var closed = started.Resolve("  Cable fixed  ");
        Assert.Equal(TicketStatus.Open, opened.Status);
        Assert.Equal(TicketStatus.InProgress, started.Status);
        Assert.Equal(TicketStatus.Closed, closed.Status);
        Assert.Equal("Cable fixed", closed.Resolution);
        Assert.Equal(opened.Id, closed.Id);
        Assert.Throws<InvalidOperationException>(() => closed.Start());
        Assert.Throws<InvalidOperationException>(() => closed.Resolve("Again"));
    }
    [Theory] [InlineData(null)] [InlineData("")] [InlineData("ab")]
    public void Rejects_invalid_resolution_without_closing(string? resolution)
    {
        var ticket = Ticket.Open(Guid.NewGuid(), TicketTitle.Create("Network")).Start();
        Assert.Throws<ArgumentException>(() => ticket.Resolve(resolution));
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Null(ticket.Resolution);
    }
    [Theory] [InlineData(3)] [InlineData(200)]
    public void Accepts_resolution_boundaries(int length)
    {
        var ticket = Ticket.Open(Guid.NewGuid(), TicketTitle.Create("Network")).Start();
        Assert.Equal(new string('x', length), ticket.Resolve(new string('x', length)).Resolution);
    }
    [Fact]
    public void Rejects_resolution_above_upper_boundary()
    {
        var ticket = Ticket.Open(Guid.NewGuid(), TicketTitle.Create("Network")).Start();
        Assert.Throws<ArgumentException>(() => ticket.Resolve(new string('x', 201)));
    }
}
