using SupportDesk.Domain;
namespace SupportDesk.Application;

public interface ITicketWriter { int Add(string title); }
public interface ITicketReader { Ticket? FindById(int id); }
public interface ITicketFormatter
{
    // Must format a valid ticket without changing it.
    string Format(Ticket ticket);
}
