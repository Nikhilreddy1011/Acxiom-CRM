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
[Route("api/leads")]
[Authorize]
public class LeadsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public LeadsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService)
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
        var query = _context.Leads.AsQueryable();

        if (isSales)
        {
            query = query.Where(l => l.AssignedTo == user.Id);
        }

        var leads = await query
            .Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                AssignedTo = l.AssignedTo
            })
            .ToListAsync();

        return Ok(leads);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LeadDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var count = await _context.Leads.CountAsync() + 2001;
        var lead = new Lead
        {
            LeadCode = $"LEAD-{count}",
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = dto.Source ?? "Website",
            Status = dto.Status ?? "New",
            Priority = dto.Priority ?? "Medium",
            ExpectedValue = dto.ExpectedValue,
            CreatedDate = DateTime.UtcNow,
            AssignedTo = User.IsInRole("SalesExecutive") ? user.Id : dto.AssignedTo
        };

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("API Lead Created", "Lead", lead.LeadCode, null, $"API created lead {lead.LeadName}");

        dto.LeadId = lead.LeadId;
        dto.LeadCode = lead.LeadCode;

        return CreatedAtAction(nameof(GetAll), new { id = lead.LeadId }, dto);
    }
}
