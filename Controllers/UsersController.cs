using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        var model = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = await _userManager.IsLockedOutAsync(user);

            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? user.Role,
                IsActive = user.IsActive,
                IsLockedOut = isLocked,
                CreatedDate = user.CreatedDate
            });
        }

        return View(model);
    }

    public IActionResult Create()
    {
        PopulateRolesDropdown();
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError("Email", "A user with this email already exists.");
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Role = model.Role,
            IsActive = model.IsActive,
            EmailConfirmed = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            await _auditService.LogAsync("User Created", "ApplicationUser", user.Id, null, $"Admin created user {user.Email} with role {model.Role}");

            TempData["Success"] = $"User '{user.FullName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        PopulateRolesDropdown(model.Role);
        return View(model);
    }

    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? user.Role,
            IsActive = user.IsActive
        };

        PopulateRolesDropdown(model.Role);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        if (id != model.Id) return BadRequest();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var oldRole = currentRoles.FirstOrDefault() ?? user.Role;

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.IsActive = model.IsActive;
        user.Role = model.Role;

        if (model.UnlockAccount)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
        }

        var updateRes = await _userManager.UpdateAsync(user);
        if (!updateRes.Succeeded)
        {
            foreach (var error in updateRes.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        if (oldRole != model.Role)
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
            await _auditService.LogAsync("Role Changed", "ApplicationUser", user.Id, oldRole, model.Role);
        }

        await _auditService.LogAsync("User Updated", "ApplicationUser", user.Id, null, $"User {user.Email} updated");
        TempData["Success"] = $"User '{user.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync("User Status Toggled", "ApplicationUser", user.Id, null, $"User {user.Email} active status set to {user.IsActive}");
        TempData["Success"] = $"User status updated to {(user.IsActive ? "Active" : "Inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    private void PopulateRolesDropdown(string selectedRole = "SalesExecutive")
    {
        var roles = new[] { "Admin", "Manager", "SalesExecutive" };
        ViewBag.Roles = new SelectList(roles, selectedRole);
    }
}
