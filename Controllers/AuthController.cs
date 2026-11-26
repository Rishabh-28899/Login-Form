
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Token2Core.Data;
using WritePad.Model;
using WritePad.Services;

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
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Username == request.Username,
                cancellationToken);

        if (user == null)
        {
            return Unauthorized("Username or password is incorrect");
        }

        var passwordValid = _passwordService.VerifyPassword(
            user.PasswordHash,
            request.Password);

        if (!passwordValid)
        {
            return Unauthorized("Username or password is incorrect");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var accessToken = GenerateAccessToken(claims);
        var refreshToken = GenerateRefreshToken();

        var refreshTokenEntity = new RefreshTokem
        {
            Username = user.Username,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        await _context.RefreshTokems.AddAsync(
            refreshTokenEntity,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

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
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest("Refresh token is required");
        }

        var storedToken = await _context.RefreshTokems
            .FirstOrDefaultAsync(
                x => x.Token == request.RefreshToken,
                cancellationToken);

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

        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Username == storedToken.Username,
                cancellationToken);

        if (user == null)
        {
            return Unauthorized("User not found");
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var newAccessToken = GenerateAccessToken(claims);
        var newRefreshToken = GenerateRefreshToken();

        storedToken.IsRevoked = true;

        var newRefreshTokenEntity = new RefreshTokem
        {
            Username = user.Username,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        await _context.RefreshTokems.AddAsync(
            newRefreshTokenEntity,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken
        });
    }

    // ==========================================
    // LOGOUT
    // ==========================================
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return BadRequest("Refresh token is required");
        }

        var storedToken = await _context.RefreshTokems
            .FirstOrDefaultAsync(
                x => x.Token == request.RefreshToken,
                cancellationToken);

        if (storedToken == null)
        {
            return Unauthorized("Invalid refresh token");
        }

        if (!storedToken.IsRevoked)
        {
            storedToken.IsRevoked = true;

            await _context.SaveChangesAsync(cancellationToken);
        }

        return Ok(new
        {
            message = "Logged out successfully"
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

