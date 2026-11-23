using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [MaxLength(50)]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead name is mandatory")]
    [MaxLength(150)]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is mandatory")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is mandatory")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [MaxLength(100)]
    public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Advertisement, Partner, Other

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Required]
    [MaxLength(50)]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Expected Value")]
    [Range(0, 100000000, ErrorMessage = "Expected value must be non-negative")]
    public decimal ExpectedValue { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
