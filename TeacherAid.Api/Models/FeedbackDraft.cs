namespace TeacherAid.Api.Models
{
    public class FeedbackDraft
    {
        public int Id { get; set; }
        public int SubmissionId { get; set; }
        public string Summary { get; set; } = "";
        public string AiFeedback { get; set; } = "";
        public string AiGrade { get; set; } = "";
        public string? TeacherFeedback { get; set; }
        public string? TeacherGrade { get; set; }
        public bool Approved { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
