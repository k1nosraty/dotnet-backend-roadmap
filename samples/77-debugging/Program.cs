using System.Diagnostics;
using SupportDesk;
using var listener = new ActivityListener
{
    ShouldListenTo = source => source.Name == PageQuery.SourceName,
    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
    ActivityStopped = activity => Console.WriteLine($"trace={activity.TraceId}; span={activity.SpanId}; operation={activity.OperationName}")
};
ActivitySource.AddActivityListener(listener);
int[] tickets = [101, 102, 103];
const int page = int.MaxValue;
const int size = 2;
Console.WriteLine($"broken=[{string.Join(',', OverflowRepro.ReadBroken(tickets, page, size))}]");
var result = PageQuery.Read(tickets, page, size);
Console.WriteLine($"fixed=[{string.Join(',', result)}]");
