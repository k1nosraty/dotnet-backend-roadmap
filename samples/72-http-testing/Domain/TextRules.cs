using System.Text;
namespace SupportDesk.Domain;
public static class TextRules
{
    // Unicode scalar values, matching PostgreSQL char_length for valid Unicode text.
    public static bool HasLength(string value, int maximum)
    { int length = value.EnumerateRunes().Count(); return length >= 3 && length <= maximum; }
}
