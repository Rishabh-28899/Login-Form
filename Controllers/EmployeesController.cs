using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WritePad.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult GetEmployees()
    {
        return Ok(new
        {
            message = "You are authorized!",
            user = User.Identity?.Name
        });
    }
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            message = "You are an Admin!",
            user = User.Identity?.Name,
            role = "Admin"
        });
    }
}  

