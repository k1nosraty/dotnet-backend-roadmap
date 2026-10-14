using SupportDesk.Domain;

Ticket ticket = new(1, "  Network issue  ");
IReadOnlyList<string> liveView = ticket.Notes;

try { ticket.Rename("ab"); }
catch (ArgumentException) { Console.WriteLine($"Rejected rename; title: {ticket.Title}"); }
try { ticket.AddNote(" "); }
catch (ArgumentException) { Console.WriteLine($"Rejected note; count: {ticket.Notes.Count}"); }
try { ticket.Close(); }
catch (InvalidOperationException) { Console.WriteLine($"Rejected early close; status: {ticket.Status}"); }

ticket.Start();
try { ticket.Close(); }
catch (InvalidOperationException) { Console.WriteLine($"Rejected close without note; status: {ticket.Status}"); }
ticket.AddNote("  Router configuration corrected  ");
Console.WriteLine($"Live view count: {liveView.Count}");

// ToList makes a separate list; these immutable string values need no deep clone.
List<string> copy = ticket.Notes.ToList();
copy.Add("Only added to the copy");
Console.WriteLine($"Copy count: {copy.Count}; actual count: {ticket.Notes.Count}");

// The exposed object is a read-only wrapper, not the private List.
try { ((ICollection<string>)ticket.Notes).Add("Bypass attempt"); }
catch (NotSupportedException) { Console.WriteLine("Wrapper rejected external mutation."); }

ticket.Close();
try { ticket.AddNote("Late note"); }
catch (InvalidOperationException) { Console.WriteLine("Rejected edit after close."); }
try { ticket.Rename("Another title"); }
catch (InvalidOperationException) { Console.WriteLine("Rejected rename after close."); }
Console.WriteLine($"Final: {ticket.Status}; notes: {ticket.Notes.Count}; title: {ticket.Title}");
