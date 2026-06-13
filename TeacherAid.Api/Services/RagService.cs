using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Text;
using TeacherAid.Api.Data;
using TeacherAid.Api.Models;

namespace TeacherAid.Api.Services;

public class RagService
{
    private readonly AppDbContext _db;
    private readonly ILLMService _llm;

    public RagService(AppDbContext db, ILLMService llm)
    {
        _db = db;
        _llm = llm;
    }

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
