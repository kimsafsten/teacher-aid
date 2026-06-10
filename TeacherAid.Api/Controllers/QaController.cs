using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TeacherAid.Api.Data;
using TeacherAid.Api.Services;

[ApiController]
[Route("api/[controller]")]
public class QaController : ControllerBase
{
    private readonly RagService _rag;
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _http;

    public QaController(RagService rag, AppDbContext db, IHttpClientFactory http)
    {
        _rag = rag;
        _db = db;
        _http = http;
    }

    // Ladda upp kursdokument
    [Authorize]
    [HttpPost("documents")]
    public async Task<IActionResult> UploadDocument([FromBody] UploadDocumentDto dto)
    {
        await _rag.IndexDocument(dto.CourseId, dto.FileName, dto.Content);
        return Ok("Dokument indexerat");
    }

    // Ställ en fråga
    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskQuestionDto dto)
    {
        var answer = await _rag.AskQuestion(dto.CourseId, dto.Question);
        return Ok(new { answer });
    }

    // Generera kursmaterial
    [Authorize]
    [HttpPost("generate-material")]
    public async Task<IActionResult> GenerateMaterial([FromBody] GenerateMaterialDto dto)
    {
        var existingDocs = await _db.CourseDocuments
            .Where(d => d.CourseId == dto.CourseId)
            .Select(d => d.Content)
            .ToListAsync();

        var context = existingDocs.Any()
            ? string.Join("\n\n", existingDocs)
            : "Inget befintligt kursmaterial finns uppladdat.";

        var client = _http.CreateClient("ollama");
        var response = await client.PostAsJsonAsync("http://localhost:11434/api/generate", new
        {
            model = "llama3",
            prompt = $"""
                Du är en erfaren pedagogisk assistent för läraren Anna Lindqvist på Yrkesakademin.
                Baserat på befintligt kursmaterial nedan, generera nytt material enligt instruktionen.
                Svara på svenska. Formatera svaret tydligt med rubriker och punktlistor där det passar.

                Befintligt kursmaterial:
                {context}

                Instruktion: {dto.Instruction}
                """,
            stream = false
        });

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var result = doc.RootElement.GetProperty("response").GetString();
        return Ok(new { content = result });
    }
}

public record UploadDocumentDto(string CourseId, string FileName, string Content);
public record AskQuestionDto(string CourseId, string Question);
public record GenerateMaterialDto(string CourseId, string Instruction);
