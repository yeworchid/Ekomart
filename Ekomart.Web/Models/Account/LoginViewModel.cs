using System.ComponentModel.DataAnnotations;

namespace Ekomart.Web.Models.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
