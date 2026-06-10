using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using TeacherAid.Api.Data;
using TeacherAid.Api.Models;

namespace TeacherAid.Api.Services
{
    public class RagService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _http;
        private const string OllamaUrl = "http://localhost:11434";

        public RagService(AppDbContext db, IHttpClientFactory http)
        {
            _db = db;
            _http = http;
        }

        // Devide text into chunks on about ~500 characters
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

        // Get embedding från Ollama
        private async Task<Vector> GetEmbedding(string text)
        {
            var client = _http.CreateClient();
            var response = await client.PostAsJsonAsync($"{OllamaUrl}/api/embed", new
            {
                model = "nomic-embed-text",
                input = text
            });

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var floats = doc.RootElement
                .GetProperty("embeddings")[0]
                .EnumerateArray()
                .Select(e => e.GetSingle())
                .ToArray();

            return new Vector(floats);
        }

        // Upload and indexate a document
        public async Task IndexDocument(string courseId, string fileName, string content)
        {
            var doc = new CourseDocument
            {
                CourseId = courseId,
                FileName = fileName,
                Content = content
            };
            _db.CourseDocuments.Add(doc);
            await _db.SaveChangesAsync();

            var chunks = ChunkText(content);
            foreach (var chunk in chunks)
            {
                var embedding = await GetEmbedding(chunk);
                _db.DocumentChunks.Add(new DocumentChunk
                {
                    CourseDocumentId = doc.Id,
                    CourseId = courseId,
                    Text = chunk,
                    Embedding = embedding
                });
            }
            await _db.SaveChangesAsync();
        }

        // Answer a students question with RAG
        public async Task<string> AskQuestion(string courseId, string question)
        {
            // Embedd question
            var questionEmbedding = await GetEmbedding(question);

            // Find the 3 closest chunks
            var relevantChunks = await _db.DocumentChunks
                .Where(c => c.CourseId == courseId)
                .OrderBy(c => c.Embedding!.L2Distance(questionEmbedding))
                .Take(3)
                .ToListAsync();

            if (!relevantChunks.Any())
                return "Jag hittade inget relevant kursmaterial för den frågan.";

            var context = string.Join("\n\n", relevantChunks.Select(c => c.Text));

            // Send to Ollama with context
            var client = _http.CreateClient();
            var response = await client.PostAsJsonAsync($"{OllamaUrl}/api/generate", new
            {
                model = "llama3",
                prompt = $"""
                    Du är en hjälpsam kursassistent för läraren Anna Lindqvist på Yrkesakademin.
                    Svara på studentens fråga baserat ENDAST på kursmaterialet nedan.
                    Om svaret inte finns i materialet, säg det istället för att gissa.
                    Svara på svenska och håll svaret kort och tydligt.

                    Kursmaterial:
                    {context}

                    Studentens fråga: {question}
                    """,
                stream = false
            });

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("response").GetString() ?? "Inget svar genererades.";
        }
    }
}
