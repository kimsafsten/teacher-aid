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
    /// Skannar kursmaterial-mappen och indexerar nya dokument för RAG.
    /// </summary>
    [HttpPost("kursmaterial")]
    public async Task<IActionResult> SyncKursmaterial()
    {
        var result = await _sync.SyncKursmaterial();
        return Ok(result);
    }

    /// <summary>
    /// Skannar inlämnings-mappen, pseudonymiserar och skapar Submission-poster.
    /// Filnamnsformat: Förnamn_Efternamn_KursID.pdf/.docx (uppgiftsnamn är valfritt, t.ex. Förnamn_Efternamn_KursID_Uppgift.pdf)
    /// </summary>
    [HttpPost("inlamningar")]
    public async Task<IActionResult> SyncInlamningar()
    {
        var result = await _sync.SyncInlamningar();
        return Ok(result);
    }
}
