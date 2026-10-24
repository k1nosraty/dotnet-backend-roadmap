using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SupportDesk;
using Xunit;
public sealed class PerformanceTests
{
    [Theory]
    [InlineData(1,1L)] [InlineData(3,14L)] [InlineData(100,338350L)] [InlineData(1000000,333333833333500000L)]
    public void Both_algorithms_keep_the_same_exact_result(int count,long expected)
    { Assert.Equal(expected,Squares.Loop(count)); Assert.Equal(expected,Squares.Formula(count)); }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(1000001)]
    public void Invalid_size_is_rejected_before_expensive_work(int count)
    { Assert.Throws<ArgumentOutOfRangeException>(()=>Squares.Loop(count)); Assert.Throws<ArgumentOutOfRangeException>(()=>Squares.Formula(count)); }
    [Fact]
    public async Task Http_contract_rejects_unknown_algorithm_and_returns_the_known_value()
    {
        using var factory=new WebApplicationFactory<Program>(); using var client=factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/sum/unknown?count=3")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/sum/loop?count=1000001")).StatusCode);
        var response=await client.GetFromJsonAsync<Value>("/sum/formula?count=3"); Assert.Equal(14,response!.value);
    }
    private sealed record Value(long value);
}
