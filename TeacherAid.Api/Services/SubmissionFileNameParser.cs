namespace TeacherAid.Api.Services;

/// <summary>
/// Parses structured metadata from submission and course-material file names.
/// </summary>
public static class SubmissionFileNameParser
{
    /// <summary>User-facing fallback when the course ID segment is missing.</summary>
    public const string UnknownCourseId = "okänd";

    /// <summary>
    /// Parses <c>Firstname_Lastname_CourseId_Assignment.ext</c> into student name and course ID.
    /// </summary>
    public static (string studentName, string courseId) Parse(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var parts = name.Split('_');

        var studentName = parts.Length >= 2
            ? $"{parts[0]} {parts[1]}"
            : parts[0];

        var courseId = parts.Length >= 3 ? parts[2] : UnknownCourseId;

        return (studentName, courseId);
    }

    /// <summary>
    /// Extracts the course ID from <c>DocumentName_CourseId.ext</c>.
    /// </summary>
    public static string? ParseCourseId(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var parts = name.Split('_');
        return parts.Length >= 2 ? parts[1] : null;
    }
}
