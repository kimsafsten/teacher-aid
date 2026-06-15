namespace TeacherAid.Api.Services;

/// <summary>
/// Parses structured metadata from submission file names.
/// Extracted from FolderSyncService to allow unit testing without infrastructure dependencies.
/// </summary>
public static class SubmissionFileNameParser
{
    /// <summary>
    /// Parses a submission file name in the format
    /// <c>Firstname_Lastname_CourseId_Assignment.ext</c> into its constituent parts.
    /// </summary>
    /// <param name="fileName">The file name (with or without extension).</param>
    /// <returns>
    /// A tuple where <c>studentName</c> is "Firstname Lastname" (or just the first segment
    /// if fewer than two segments exist) and <c>courseId</c> is the third underscore-delimited
    /// segment, or "okänd" if fewer than three segments exist.
    /// </returns>
    public static (string studentName, string courseId) Parse(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var parts = name.Split('_');

        var studentName = parts.Length >= 2
            ? $"{parts[0]} {parts[1]}"
            : parts[0];

        var courseId = parts.Length >= 3 ? parts[2] : "okänd";

        return (studentName, courseId);
    }

    /// <summary>
    /// Extracts the course ID from a course material file name in the format
    /// <c>DocumentName_CourseId.ext</c>.
    /// </summary>
    /// <param name="fileName">The file name (with or without extension).</param>
    /// <returns>
    /// The second underscore-delimited segment, or <c>null</c> if the file name
    /// contains fewer than two segments.
    /// </returns>
    public static string? ParseCourseId(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var parts = name.Split('_');
        return parts.Length >= 2 ? parts[1] : null;
    }
}
