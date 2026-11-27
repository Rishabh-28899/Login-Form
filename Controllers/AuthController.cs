
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Token2Core.Data;
using WritePad.DTOs;
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
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IConfiguration configuration,
        TokenDbContext context,
        PasswordService passwordService,
        ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    // ==========================================
    // LOGIN
    // ==========================================
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(
                ApiResponse<object>.FailureResponse(
                    "Username and password are required."));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Username == request.Username,
                cancellationToken);

        if (user == null)
        {
            _logger.LogWarning(
                "Login failed. Invalid credentials.");

            return Unauthorized(
                ApiResponse<object>.FailureResponse(
                    "Username or password is incorrect."));
        }

        var passwordValid = _passwordService.VerifyPassword(
            user.PasswordHash,
            request.Password);

        if (!passwordValid)
        {
            _logger.LogWarning(
                "Login failed for UserId: {UserId}",
                user.Id);

            return Unauthorized(
                ApiResponse<object>.FailureResponse(
                    "Username or password is incorrect."));
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

        _logger.LogInformation(
            "User {UserId} logged in successfully.",
            user.Id);

        var responseData = new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        return Ok(
            ApiResponse<AuthResponseDto>.SuccessResponse(
                responseData,
                "Login successful."));
    }

    // ==========================================
    // REFRESH TOKEN
    // ==========================================
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(
                ApiResponse<object>.FailureResponse(
                    "Refresh token is required."));
        }

        var storedToken = await _context.RefreshTokems
            .FirstOrDefaultAsync(
                x => x.Token == request.RefreshToken,
                cancellationToken);

        if (storedToken == null)
        {
            return Unauthorized(
                ApiResponse<object>.FailureResponse(
                    "Invalid refresh token."));
        }

        if (storedToken.IsRevoked)
        {
            _logger.LogWarning(
                "Attempt to use revoked refresh token.");

            return Unauthorized(
                ApiResponse<object>.FailureResponse(
                    "Refresh token has been revoked."));
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(
                ApiResponse<object>.FailureResponse(
                    "Refresh token has expired."));
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Username == storedToken.Username,
                cancellationToken);

        if (user == null)
        {
            return Unauthorized(
                ApiResponse<object>.FailureResponse(
                    "User not found."));
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

        _logger.LogInformation(
            "Refresh token rotated successfully for UserId: {UserId}",
            user.Id);

        var responseData = new AuthResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };

        return Ok(
            ApiResponse<AuthResponseDto>.SuccessResponse(
                responseData,
                "Token refreshed successfully."));
    }

    // ==========================================
    // LOGOUT
    // ==========================================
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(
                ApiResponse<object>.FailureResponse(
                    "Refresh token is required."));
        }

        var storedToken = await _context.RefreshTokems
            .FirstOrDefaultAsync(
                x => x.Token == request.RefreshToken,
                cancellationToken);

        if (storedToken == null)
        {
            return Unauthorized(
                ApiResponse<object>.FailureResponse(
                    "Invalid refresh token."));
        }

        if (!storedToken.IsRevoked)
        {
            storedToken.IsRevoked = true;

            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation(
            "User logged out. Username: {Username}",
            storedToken.Username);

        return Ok(
            ApiResponse<object>.SuccessResponse(
                null,
                "Logged out successfully."));
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

