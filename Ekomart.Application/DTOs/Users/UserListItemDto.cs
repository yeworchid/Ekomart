namespace Ekomart.Application.DTOs.Users;

public class UserListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
}
