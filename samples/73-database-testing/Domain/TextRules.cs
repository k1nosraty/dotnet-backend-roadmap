using System.Text;
namespace SupportDesk.Domain;
public static class TextRules
{
    // Unicode scalar values, matching PostgreSQL char_length for valid Unicode text.
    // PostgreSQL text cannot store NUL, regardless of length.
    public static bool HasLength(string value, int maximum)
    {
        if (value.Contains('\0')) return false;
        int length = value.EnumerateRunes().Count();
        return length >= 3 && length <= maximum;
    }
}
