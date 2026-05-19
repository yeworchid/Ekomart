using System.ComponentModel.DataAnnotations;

namespace Ekomart.Web.Models.Profile;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Введите имя.")]
    [StringLength(120, ErrorMessage = "Имя не должно быть длиннее 120 символов.")]
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
