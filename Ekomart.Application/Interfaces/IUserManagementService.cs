using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Users;

namespace Ekomart.Application.Interfaces;

public interface IUserManagementService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(
        PagedRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task UpdateRolesAsync(
        UserRoleUpdateDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default);
}
