using SupportDesk.Domain;
var tenant=Guid.Parse("11111111-1111-1111-1111-111111111111");
var owner=new Actor("requester",tenant,false); var agent=new Actor("agent",tenant,true);
var request=DeskRequest.Open(owner,Guid.NewGuid(),"Printer queue","urgent",DateTimeOffset.UnixEpoch);
if(!request.Claim(agent,1) || !request.Close(agent,2,"Driver updated") || !request.Reopen(owner,3,DateTimeOffset.UnixEpoch))
    throw new InvalidOperationException("Acceptance scenario failed.");
Console.WriteLine($"state={request.State}; version={request.Version}; reopens={request.Reopens}; dueHours={(request.DueAt-DateTimeOffset.UnixEpoch).TotalHours}");
