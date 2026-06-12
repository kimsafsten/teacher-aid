namespace TeacherAid.Api.Services;

public interface ILLMService
{
    Task<float[]> GetEmbeddingAsync(string text);
    Task<string> GenerateAsync(string prompt);
}
