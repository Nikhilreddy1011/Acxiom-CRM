using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [MaxLength(50)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer name is mandatory")]
    [MaxLength(150)]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

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

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Inactive

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
