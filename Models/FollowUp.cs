using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class FollowUp
{
    [Key]
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    [Required(ErrorMessage = "Follow-up date is required")]
    [Display(Name = "Follow-up Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Required]
    [MaxLength(50)]
    [Display(Name = "Follow-up Type")]
    public string FollowUpType { get; set; } = "Call"; // Call, Email, Meeting, Demo

    [Required(ErrorMessage = "Subject is mandatory")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Remarks { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }
}
