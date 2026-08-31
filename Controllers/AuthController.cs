using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WritePad.Model;
using WritePad.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Token2Core.Data;


namespace Token2Core.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly TokenDbContext _context;
    private readonly PasswordService _passwordService;

    public AuthController(
        IConfiguration configuration,
        TokenDbContext context,
        PasswordService passwordService)
    {
        _configuration = configuration;
        _context = context;
        _passwordService = passwordService;
    }

    // ==========================================
    // LOGIN
    // ==========================================
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Find user in database
        var user = _context.Users
            .FirstOrDefault(x => x.Username == request.Username);

        if (user == null)
        {
            return Unauthorized("Username or password is incorrect");
        }

        // Verify password against stored hash
        var passwordValid = _passwordService.VerifyPassword(
            user.PasswordHash,
            request.Password);

        if (!passwordValid)
        {
            return Unauthorized("Username or password is incorrect");
        }

        // Create claims
        var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Role, user.Role)
};

        // Generate Access Token
        var accessToken = GenerateAccessToken(claims);

        // Generate Refresh Token
        var refreshToken = GenerateRefreshToken();

        // Save Refresh Token to database
        var refreshTokenEntity = new RefreshTokem
        {
            Username = user.Username,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokems.Add(refreshTokenEntity);
        _context.SaveChanges();

        return Ok(new
        {
            accessToken = accessToken,
            refreshToken = refreshToken
        });
    }


    // ==========================================
    // REFRESH TOKEN
    // ==========================================
    [HttpPost("refresh")]
    public IActionResult Refresh(
        [FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest("Refresh token is required");
        }

        // Find token in database
        var storedToken = _context.RefreshTokems
            .FirstOrDefault(x => x.Token == request.RefreshToken);

        if (storedToken == null)
        {
            return Unauthorized("Invalid refresh token");
        }

        if (storedToken.IsRevoked)
        {
            return Unauthorized("Refresh token has been revoked");
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized("Refresh token has expired");
        }

        // Find user
        var user = _context.Users
            .FirstOrDefault(x => x.Username == storedToken.Username);

        if (user == null)
        {
            return Unauthorized("User not found");
        }

        // Create claims
        var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.Role, user.Role)
};

        // Generate new Access Token
        var newAccessToken = GenerateAccessToken(claims);

        // Generate new Refresh Token
        var newRefreshToken = GenerateRefreshToken();

        // Revoke old refresh token
        storedToken.IsRevoked = true;

        // Save new refresh token
        var newRefreshTokenEntity = new RefreshTokem
        {
            Username = user.Username,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokems.Add(newRefreshTokenEntity);
        _context.SaveChanges();

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        });
    }


    // ==========================================
    // GENERATE ACCESS TOKEN
    // ==========================================
    private string GenerateAccessToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }


    // ==========================================
    // GENERATE REFRESH TOKEN
    // ==========================================
    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64));
    }
}