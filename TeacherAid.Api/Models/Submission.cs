namespace TeacherAid.Api.Models
{
    public class Submission
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public FeedbackDraft? FeedbackDraft { get; set; }
    }
}
