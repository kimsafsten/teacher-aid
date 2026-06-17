namespace TeacherAid.Api.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using TeacherAid.Api.Data;
    using TeacherAid.Api.Models;
    using TeacherAid.Api.DTO;
    using TeacherAid.Api.Services;

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _http;
        private readonly RagService _rag;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public SubmissionsController(AppDbContext db, IHttpClientFactory http, RagService rag, IConfiguration config, IWebHostEnvironment env)
        {
            _db = db;
            _http = http;
            _rag = rag;
            _config = config;
            _env = env;
        }

        // 1. Ta emot studentinlämning
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubmissionDto dto)
        {
            var submission = new Submission
            {
                StudentName = dto.StudentName,
                CourseId = dto.CourseId,
                Content = dto.Content
            };
            _db.Submissions.Add(submission);
            await _db.SaveChangesAsync();
            return Ok(new { submission.Id });
        }

        // 2. Trigga n8n-workflow och logga körningen
        [HttpPost("{id}/process")]
        public async Task<IActionResult> Process(int id)
        {
            var submission = await _db.Submissions.FindAsync(id);
            if (submission == null) return NotFound();

            var log = new AutomationLog
            {
                SubmissionId = id,
                Status = "pending"
            };
            _db.AutomationLogs.Add(log);
            await _db.SaveChangesAsync();

            // Hämta uppgiftsbeskrivning och bedömningsmall för uppgiften.
            var context = await _rag.GetAssignmentContext(submission.CourseId, submission.AssignmentId);

            // Fire-and-forget: trigga n8n utan att vänta på svar.
            // Ollama kan ta flera minuter — frontendet pollar ändå för resultatet.
            var payload = new
            {
                submissionId = submission.Id,
                courseId = submission.CourseId,
                assignmentId = submission.AssignmentId,
                content = submission.Content,
                uppgiftsbeskrivning = context.Uppgiftsbeskrivning,
                bedömningsmall = context.Bedömningsmall
            };
            _ = Task.Run(async () =>
            {
                try
                {
                    using var client = _http.CreateClient("ollama"); // 5 min timeout
                    await client.PostAsJsonAsync("http://localhost:5678/webhook/feedback", payload);
                }
                catch
                {
                    // Fel loggas inte här — det viktigaste är att webhooken skickades.
                }
            });

            return Ok("Processing started");
        }

        // 3. Hämta feedbackutkast
        [HttpGet("{id}/feedback")]
        public async Task<IActionResult> GetFeedback(int id)
        {
            var draft = await _db.FeedbackDrafts
                .FirstOrDefaultAsync(f => f.SubmissionId == id);
            if (draft == null) return NotFound("No feedback generated yet");
            return Ok(draft);
        }

        // 4. Spara lärarens godkända feedback
        [HttpPut("{id}/feedback")]
        public async Task<IActionResult> ApproveFeedback(int id, [FromBody] ApproveFeedbackDto dto)
        {
            var draft = await _db.FeedbackDrafts
                .FirstOrDefaultAsync(f => f.SubmissionId == id);
            if (draft == null) return NotFound();

            draft.TeacherFeedback = dto.TeacherFeedback;
            draft.TeacherGrade = dto.TeacherGrade;
            draft.Approved = true;
            await _db.SaveChangesAsync();
            return Ok(draft);
        }

        // 5. Hämta alla inlämningar med feedbackstatus
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var submissions = await _db.Submissions
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new
                {
                    s.Id,
                    s.StudentName,
                    s.CourseId,
                    s.SourceFileName,
                    s.SubmittedAt,
                    Feedback = _db.FeedbackDrafts
                        .Where(f => f.SubmissionId == s.Id)
                        .Select(f => new
                        {
                            f.Approved,
                            f.TeacherFeedback,
                            f.TeacherGrade,
                            f.AiFeedback,
                            f.Summary,
                            f.CreatedAt
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(submissions);
        }

        // 6. Hämta inlämningar utan feedback (väntande)
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var pending = await _db.Submissions
                .Where(s => !_db.FeedbackDrafts.Any(f => f.SubmissionId == s.Id))
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new
                {
                    s.Id,
                    s.StudentName,
                    s.CourseId,
                    s.SourceFileName,
                    s.SubmittedAt
                })
                .ToListAsync();

            return Ok(pending);
        }

        // 7. Servera den ursprungliga inlämningsfilen
        [HttpGet("{id}/file")]
        public async Task<IActionResult> GetFile(int id)
        {
            var submission = await _db.Submissions.FindAsync(id);
            if (submission?.SourceFileName == null) return NotFound();

            var root = Path.GetFullPath(Path.Combine(_env.ContentRootPath,
                _config["FolderPaths:Inlamningar"] ?? "../inlamningar"));

            // Försök direkt sökväg först, sök sedan rekursivt (hanterar gamla inlämningar utan AssignmentId)
            var filePath = Path.Combine(root, submission.CourseId, submission.AssignmentId, submission.SourceFileName);
            if (!System.IO.File.Exists(filePath))
            {
                filePath = Directory
                    .GetFiles(root, submission.SourceFileName, SearchOption.AllDirectories)
                    .FirstOrDefault()!;
            }

            if (filePath == null || !System.IO.File.Exists(filePath))
                return NotFound("Filen hittades inte på disk");

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf"  => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt"  => "text/plain; charset=utf-8",
                _       => "application/octet-stream"
            };

            var stream = System.IO.File.OpenRead(filePath);
            return File(stream, contentType, submission.SourceFileName);
        }

        // 8. Hämta automationsloggar (läraren kan se historik)
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _db.AutomationLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(100)
                .Select(l => new
                {
                    l.Id,
                    l.SubmissionId,
                    l.Timestamp,
                    l.Status,
                    l.TokensUsed,
                    l.ErrorMessage
                })
                .ToListAsync();

            return Ok(logs);
        }
    }
}
