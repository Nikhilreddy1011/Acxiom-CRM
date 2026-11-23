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
[Route("api/opportunities")]
[Authorize]
public class OpportunitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public OpportunitiesApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService)
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
        var query = _context.Opportunities.AsQueryable();

        if (isSales)
        {
            query = query.Where(o => o.AssignedTo == user.Id);
        }

        var opps = await query
            .Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                LeadId = o.LeadId,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                AssignedTo = o.AssignedTo
            })
            .ToListAsync();

        return Ok(opps);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OpportunityDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Amount must be greater than zero." });
        }

        if (dto.Probability < 0 || dto.Probability > 100)
        {
            return BadRequest(new { message = "Probability must be between 0 and 100." });
        }

        var opportunity = new Opportunity
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage ?? "Qualification",
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status ?? "Open",
            CreatedDate = DateTime.UtcNow,
            AssignedTo = User.IsInRole("SalesExecutive") ? user.Id : dto.AssignedTo
        };

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("API Opportunity Created", "Opportunity", opportunity.OpportunityName, null, $"Amount: ${opportunity.Amount:N2}, Stage: {opportunity.Stage}");

        dto.OpportunityId = opportunity.OpportunityId;
        return CreatedAtAction(nameof(GetAll), new { id = opportunity.OpportunityId }, dto);
    }
}
