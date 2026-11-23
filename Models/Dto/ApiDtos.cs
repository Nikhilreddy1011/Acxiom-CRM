using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Dto;

public class LoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = "Active";
    public string? AssignedTo { get; set; }
}

public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;

    [Required]
    public string LeadName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string Source { get; set; } = "Website";
    public string Status { get; set; } = "New";
    public string Priority { get; set; } = "Medium";
    public decimal ExpectedValue { get; set; }
    public string? AssignedTo { get; set; }
}

public class OpportunityDto
{
    public int OpportunityId { get; set; }

    [Required]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }

    [Range(0.01, 100000000)]
    public decimal Amount { get; set; }

    public string Stage { get; set; } = "Qualification";

    [Range(0, 100)]
    public int Probability { get; set; } = 20;

    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public string Status { get; set; } = "Open";
    public string? AssignedTo { get; set; }
    public decimal WeightedPipeline => Amount * Probability / 100m;
}

public class FollowUpDto
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    [Required]
    public string FollowUpType { get; set; } = "Call";

    [Required]
    public string Subject { get; set; } = string.Empty;

    public string? Remarks { get; set; }
    public string Status { get; set; } = "Planned";
    public string? AssignedTo { get; set; }
}
