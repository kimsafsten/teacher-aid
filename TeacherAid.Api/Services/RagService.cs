using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Text;
using TeacherAid.Api.Data;
using TeacherAid.Api.Models;

namespace TeacherAid.Api.Services;

/// <summary>
/// Provides retrieval-augmented generation (RAG) for course materials: indexes document text
/// into embedded chunks and answers student questions using the most relevant stored chunks.
/// </summary>
public class RagService
{
    private readonly AppDbContext _db;
    private readonly ILLMService _llm;

    /// <summary>
    /// Creates a RagService with the given database context and LLM backend.
    /// </summary>
    /// <param name="db">The database context used to persist documents, chunks, and embeddings.</param>
    /// <param name="llm">The LLM service used to generate embeddings and completion responses.</param>
    public RagService(AppDbContext db, ILLMService llm)
    {
        _db = db;
        _llm = llm;
    }

    /// <summary>
    /// Splits a given text into chunks of specified maximum size, attempting to minimize splitting mid-paragraph.
    /// Text is first split by newline into paragraphs. Paragraphs are concatenated into chunks until the maximum chunk size is reached.
    /// </summary>
    /// <param name="text">The input text to be chunked.</param>
    /// <param name="chunkSize">The maximum allowed character length for each chunk. Defaults to 500.</param>
    /// <returns>A list of string chunks, each approximately <paramref name="chunkSize"/> characters long. A single paragraph exceeding <paramref name="chunkSize"/> will be kept intact and produce a chunk larger than the limit.</returns>
    /// <remarks>
    /// Edge cases:
    /// <list type="bullet">
    ///   <item>If a single paragraph is longer than <paramref name="chunkSize"/>, the entire paragraph is placed in a chunk that may exceed <paramref name="chunkSize"/>.</item>
    ///   <item>Empty or whitespace-only input returns an empty list.</item>
    /// </list>
    /// </remarks>
    private List<string> ChunkText(string text, int chunkSize = 500)
    {
        var chunks = new List<string>();
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

    /// <summary>
    /// Indexes a document for a specified course by saving its metadata and content,
    /// splitting its content into manageable chunks, generating embeddings for each chunk,
    /// and storing the resulting chunk/embedding pairs in the database.
    /// </summary>
    /// <param name="courseId">The unique identifier for the course to which the document belongs.</param>
    /// <param name="fileName">The name of the document being indexed.</param>
    /// <param name="content">The full textual content of the document to split and embed.</param>
    /// <param name="sourceFileName">The optional source file name, if different from the primary file name.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Edge cases:
    /// <list type="bullet">
    ///   <item>If <paramref name="content"/> is empty or whitespace, the method will still create a CourseDocument entry but no chunks will be added.</item>
    ///   <item>If a chunk from <paramref name="content"/> is unusually large, the method relies on <c>ChunkText</c> to handle and store it as-is.</item>
    ///   <item>Database access and LLM embedding calls are async and may throw if unavailable.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="DbUpdateException">Thrown if saving to the database fails.</exception>
    /// <exception cref="HttpRequestException">Thrown if the LLM service is unreachable when generating embeddings.</exception>
    public async Task IndexDocument(string courseId, string fileName, string content, string? sourceFileName = null)
    {
        var doc = new CourseDocument
        {
            CourseId = courseId,
            FileName = fileName,
            Content = content,
            SourceFileName = sourceFileName
        };
        _db.CourseDocuments.Add(doc);
        await _db.SaveChangesAsync();

        var chunks = ChunkText(content);
        foreach (var chunk in chunks)
        {
            var floats = await _llm.GetEmbeddingAsync(chunk);
            _db.DocumentChunks.Add(new DocumentChunk
            {
                CourseDocumentId = doc.Id,
                CourseId = courseId,
                Text = chunk,
                Embedding = new Vector(floats)
            });
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Answers a question for a course by retrieving the most semantically similar document chunks
    /// and passing them as context to the language model.
    /// </summary>
    /// <param name="courseId">The unique identifier of the course whose indexed material should be searched.</param>
    /// <param name="question">The student's question to embed and answer.</param>
    /// <returns>
    /// A Swedish answer generated from the retrieved course material, or a fixed message when no
    /// indexed chunks exist for the course.
    /// </returns>
    /// <remarks>
    /// Edge cases:
    /// <list type="bullet">
    ///   <item>If no chunks are indexed for <paramref name="courseId"/>, returns <c>"Jag hittade inget relevant kursmaterial för den frågan."</c> without calling the LLM for generation.</item>
    ///   <item>Only the three nearest chunks by L2 distance are included in the prompt, regardless of absolute similarity.</item>
    ///   <item>Chunks with a null <see cref="DocumentChunk.Embedding"/> may affect ordering or distance calculation.</item>
    ///   <item>LLM embedding and generation calls are async and may throw if the backing service is unavailable.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="HttpRequestException">Thrown if the LLM service is unreachable when generating the embedding or the answer.</exception>
    public async Task<string> AskQuestion(string courseId, string question)
    {
        var floats = await _llm.GetEmbeddingAsync(question);
        var questionEmbedding = new Vector(floats);

        var relevantChunks = await _db.DocumentChunks
            .Where(c => c.CourseId == courseId)
            .OrderBy(c => c.Embedding!.L2Distance(questionEmbedding))
            .Take(3)
            .ToListAsync();

        if (!relevantChunks.Any())
            return "Jag hittade inget relevant kursmaterial för den frågan.";

        var context = string.Join("\n\n", relevantChunks.Select(c => c.Text));

        var prompt = $"""
            Du är en hjälpsam kursassistent för läraren Anna Lindqvist på Yrkesakademin.
            Svara på studentens fråga baserat ENDAST på kursmaterialet nedan.
            Om svaret inte finns i materialet, säg det istället för att gissa.
            Svara på svenska och håll svaret kort och tydligt.

            Kursmaterial:
            {context}

            Studentens fråga: {question}
            """;

        return await _llm.GenerateAsync(prompt);
    }
}
