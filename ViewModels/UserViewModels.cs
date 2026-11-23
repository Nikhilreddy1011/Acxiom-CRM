using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateUserViewModel
{
    [Required]
    [MaxLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "SalesExecutive";

    [Display(Name = "Active Account")]
    public bool IsActive { get; set; } = true;
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "SalesExecutive";

    [Display(Name = "Active Account")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Unlock Account (if locked)")]
    public bool UnlockAccount { get; set; }
}
