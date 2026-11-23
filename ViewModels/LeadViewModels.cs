using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class ConvertLeadViewModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    public string? CompanyName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer name is required")]
    [Display(Name = "New Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "Create Opportunity from this Lead")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Range(0.01, 100000000, ErrorMessage = "Opportunity amount must be greater than zero")]
    [Display(Name = "Opportunity Amount ($)")]
    public decimal OpportunityAmount { get; set; }

    [Display(Name = "Initial Stage")]
    public string Stage { get; set; } = "Qualification";

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100")]
    public int Probability { get; set; } = 30;

    [Display(Name = "Expected Close Date")]
    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);
}
