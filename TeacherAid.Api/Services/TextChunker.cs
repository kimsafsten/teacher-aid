using System.Text;

namespace TeacherAid.Api.Services;

/// <summary>
/// Splits text into manageable chunks for embedding and RAG indexing.
/// Extracted from RagService to allow unit testing without infrastructure dependencies.
/// </summary>
public static class TextChunker
{
    /// <summary>
    /// Splits <paramref name="text"/> into chunks no larger than <paramref name="chunkSize"/>
    /// characters, splitting on newline boundaries where possible.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="chunkSize">Maximum characters per chunk (default 500).</param>
    /// <returns>
    /// A list of trimmed string chunks. Returns an empty list for null, empty,
    /// or whitespace-only input. A single paragraph that exceeds <paramref name="chunkSize"/>
    /// is kept intact and produces a chunk larger than the limit.
    /// </returns>
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
