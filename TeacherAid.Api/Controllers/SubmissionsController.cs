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

        // 1. Recive studen twork
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

        // 2. Trigger n8n-workflow
        [HttpPost("{id}/process")]
        public async Task<IActionResult> Process(int id)
        {
            var submission = await _db.Submissions.FindAsync(id);
            if (submission == null) return NotFound();

            var client = _http.CreateClient();
            await client.PostAsJsonAsync("http://localhost:5678/webhook/feedback", new
            {
                submissionId = submission.Id,
                studentName = submission.StudentName,
                courseId = submission.CourseId,
                content = submission.Content
            });

            return Ok("Processing started");
        }

        // 3. Get feedbackutkast
        [HttpGet("{id}/feedback")]
        public async Task<IActionResult> GetFeedback(int id)
        {
            var draft = await _db.FeedbackDrafts
                .FirstOrDefaultAsync(f => f.SubmissionId == id);
            if (draft == null) return NotFound("No feedback generated yet");
            return Ok(draft);
        }

        // 4. Save teachers approved feedback
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
    }
}
