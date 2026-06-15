namespace TeacherAid.Api.Services;

/// <summary>
/// Replaces student name occurrences in text with a neutral placeholder.
/// Extracted from FolderSyncService to allow unit testing without infrastructure dependencies.
/// </summary>
public static class TextPseudonymizer
{
    private const string Placeholder = "[Student]";

    /// <summary>
    /// Replaces all occurrences of the student's full name, first name, and last name
    /// (each longer than 2 characters) with <c>[Student]</c>. Replacement is case-insensitive.
    /// </summary>
    /// <param name="text">The text to pseudonymize.</param>
    /// <param name="studentName">Full name in "Firstname Lastname" format.</param>
    /// <returns>
    /// The input text with name occurrences replaced. Returns the original text unchanged
    /// if <paramref name="studentName"/> is null or whitespace.
    /// </returns>
    public static string Pseudonymize(string text, string studentName)
    {
        if (string.IsNullOrWhiteSpace(studentName)) return text;

        // Replace full name first (most specific match)
        var result = text.Replace(studentName, Placeholder, StringComparison.OrdinalIgnoreCase);

        // Replace individual name parts longer than 2 characters
        var parts = studentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts.Where(p => p.Length > 2))
            result = result.Replace(part, Placeholder, StringComparison.OrdinalIgnoreCase);

        return result;
    }
}
