using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Dto;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Authorize]
public class FollowUpsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public FollowUpsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService)
    {
        _context = context;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var isSales = User.IsInRole("SalesExecutive");
        var query = _context.FollowUps.AsQueryable();

        if (isSales)
        {
            query = query.Where(f => f.AssignedTo == user.Id);
        }

        var followUps = await query
            .Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                CustomerId = f.CustomerId,
                LeadId = f.LeadId,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedTo = f.AssignedTo
            })
            .ToListAsync();

        return Ok(followUps);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FollowUpDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType ?? "Call",
            Subject = dto.Subject,
            Remarks = dto.Remarks,
            Status = dto.Status ?? "Planned",
            AssignedTo = User.IsInRole("SalesExecutive") ? user.Id : dto.AssignedTo
        };

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("API Follow-up Created", "FollowUp", followUp.FollowUpId.ToString(), null, $"Subject: {followUp.Subject}, Date: {followUp.FollowUpDate:d}");

        dto.FollowUpId = followUp.FollowUpId;
        return CreatedAtAction(nameof(GetAll), new { id = followUp.FollowUpId }, dto);
    }
}
