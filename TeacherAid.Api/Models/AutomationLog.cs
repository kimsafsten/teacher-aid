namespace TeacherAid.Api.Models;

public class AutomationLog
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "pending"; // "pending" | "success" | "failed"
    public int? TokensUsed { get; set; }
    public string? ErrorMessage { get; set; }
}
