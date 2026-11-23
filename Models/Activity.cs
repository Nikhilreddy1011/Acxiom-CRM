using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Activity Type")]
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is mandatory")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    public int? OpportunityId { get; set; }

    [ForeignKey("OpportunityId")]
    public virtual Opportunity? Opportunity { get; set; }

    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Completed"; // Pending, Completed
}
