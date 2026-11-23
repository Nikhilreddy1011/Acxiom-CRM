using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Opportunity
{
    [Key]
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity name is mandatory")]
    [MaxLength(150)]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 100000000, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

    [Required]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected close date is required")]
    [Display(Name = "Expected Close Date")]
    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Open"; // Open, Won, Lost, Inactive

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    [NotMapped]
    [Display(Name = "Weighted Pipeline Value")]
    public decimal WeightedPipeline => Amount * Probability / 100m;
}
