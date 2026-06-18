using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using TeacherAid.Api.Data;
using TeacherAid.Api.Models;

namespace TeacherAid.Api.Services;

/// <summary>
/// Indexes course documents for RAG and answers student questions from the nearest embedded chunks.
/// </summary>
public class RagService
{
    private const int RelevantChunkCount = 3;

    private readonly AppDbContext _db;
    private readonly ILLMService _llm;

    public RagService(AppDbContext db, ILLMService llm)
    {
        _db = db;
        _llm = llm;
    }

    /// <summary>
    /// Persists document metadata, chunks the content, embeds each chunk, and stores the vectors.
    /// </summary>
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
    /// Loads the assignment description and grading rubric used as feedback context.
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
    /// Answers a question using the <see cref="RelevantChunkCount"/> nearest chunks by L2 distance.
    /// </summary>
    public async Task<string> AskQuestion(string courseId, string question)
    {
        var floats = await _llm.GetEmbeddingAsync(question);
        var questionEmbedding = new Vector(floats);

        var relevantChunks = await _db.DocumentChunks
            .Where(c => c.CourseId == courseId)
            .OrderBy(c => c.Embedding!.L2Distance(questionEmbedding))
            .Take(RelevantChunkCount)
            .ToListAsync();

        if (!relevantChunks.Any())
            return "Jag hittade inget relevant kursmaterial för den frågan. Vänligen ta kontakt med din lärare för att få svar.";

        var context = string.Join("\n\n", relevantChunks.Select(c => c.Text));

        var prompt = $"""
            Du är en hjälpsam kursassistent för läraren Anna Lindqvist på Yrkesakademin.
            Svara på studentens fråga baserat ENDAST på kursmaterialet nedan.
            Om svaret inte finns i materialet, säg att du inte kan svara på frågan och uppmanar studenten att ta kontakt med läraren för att få svar.
            Svara på svenska och håll svaret kort och tydligt. Behåll etablerade facktermer på engelska när det är branschstandard (t.ex. structure as code).
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
