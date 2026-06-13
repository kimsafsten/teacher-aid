using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using TeacherAid.Api.Data;
using TeacherAid.Api.Models;

namespace TeacherAid.Api.Services;

public class FolderSyncService
{
    private readonly AppDbContext _db;
    private readonly RagService _rag;
    private readonly DocumentExtractorService _extractor;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] SupportedExtensions = [".pdf", ".docx", ".txt"];

    public FolderSyncService(AppDbContext db, RagService rag, DocumentExtractorService extractor, IConfiguration config, IWebHostEnvironment env)
    {
        _db = db;
        _rag = rag;
        _extractor = extractor;
        _config = config;
        _env = env;
    }

    public async Task<SyncResult> SyncKursmaterial()
    {
        var folder = ResolvePath(_config["FolderPaths:Kursmaterial"] ?? "../kursmaterial");
        var result = new SyncResult();

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            result.Message = $"Mappen skapades: {folder}. Lägg in dokument och synka igen.";
            return result;
        }

        var existingFileNames = await _db.CourseDocuments
            .Where(d => d.SourceFileName != null)
            .Select(d => d.SourceFileName!)
            .ToListAsync();

        var files = Directory.GetFiles(folder)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Where(f => !existingFileNames.Contains(Path.GetFileName(f)))
            .ToList();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            try
            {
                var text = _extractor.ExtractText(file);
                // Parsa kurs-ID ur filnamnet om möjligt (andra segmentet: Kursnamn_KursID.pdf)
                var courseId = ParseCourseIdFromFileName(fileName) ?? "okänd";
                await _rag.IndexDocument(courseId, fileName, text, fileName);
                result.Processed.Add(fileName);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{fileName}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<SyncResult> SyncInlamningar()
    {
        var folder = ResolvePath(_config["FolderPaths:Inlamningar"] ?? "../inlamningar");
        var result = new SyncResult();

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
            result.Message = $"Mappen skapades: {folder}. Lägg in inlämningar och synka igen.";
            return result;
        }

        var existingFileNames = await _db.Submissions
            .Where(s => s.SourceFileName != null)
            .Select(s => s.SourceFileName!)
            .ToListAsync();

        var files = Directory.GetFiles(folder)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Where(f => !existingFileNames.Contains(Path.GetFileName(f)))
            .ToList();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            try
            {
                var text = _extractor.ExtractText(file);
                var (studentName, courseId) = ParseSubmissionFileName(fileName);

                // Pseudonymisera: ersätt studentens namn i texten
                var anonymizedText = PseudonymizeText(text, studentName);

                _db.Submissions.Add(new Submission
                {
                    StudentName = studentName,
                    CourseId = courseId,
                    Content = anonymizedText,
                    SourceFileName = fileName
                });

                result.Processed.Add(fileName);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{fileName}: {ex.Message}");
            }
        }

        if (result.Processed.Any())
            await _db.SaveChangesAsync();

        return result;
    }

    // Förnamn_Efternamn_KursID_valfritt.pdf → ("Förnamn Efternamn", "KursID")
    private static (string studentName, string courseId) ParseSubmissionFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var parts = name.Split('_');

        var studentName = parts.Length >= 2
            ? $"{parts[0]} {parts[1]}"
            : parts[0];

        var courseId = parts.Length >= 3 ? parts[2] : "okänd";

        return (studentName, courseId);
    }

    // KursNamn_KursID.pdf → "KursID" (valfritt, andra segmentet)
    private static string? ParseCourseIdFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var parts = name.Split('_');
        return parts.Length >= 2 ? parts[1] : null;
    }

    private static string PseudonymizeText(string text, string studentName)
    {
        if (string.IsNullOrWhiteSpace(studentName)) return text;

        // Ersätt fullständigt namn och delar av det
        var parts = studentName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var result = text;

        // Ersätt fullt namn först (viktigast)
        result = result.Replace(studentName, "[Student]", StringComparison.OrdinalIgnoreCase);

        // Ersätt förnamn och efternamn separat
        foreach (var part in parts.Where(p => p.Length > 2))
            result = result.Replace(part, "[Student]", StringComparison.OrdinalIgnoreCase);

        return result;
    }

    private string ResolvePath(string relativePath)
    {
        // ContentRootPath pekar på projektkatalogen (där .csproj och appsettings.json finns),
        // oavsett om appen körs via dotnet run eller från bin/Debug.
        return Path.GetFullPath(Path.Combine(_env.ContentRootPath, relativePath));
    }
}

public class SyncResult
{
    public List<string> Processed { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public string? Message { get; set; }
    public int ProcessedCount => Processed.Count;
    public int ErrorCount => Errors.Count;
}
