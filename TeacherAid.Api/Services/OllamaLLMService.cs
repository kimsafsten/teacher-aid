using System.Text.Json;

namespace TeacherAid.Api.Services;

public class OllamaLLMService : ILLMService
{
    private readonly IHttpClientFactory _http;
    private const string OllamaUrl = "http://localhost:11434";
    private const int MaxResponseLength = 8000;

    public OllamaLLMService(IHttpClientFactory http)
    {
        _http = http;
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var client = _http.CreateClient();
        var response = await client.PostAsJsonAsync($"{OllamaUrl}/api/embed", new
        {
            model = "nomic-embed-text",
            input = text
        });

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("embeddings")[0]
            .EnumerateArray()
            .Select(e => e.GetSingle())
            .ToArray();
    }

    public async Task<string> GenerateAsync(string prompt)
    {
        var client = _http.CreateClient("ollama");
        var response = await client.PostAsJsonAsync($"{OllamaUrl}/api/generate", new
        {
            model = "llama3",
            prompt,
            stream = false
        });

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var raw = doc.RootElement.GetProperty("response").GetString()
                  ?? "No response generated.";

        return Sanitize(raw);
    }

    /// <summary>
    /// Strips control characters and caps length so output is safe for PostgreSQL and the UI.
    /// </summary>
    private static string Sanitize(string text)
    {
        text = new string(text.Where(c => c >= 32 || c == '\n' || c == '\t').ToArray());

        if (text.Length > MaxResponseLength)
            text = text[..MaxResponseLength] + "\n\n[Response truncated]";

        return text.Trim();
    }
}
