using System.ComponentModel.DataAnnotations;

namespace Ekomart.Application.DTOs.Users;

public class UserRoleUpdateDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
