using SupportDesk.Domain;
using Xunit;
public sealed class DomainTests
{
    private static readonly Guid Tenant=Guid.NewGuid();
    private static readonly Actor Owner=new("owner",Tenant,false),Agent=new("agent",Tenant,true);
    [Theory]
    [InlineData("normal",24)] [InlineData("urgent",4)]
    public void Priority_sets_server_owned_due_date(string priority,int hours)
    { var request=DeskRequest.Open(Owner,Guid.NewGuid(),"Printer",priority,DateTimeOffset.UnixEpoch); Assert.Equal(hours,(request.DueAt-DateTimeOffset.UnixEpoch).TotalHours); }
    [Theory]
    [InlineData("ab")] [InlineData(" ")] [InlineData(null)]
    public void Invalid_title_is_rejected(string? title)=>Assert.Throws<ArgumentException>(()=>DeskRequest.Open(Owner,Guid.NewGuid(),title,"normal",DateTimeOffset.UnixEpoch));
    [Fact]
    public void Unicode_scalar_boundary_and_unknown_priority_are_explicit()
    {
        Assert.True(DeskRequest.ValidText(string.Concat(Enumerable.Repeat("🙂",120)),120));
        Assert.False(DeskRequest.ValidText(new string('a',121),120));
        Assert.Throws<ArgumentException>(()=>DeskRequest.Open(Owner,Guid.NewGuid(),"Printer","critical",DateTimeOffset.UnixEpoch));
    }
    [Fact]
    public void Owner_may_reopen_only_once_and_cannot_claim()
    {
        var request=DeskRequest.Open(Owner,Guid.NewGuid(),"Printer","normal",DateTimeOffset.UnixEpoch);
        Assert.False(request.Claim(Owner,1)); Assert.True(request.Claim(Agent,1));
        Assert.False(request.Close(Agent,1,"Fixed")); Assert.False(request.Close(Agent,2,"no"));
        Assert.True(request.Close(Agent,2,"Fixed")); Assert.True(request.Reopen(Owner,3,DateTimeOffset.UnixEpoch));
        Assert.True(request.Claim(Agent,4)); Assert.True(request.Close(Agent,5,"Fixed again"));
        Assert.False(request.Reopen(Owner,6,DateTimeOffset.UnixEpoch)); Assert.Equal(6,request.Version);
    }
    [Fact]
    public void Cross_tenant_agent_and_other_requester_cannot_read_or_transition()
    {
        var request=DeskRequest.Open(Owner,Guid.NewGuid(),"Printer","normal",DateTimeOffset.UnixEpoch);
        var other=new Actor("other",Guid.NewGuid(),true); var peer=new Actor("peer",Tenant,false);
        Assert.False(request.Visible(other)); Assert.False(request.Visible(peer)); Assert.False(request.Claim(other,1));
    }
}
