namespace TeacherAid.Api.Models
{
    public class CourseDocument
    {
        public int Id { get; set; }
        public string CourseId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public List<DocumentChunk> Chunks { get; set; } = new();
    }
}
