namespace TeacherAid.Api.DTO
{
    public record CreateSubmissionDto(
        string StudentName, 
        string CourseId, 
        string Content);
}
