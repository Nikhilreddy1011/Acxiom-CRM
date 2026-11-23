using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class FollowUpsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public FollowUpsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? filter = "All", string? type = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var isSales = User.IsInRole("SalesExecutive");
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedUser)
            .AsQueryable();

        if (isSales)
        {
            query = query.Where(f => f.AssignedTo == user.Id);
        }

        var now = DateTime.UtcNow;

        switch (filter?.ToLower())
        {
            case "pending":
                query = query.Where(f => f.Status == "Planned" && f.FollowUpDate >= now);
                break;
            case "overdue":
                query = query.Where(f => (f.Status == "Planned" || f.Status == "Missed") && f.FollowUpDate < now);
                break;
            case "completed":
                query = query.Where(f => f.Status == "Completed");
                break;
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(f => f.FollowUpType == type);
        }

        var followUps = await query.OrderBy(f => f.FollowUpDate).ToListAsync();

        ViewBag.CurrentFilter = filter;
        ViewBag.CurrentType = type;

        return View(followUps);
    }

    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        await PopulateDropdownsAsync(customerId, leadId);
        return View(new FollowUp
        {
            CustomerId = customerId,
            LeadId = leadId,
            FollowUpDate = DateTime.UtcNow.AddDays(1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp followUp)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Validation: FollowUpDate cannot be earlier than today for planned follow-up
        if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError("FollowUpDate", "New/planned follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(followUp.CustomerId, followUp.LeadId, followUp.AssignedTo);
            return View(followUp);
        }

        if (User.IsInRole("SalesExecutive"))
        {
            followUp.AssignedTo = user.Id;
        }

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Follow-up Scheduled", "FollowUp", followUp.FollowUpId.ToString(), null, $"Scheduled {followUp.FollowUpType} for {followUp.FollowUpDate:d}: {followUp.Subject}");
        TempData["Success"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && followUp.AssignedTo != user.Id)
        {
            return Forbid();
        }

        await PopulateDropdownsAsync(followUp.CustomerId, followUp.LeadId, followUp.AssignedTo);
        return View(followUp);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUp followUp)
    {
        if (id != followUp.FollowUpId) return BadRequest();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var existing = await _context.FollowUps.FindAsync(id);
        if (existing == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && existing.AssignedTo != user.Id)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(followUp.CustomerId, followUp.LeadId, followUp.AssignedTo);
            return View(followUp);
        }

        var oldStatus = existing.Status;

        existing.CustomerId = followUp.CustomerId;
        existing.LeadId = followUp.LeadId;
        existing.FollowUpDate = followUp.FollowUpDate;
        existing.FollowUpType = followUp.FollowUpType;
        existing.Subject = followUp.Subject;
        existing.Remarks = followUp.Remarks;
        existing.Status = followUp.Status;

        if (!User.IsInRole("SalesExecutive"))
        {
            existing.AssignedTo = followUp.AssignedTo;
        }

        await _context.SaveChangesAsync();

        if (existing.Status == "Completed" && oldStatus != "Completed")
        {
            await _auditService.LogAsync("Follow-up Completed", "FollowUp", existing.FollowUpId.ToString(), $"Status: {oldStatus}", $"Completed on {DateTime.UtcNow:g}");
        }
        else
        {
            await _auditService.LogAsync("Follow-up Updated", "FollowUp", existing.FollowUpId.ToString(), null, $"Status: {existing.Status}, Date: {existing.FollowUpDate:d}");
        }

        TempData["Success"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? completionRemarks)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && followUp.AssignedTo != user.Id)
        {
            return Forbid();
        }

        followUp.Status = "Completed";
        if (!string.IsNullOrWhiteSpace(completionRemarks))
        {
            followUp.Remarks = (followUp.Remarks + "\nCompletion note: " + completionRemarks).Trim();
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("Follow-up Completed", "FollowUp", followUp.FollowUpId.ToString(), null, $"Marked completed: {followUp.Subject}");

        TempData["Success"] = "Follow-up marked as completed.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? leadId = null, string? userId = null)
    {
        var customers = await _context.Customers.Where(c => c.Status == "Active").OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _context.Leads.Where(l => l.Status != "Converted").OrderBy(l => l.LeadName).ToListAsync();
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();

        ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", customerId);
        ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName", leadId);
        ViewBag.Users = new SelectList(users, "Id", "FullName", userId);
    }
}
