namespace TeacherAid.Api.Controllers
{
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using TeacherAid.Api.Data;
    using TeacherAid.Api.Models;
    using TeacherAid.Api.DTO;

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SubmissionsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _http;

        public SubmissionsController(AppDbContext db, IHttpClientFactory http)
        {
            _db = db;
            _http = http;
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

            try
            {
                var client = _http.CreateClient();
                var response = await client.PostAsJsonAsync("http://localhost:5678/webhook/feedback", new
                {
                    submissionId = submission.Id,
                    studentName = submission.StudentName,
                    courseId = submission.CourseId,
                    content = submission.Content
                });

                if (response.IsSuccessStatusCode)
                {
                    log.Status = "success";
                }
                else
                {
                    log.Status = "failed";
                    log.ErrorMessage = $"n8n svarade med statuskod {(int)response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                log.Status = "failed";
                log.ErrorMessage = ex.Message;
            }
            finally
            {
                await _db.SaveChangesAsync();
            }

            return log.Status == "success"
                ? Ok("Processing started")
                : StatusCode(502, new { error = "n8n-anropet misslyckades", detail = log.ErrorMessage });
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

        // 5. Hämta automationsloggar (läraren kan se historik)
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
