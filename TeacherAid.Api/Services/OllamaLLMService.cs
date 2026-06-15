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

    // Sanitize AI output before returning it to callers.
    // Prevents control characters and null bytes from crashing downstream
    // consumers (JSON parsers, PostgreSQL) and caps response length.
    private static string Sanitize(string text)
    {
        // Remove control characters except newline and tab
        text = new string(text.Where(c => c >= 32 || c == '\n' || c == '\t').ToArray());

        // Remove null bytes — PostgreSQL throws on \0 in text columns
        text = text.Replace("\0", string.Empty);

        // Cap length to prevent runaway responses from filling the database or hanging the UI
        if (text.Length > MaxResponseLength)
            text = text[..MaxResponseLength] + "\n\n[Response truncated]";

        return text.Trim();
    }
}
