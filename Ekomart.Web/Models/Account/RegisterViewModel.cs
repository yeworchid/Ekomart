using System.ComponentModel.DataAnnotations;

namespace Ekomart.Web.Models.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter name.")]
    [StringLength(120, ErrorMessage = "Name cannot be longer than 120 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter password.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Passwords must match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
