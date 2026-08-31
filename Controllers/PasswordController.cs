using WritePad.Services;
using Microsoft.AspNetCore.Mvc;

namespace Token2Core.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordController : ControllerBase
    {
        private readonly PasswordService _passwordService;

        public PasswordController(PasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        [HttpPost("hash")]
        public IActionResult HashPassword(string password)
        {
            var hash = _passwordService.HashPassword(password);

            return Ok(new
            {
                passwordHash = hash
            });
        }

        [HttpPost("verify")]
        public IActionResult VerifyPassword(
            string password,
            string hashedPassword)
        {
            var result = _passwordService.VerifyPassword(
                hashedPassword,
                password);

            return Ok(new
            {
                passwordValid = result
            });
        }
    }
}
