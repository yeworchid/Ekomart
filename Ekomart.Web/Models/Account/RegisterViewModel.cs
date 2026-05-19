using System.ComponentModel.DataAnnotations;

namespace Ekomart.Web.Models.Account;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите имя.")]
    [StringLength(120, ErrorMessage = "Имя не должно быть длиннее 120 символов.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите email.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "Пароли должны совпадать.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
