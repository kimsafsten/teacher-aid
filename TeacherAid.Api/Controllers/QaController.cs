using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherAid.Api.Data;
using TeacherAid.Api.Models;
using TeacherAid.Api.Services;

[ApiController]
[Route("api/[controller]")]
public class QaController : ControllerBase
{
    private readonly RagService _rag;
    private readonly AppDbContext _db;
    private readonly ILLMService _llm;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public QaController(RagService rag, AppDbContext db, ILLMService llm,
        IConfiguration config, IWebHostEnvironment env)
    {
        _rag = rag;
        _db = db;
        _llm = llm;
        _config = config;
        _env = env;
    }

    [Authorize]
    [HttpPost("documents")]
    public async Task<IActionResult> UploadDocument([FromBody] UploadDocumentDto dto)
    {
        await _rag.IndexDocument(dto.CourseId, "", DocumentType.CourseMaterial, dto.FileName, dto.Content);
        return Ok("Dokument indexerat");
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskQuestionDto dto)
    {
        if (dto.Question.Length > 400)
            return BadRequest(new { error = "Frågan får inte vara längre än 400 tecken." });

        var answer = await _rag.AskQuestion(dto.CourseId, dto.Question);
        return Ok(new { answer });
    }

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

        var prompt = $"""
            Du är en erfaren pedagogisk assistent för läraren Anna Lindqvist på Yrkesakademin.
            Baserat på befintligt kursmaterial nedan, generera nytt material enligt instruktionen.
            Svara på svenska. Formatera svaret tydligt med rubriker och punktlistor där det passar.

            Befintligt kursmaterial:
            {context}

            Instruktion: {dto.Instruction}
            """;

        var result = await _llm.GenerateAsync(prompt);
        var savedAs = await SaveToFileAsync(dto.CourseId, dto.Instruction, result);
        return Ok(new { content = result, savedAs });
    }

    [Authorize]
    [HttpGet("generated")]
    public IActionResult ListGenerated()
    {
        var folder = ResolveGeneratedFolder();
        if (!Directory.Exists(folder))
            return Ok(new { files = Array.Empty<object>() });

        var files = Directory.GetFiles(folder, "*.md")
            .Select(f => new
            {
                fileName = Path.GetFileName(f),
                generatedAt = System.IO.File.GetLastWriteTime(f).ToString("yyyy-MM-dd HH:mm")
            })
            .OrderByDescending(f => f.generatedAt)
            .ToList();

        return Ok(new { files });
    }

    [Authorize]
    [HttpGet("generated/{fileName}")]
    public IActionResult GetGenerated(string fileName)
    {
        if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
            return BadRequest("Ogiltigt filnamn.");

        var folder = ResolveGeneratedFolder();
        var filePath = Path.Combine(folder, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var content = System.IO.File.ReadAllText(filePath);
        return Ok(new { content });
    }

    [Authorize]
    [HttpPut("generated/{fileName}")]
    public async Task<IActionResult> SaveGenerated(string fileName, [FromBody] SaveGeneratedDto dto)
    {
        if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
            return BadRequest("Ogiltigt filnamn.");

        var folder = ResolveGeneratedFolder();
        var filePath = Path.Combine(folder, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        await System.IO.File.WriteAllTextAsync(filePath, dto.Content);
        return Ok();
    }

    private string ResolveGeneratedFolder()
    {
        var basePath = _config["FolderPaths:Generated"] ?? "../genererat";
        return Path.IsPathRooted(basePath)
            ? basePath
            : Path.Combine(_env.ContentRootPath, basePath);
    }

    private async Task<string> SaveToFileAsync(string courseId, string instruction, string content)
    {
        var folder = ResolveGeneratedFolder();
        Directory.CreateDirectory(folder);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var fileName = $"{courseId}_{timestamp}.md";
        var filePath = Path.Combine(folder, fileName);

        var markdown = $"""
            # Genererat kursmaterial

            **Kurs:** {courseId}
            **Instruktion:** {instruction}
            **Genererat:** {DateTime.Now:yyyy-MM-dd HH:mm}

            ---

            {content}
            """;

        await System.IO.File.WriteAllTextAsync(filePath, markdown);
        return fileName;
    }
}

public record UploadDocumentDto(string CourseId, string FileName, string Content);
public record AskQuestionDto(string CourseId, string Question);
public record GenerateMaterialDto(string CourseId, string Instruction);
public record SaveGeneratedDto(string Content);
