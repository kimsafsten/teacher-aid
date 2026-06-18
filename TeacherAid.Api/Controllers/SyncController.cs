using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeacherAid.Api.Services;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SyncController : ControllerBase
{
    private readonly FolderSyncService _sync;

    public SyncController(FolderSyncService sync)
    {
        _sync = sync;
    }

    /// <summary>
    /// Scans the course-material folder and indexes new documents for RAG.
    /// </summary>
    [HttpPost("course-material")]
    public async Task<IActionResult> SyncCourseMaterial()
    {
        var result = await _sync.SyncCourseMaterial();
        return Ok(result);
    }

    /// <summary>
    /// Scans the submissions folder, pseudonymizes content, and creates Submission records.
    /// File name format: Firstname_Lastname_CourseId.pdf/.docx (assignment name optional, e.g. Firstname_Lastname_CourseId_Assignment.pdf).
    /// </summary>
    [HttpPost("submissions")]
    public async Task<IActionResult> SyncSubmissions()
    {
        var result = await _sync.SyncSubmissions();
        return Ok(result);
    }
}
