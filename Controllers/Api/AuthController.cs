using AcxiomCRM.Models;
using AcxiomCRM.Models.Dto;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !user.IsActive)
        {
            await _auditService.LogAsync("API Login Failed", "Authentication", null, null, $"API login attempt failed for {model.Email}");
            return Unauthorized(new { message = "Invalid email or inactive account." });
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, isPersistent: false, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            await _auditService.LogAsync("API Login Succeeded", "Authentication", user.Id, null, $"API user {user.Email} logged in");

            return Ok(new
            {
                message = "Login successful",
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    Roles = roles
                }
            });
        }

        if (result.IsLockedOut)
        {
            return StatusCode(423, new { message = "Account is locked out due to multiple failed login attempts." });
        }

        return Unauthorized(new { message = "Invalid credentials." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new { message = "Logged out successfully." });
    }
}
