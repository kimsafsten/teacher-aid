using Microsoft.AspNetCore.Mvc;
using TeacherAid.Api.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    /// <summary>
    /// Authenticates a teacher and returns a JWT on success.
    /// </summary>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        var token = _auth.Login(dto.Username, dto.Password);
        if (token == null) return Unauthorized("Fel användarnamn eller lösenord");
        return Ok(new { token });
    }
}

public record LoginDto(string Username, string Password);
