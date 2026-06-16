using Pgvector;

namespace TeacherAid.Api.Models
{
    public class DocumentChunk
    {
        public int Id { get; set; }
        public int CourseDocumentId { get; set; }
        public string CourseId { get; set; } = "";
        public string AssignmentId { get; set; } = "";
        public string Text { get; set; } = "";
        public Vector? Embedding { get; set; }
    }
}
