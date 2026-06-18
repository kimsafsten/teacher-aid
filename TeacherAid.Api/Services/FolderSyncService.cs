using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using TeacherAid.Api.Data;
using TeacherAid.Api.Models;

namespace TeacherAid.Api.Services;

/// <summary>
/// Scans configured folders on disk and syncs course materials and student submissions into the database.
/// </summary>
public class FolderSyncService
{
    private readonly AppDbContext _db;
    private readonly RagService _rag;
    private readonly DocumentExtractorService _extractor;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IHttpClientFactory _http;

    private static readonly string[] SupportedExtensions = [".pdf", ".docx", ".txt"];

    private static class AssignmentDocumentFileNames
    {
        public const string Description = "uppgiftsbeskrivning";
        public const string GradingRubric = "bedömningsmall";
    }

    private static readonly string[] AssignmentDocumentNames =
        [AssignmentDocumentFileNames.Description, AssignmentDocumentFileNames.GradingRubric];

    public FolderSyncService(AppDbContext db, RagService rag, DocumentExtractorService extractor, IConfiguration config, IWebHostEnvironment env, IHttpClientFactory http)
    {
        _db = db;
        _rag = rag;
        _extractor = extractor;
        _config = config;
        _env = env;
        _http = http;
    }

    public async Task<SyncResult> SyncCourseMaterial()
    {
        var folder = ResolvePath(_config["FolderPaths:CourseMaterial"] ?? "../kursmaterial");
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
                var courseId = SubmissionFileNameParser.ParseCourseId(fileName) ?? "okänd";
                await _rag.IndexDocument(courseId, "", DocumentType.CourseMaterial, fileName, text, fileName);
                result.Processed.Add(fileName);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{fileName}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<SyncResult> SyncSubmissions()
    {
        var rootFolder = ResolvePath(_config["FolderPaths:Submissions"] ?? "../inlamningar");
        var result = new SyncResult();

        if (!Directory.Exists(rootFolder))
        {
            Directory.CreateDirectory(rootFolder);
            result.Message = $"Mappen skapades: {rootFolder}. Lägg in inlämningar och synka igen.";
            return result;
        }

        var existingSubmissions = await _db.Submissions
            .Where(s => s.SourceFileName != null)
            .Select(s => s.SourceFileName!)
            .ToListAsync();

        var existingDocuments = await _db.CourseDocuments
            .Where(d => d.SourceFileName != null)
            .Select(d => d.SourceFileName!)
            .ToListAsync();

        var newSubmissions = new List<Submission>();

        foreach (var courseFolder in Directory.GetDirectories(rootFolder))
        {
            var courseId = Path.GetFileName(courseFolder);

            foreach (var assignmentFolder in Directory.GetDirectories(courseFolder))
            {
                var assignmentId = Path.GetFileName(assignmentFolder);

                var files = Directory.GetFiles(assignmentFolder)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();

                bool hasAssignmentDescription = files.Any(f =>
                    Path.GetFileNameWithoutExtension(f).ToLowerInvariant() == AssignmentDocumentFileNames.Description);
                bool hasGradingRubric = files.Any(f =>
                    Path.GetFileNameWithoutExtension(f).ToLowerInvariant() == AssignmentDocumentFileNames.GradingRubric);

                var missingDocs = new List<string>();
                if (!hasAssignmentDescription) missingDocs.Add(AssignmentDocumentFileNames.Description);
                if (!hasGradingRubric) missingDocs.Add(AssignmentDocumentFileNames.GradingRubric);

                if (missingDocs.Count > 0)
                {
                    result.Warnings.Add(
                        $"{courseId}/{assignmentId}: saknar {string.Join(" och ", missingDocs)} — inlämningar hoppades över.");
                    continue;
                }

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var baseName = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();

                    if (AssignmentDocumentNames.Contains(baseName))
                    {
                        if (existingDocuments.Contains(fileName)) continue;
                        try
                        {
                            var docType = baseName == AssignmentDocumentFileNames.GradingRubric
                                ? DocumentType.GradingRubric
                                : DocumentType.AssignmentDescription;

                            var text = _extractor.ExtractText(file);
                            await _rag.IndexDocument(courseId, assignmentId, docType, fileName, text, fileName);
                            result.Processed.Add($"[{docType}] {fileName}");
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"{fileName}: {ex.Message}");
                        }
                    }
                    else
                    {
                        if (existingSubmissions.Contains(fileName)) continue;
                        try
                        {
                            var text = _extractor.ExtractText(file);
                            var (studentName, _) = SubmissionFileNameParser.Parse(fileName);
                            var anonymizedText = TextPseudonymizer.Pseudonymize(text, studentName);

                            var submission = new Submission
                            {
                                StudentName = studentName,
                                CourseId = courseId,
                                AssignmentId = assignmentId,
                                Content = anonymizedText,
                                SourceFileName = fileName
                            };
                            _db.Submissions.Add(submission);
                            newSubmissions.Add(submission);

                            result.Processed.Add(fileName);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"{fileName}: {ex.Message}");
                        }
                    }
                }
            }
        }

        if (newSubmissions.Count > 0)
        {
            await _db.SaveChangesAsync();

            foreach (var submission in newSubmissions)
            {
                _db.AutomationLogs.Add(new AutomationLog
                {
                    SubmissionId = submission.Id,
                    Status = "pending"
                });
            }
            await _db.SaveChangesAsync();

            var n8nUrl = _config["N8n:WebhookUrl"] ?? "http://localhost:5678/webhook/feedback";

            foreach (var submission in newSubmissions)
            {
                var context = await _rag.GetAssignmentContext(submission.CourseId, submission.AssignmentId);
                var payload = new
                {
                    submissionId = submission.Id,
                    courseId = submission.CourseId,
                    assignmentId = submission.AssignmentId,
                    content = submission.Content,
                    assignmentDescription = context.AssignmentDescription,
                    gradingRubric = context.GradingRubric
                };
                // Fire-and-forget: webhook errors are not surfaced to the caller.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var client = _http.CreateClient("ollama");
                        await client.PostAsJsonAsync(n8nUrl, payload);
                    }
                    catch { }
                });
            }
        }

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
    public List<string> Warnings { get; set; } = [];
    public string? Message { get; set; }
    public int ProcessedCount => Processed.Count;
    public int ErrorCount => Errors.Count;
    public int WarningCount => Warnings.Count;
}
