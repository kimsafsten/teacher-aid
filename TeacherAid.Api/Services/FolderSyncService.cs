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
                // Parse course ID from the file name (second segment: DocumentName_CourseId.pdf)
                var courseId = SubmissionFileNameParser.ParseCourseId(fileName) ?? "okänd";
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
                var (studentName, courseId) = SubmissionFileNameParser.Parse(fileName);

                // Replace student name occurrences in the submission text
                var anonymizedText = TextPseudonymizer.Pseudonymize(text, studentName);

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

    private string ResolvePath(string relativePath)
    {
        // ContentRootPath points to the project directory (where .csproj and appsettings.json live),
        // regardless of whether the app runs via dotnet run or from bin/Debug.
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
