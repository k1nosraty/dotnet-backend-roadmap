internal static class AggregateChecks
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (ArgumentException) { return; }
        throw new InvalidOperationException("Expected invalid aggregate rejection.");
    }
    public static void Run()
    {
        var source = new[] { "First note" };
        var restored = SupportDesk.Domain.Ticket.Restore(1, " Network ", "InProgress", source);
        source[0] = "Changed outside";
        Check(restored.Title == "Network" && restored.Notes[0] == "First note", "defensive copy");
        try
        {
            ((System.Collections.Generic.IList<string>)restored.Notes).Add("Forbidden mutation");
            throw new InvalidOperationException("Expected readonly collection.");
        }
        catch (NotSupportedException) { }
        var withNote = restored.AddNote("Second note")!;
        Check(restored.Notes.Count == 1 && withNote.Notes.Count == 2, "immutable transition");
        Check(withNote.AddNote("  Second note  ") is null, "normalized duplicate");
        var full = restored;
        for (int i = 0; i < 4; i++) full = full.AddNote($"Note {i}")!;
        Check(full.Notes.Count == 5 && full.AddNote("Sixth note") is null, "aggregate capacity");
        Reject(() => SupportDesk.Domain.Ticket.Restore(0, "Network", "Open", []));
        Reject(() => SupportDesk.Domain.Ticket.Restore(1, "ab", "Open", []));
        Reject(() => SupportDesk.Domain.Ticket.Restore(1, "Network", "Deleted", []));
        Reject(() => SupportDesk.Domain.Ticket.Restore(1, "Network", "Closed", []));
        Reject(() => SupportDesk.Domain.Ticket.Restore(1, "Network", "Open", ["ab"]));
        Reject(() => SupportDesk.Domain.Ticket.Restore(1, "Network", "Open", [null!]));
        Reject(() => SupportDesk.Domain.Ticket.Restore(1, "Network", "Open", [" abc "]));
        Reject(() => SupportDesk.Domain.Ticket.Restore(1, "Network", "Open", ["Same", "Same"]));
        Reject(() => SupportDesk.Domain.Ticket.Restore(1, "Network", "Open", Enumerable.Range(0,6).Select(i => $"Note {i}")));
        var closed = SupportDesk.Domain.Ticket.Restore(1, "Network", "Closed", ["Fixed cable"]);
        Check(closed.Rename(SupportDesk.Domain.TicketTitle.Create("New title")) is null &&
              closed.AddNote("New note") is null, "restored state obeys original rules");
        Console.WriteLine("PASS: aggregate invariants and rehydration");
    }
}
