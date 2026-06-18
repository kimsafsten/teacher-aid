namespace TeacherAid.Api.Services;

/// <summary>
/// Replaces student name occurrences in text with a neutral placeholder.
/// Full name is replaced before individual parts to avoid partial matches.
/// </summary>
public static class TextPseudonymizer
{
    private const string Placeholder = "[Student]";
    private const int MinPartLength = 3;

    /// <summary>
    /// Replaces the full name and name parts (≥ <see cref="MinPartLength"/> chars), case-insensitively.
    /// </summary>
    public static string Pseudonymize(string text, string studentName)
    {
        if (string.IsNullOrWhiteSpace(studentName)) return text;

        var result = text.Replace(studentName, Placeholder, StringComparison.OrdinalIgnoreCase);

        foreach (var part in studentName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                     .Where(p => p.Length >= MinPartLength))
        {
            result = result.Replace(part, Placeholder, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
