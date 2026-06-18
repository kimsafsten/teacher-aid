using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
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
    /// Indexes a document for a specified course by saving its metadata and content,
    /// splitting its content into manageable chunks, generating embeddings for each chunk,
    /// and storing the resulting chunk/embedding pairs in the database.
    /// </summary>
    /// <param name="courseId">The unique identifier for the course to which the document belongs.</param>
    /// <param name="fileName">The name of the document being indexed.</param>
    /// <param name="content">The full textual content of the document to split and embed.</param>
    /// <param name="sourceFileName">The optional source file name, if different from the primary file name.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="DbUpdateException">Thrown if saving to the database fails.</exception>
    /// <exception cref="HttpRequestException">Thrown if the LLM service is unreachable when generating embeddings.</exception>
    public async Task IndexDocument(
        string courseId,
        string assignmentId,
        DocumentType documentType,
        string fileName,
        string content,
        string? sourceFileName = null)
    {
        var doc = new CourseDocument
        {
            CourseId = courseId,
            AssignmentId = assignmentId,
            DocumentType = documentType,
            FileName = fileName,
            Content = content,
            SourceFileName = sourceFileName
        };
        _db.CourseDocuments.Add(doc);
        await _db.SaveChangesAsync();

        var chunks = TextChunker.Chunk(content);
        foreach (var chunk in chunks)
        {
            var floats = await _llm.GetEmbeddingAsync(chunk);
            _db.DocumentChunks.Add(new DocumentChunk
            {
                CourseDocumentId = doc.Id,
                CourseId = courseId,
                AssignmentId = assignmentId,
                Text = chunk,
                Embedding = new Vector(floats)
            });
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Loads the assignment description and grading rubric for a specific assignment
    /// to use as context when generating feedback.
    /// </summary>
    public async Task<AssignmentContext> GetAssignmentContext(string courseId, string assignmentId)
    {
        var docs = await _db.CourseDocuments
            .Where(d => d.CourseId == courseId && d.AssignmentId == assignmentId)
            .Where(d => d.DocumentType == DocumentType.AssignmentDescription
                     || d.DocumentType == DocumentType.GradingRubric)
            .ToListAsync();

        return new AssignmentContext
        {
            AssignmentDescription = docs
                .FirstOrDefault(d => d.DocumentType == DocumentType.AssignmentDescription)
                ?.Content ?? "",
            GradingRubric = docs
                .FirstOrDefault(d => d.DocumentType == DocumentType.GradingRubric)
                ?.Content ?? ""
        };
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
            return "Jag hittade inget relevant kursmaterial för den frågan. Vänligen ta kontakt med din lärare för att få svar.";

        var context = string.Join("\n\n", relevantChunks.Select(c => c.Text));

        var prompt = $"""
            Du är en hjälpsam kursassistent för läraren Anna Lindqvist på Yrkesakademin.
            Svara på studentens fråga baserat ENDAST på kursmaterialet nedan.
            Om svaret inte finns i materialet, säg att du inte kan svara på frågan och uppmanar studenten att ta kontakt med läraren för att få svar.
            Svara på svenska och håll svaret kort och tydligt.
            Stava alltid korrekt och använd aldrig stora bokstäver mitt i meningar eller ord.

            Kursmaterial:
            {context}

            Studentens fråga: {question}
            """;

        return await _llm.GenerateAsync(prompt);
    }
}

public class AssignmentContext
{
    public string AssignmentDescription { get; set; } = "";
    public string GradingRubric { get; set; } = "";
    public bool HasContext => !string.IsNullOrEmpty(AssignmentDescription) || !string.IsNullOrEmpty(GradingRubric);
}
