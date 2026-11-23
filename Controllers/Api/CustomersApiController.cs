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
[Route("api/customers")]
[Authorize]
public class CustomersApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public CustomersApiController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
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
        var query = _context.Customers.AsQueryable();

        if (isSales)
        {
            query = query.Where(c => c.AssignedTo == user.Id);
        }

        var customers = await query
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                AssignedTo = c.AssignedTo
            })
            .ToListAsync();

        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var c = await _context.Customers.FindAsync(id);
        if (c == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && c.AssignedTo != user.Id)
        {
            return Forbid();
        }

        return Ok(new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Status = c.Status,
            AssignedTo = c.AssignedTo
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CustomerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (await _context.Customers.AnyAsync(c => c.Email.ToLower() == dto.Email.ToLower()))
        {
            return Conflict(new { message = "Email already in use." });
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == dto.Phone))
        {
            return Conflict(new { message = "Phone already in use." });
        }

        var count = await _context.Customers.CountAsync() + 1001;
        var customer = new Customer
        {
            CustomerCode = $"CUST-{count}",
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Status = dto.Status ?? "Active",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = user.Id,
            AssignedTo = User.IsInRole("SalesExecutive") ? user.Id : dto.AssignedTo
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("API Customer Created", "Customer", customer.CustomerCode, null, $"Created customer {customer.CustomerName}");

        dto.CustomerId = customer.CustomerId;
        dto.CustomerCode = customer.CustomerCode;

        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CustomerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var existing = await _context.Customers.FindAsync(id);
        if (existing == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && existing.AssignedTo != user.Id)
        {
            return Forbid();
        }

        existing.CustomerName = dto.CustomerName;
        existing.Email = dto.Email;
        existing.Phone = dto.Phone;
        existing.CompanyName = dto.CompanyName;
        existing.Address = dto.Address;
        existing.City = dto.City;
        existing.State = dto.State;
        existing.Status = dto.Status;

        if (!User.IsInRole("SalesExecutive"))
        {
            existing.AssignedTo = dto.AssignedTo;
        }

        await _context.SaveChangesAsync();
        await _auditService.LogAsync("API Customer Updated", "Customer", existing.CustomerCode, null, $"Updated customer {existing.CustomerName}");

        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var existing = await _context.Customers.FindAsync(id);
        if (existing == null) return NotFound();

        if (User.IsInRole("SalesExecutive") && existing.AssignedTo != user.Id)
        {
            return Forbid();
        }

        existing.Status = "Inactive";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("API Customer Deactivated", "Customer", existing.CustomerCode, null, $"Deactivated customer {existing.CustomerName}");
        return NoContent();
    }
}
