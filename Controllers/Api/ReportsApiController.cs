using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipelineReport()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var isSales = User.IsInRole("SalesExecutive");
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedUser)
            .AsQueryable();

        if (isSales)
        {
            query = query.Where(o => o.AssignedTo == user.Id);
        }

        var opps = await query.ToListAsync();

        var report = new
        {
            TotalOpportunities = opps.Count,
            TotalPipelineValue = opps.Where(o => o.Status == "Open").Sum(o => o.Amount),
            TotalWeightedValue = opps.Where(o => o.Status == "Open").Sum(o => o.WeightedPipeline),
            StageBreakdown = opps
                .GroupBy(o => o.Stage)
                .Select(g => new
                {
                    Stage = g.Key,
                    Count = g.Count(),
                    TotalAmount = g.Sum(x => x.Amount)
                }),
            Opportunities = opps.Select(o => new
            {
                o.OpportunityId,
                o.OpportunityName,
                Customer = o.Customer?.CustomerName,
                o.Amount,
                o.Stage,
                o.Probability,
                o.WeightedPipeline,
                o.ExpectedCloseDate,
                o.Status,
                AssignedTo = o.AssignedUser?.FullName
            })
        };

        return Ok(report);
    }
}
