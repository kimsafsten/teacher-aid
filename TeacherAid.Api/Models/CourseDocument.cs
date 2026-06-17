namespace TeacherAid.Api.Models
{
    public enum DocumentType
    {
        Kursmaterial,
        Uppgiftsbeskrivning,
        Bedömningsmall
    }

    public class CourseDocument
    {
        public int Id { get; set; }
        public string CourseId { get; set; } = "";
        public string AssignmentId { get; set; } = "";
        public DocumentType DocumentType { get; set; } = DocumentType.Kursmaterial;
        public string FileName { get; set; } = "";
        public string Content { get; set; } = "";
        public string? SourceFileName { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public List<DocumentChunk> Chunks { get; set; } = new();
    }
}
