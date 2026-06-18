using System.Text;

namespace TeacherAid.Api.Services;

/// <summary>
/// Splits text into chunks for embedding and RAG indexing.
/// </summary>
public static class TextChunker
{
    /// <summary>
    /// Splits on newline boundaries where possible. Paragraphs longer than
    /// <paramref name="chunkSize"/> are kept intact as a single chunk.
    /// </summary>
    public static List<string> Chunk(string text, int chunkSize = 500)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var current = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (current.Length + paragraph.Length > chunkSize && current.Length > 0)
            {
                chunks.Add(current.ToString().Trim());
                current.Clear();
            }
            current.AppendLine(paragraph);
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().Trim());

        return chunks;
    }
}
