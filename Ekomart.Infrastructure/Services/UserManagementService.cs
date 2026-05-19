using Ekomart.Application.Common;
using Ekomart.Application.DTOs.Users;
using Ekomart.Application.Interfaces;
using Ekomart.Domain.Enums;
using Ekomart.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ekomart.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IAuditService _auditService;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users
            .AsNoTracking()
            .OrderBy(user => user.Email);

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNumber = Paging.PageNumber(request);
        var pageSize = Paging.PageSize(request);

        var users = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var result = new List<UserListItemDto>(users.Count);
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new UserListItemDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                Roles = roles.ToArray()
            });
        }

        return Paging.Result(result, request, totalCount);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        return await _roleManager.Roles
            .AsNoTracking()
            .Select(role => role.Name!)
            .OrderBy(role => role)
            .ToArrayAsync(cancellationToken);
    }

    public async Task UpdateRolesAsync(
        UserRoleUpdateDto dto,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user is null)
        {
            throw new InvalidOperationException("User was not found.");
        }

        var requestedRoles = dto.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var role in requestedRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                throw new InvalidOperationException($"Role '{role}' was not found.");
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles
            .Except(requestedRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rolesToAdd = requestedRoles
            .Except(currentRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            EnsureSuccess(removeResult);
        }

        if (rolesToAdd.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            EnsureSuccess(addResult);
        }

        await _auditService.LogAsync(
            AuditAction.UserRoleChanged,
            nameof(ApplicationUser),
            user.Id,
            adminUserId,
            $"Roles changed for {user.Email}: {string.Join(", ", requestedRoles)}.",
            cancellationToken: cancellationToken);
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException(errors);
    }
}
