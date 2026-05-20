using System.Security.Claims;
using Ekomart.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Ekomart.Web.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public const string ManagersGroup = "orders:managers";

    public static string UserGroup(string userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        if (Context.User?.IsInRole(AppRoles.Manager) == true ||
            Context.User?.IsInRole(AppRoles.Admin) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ManagersGroup);
        }

        await base.OnConnectedAsync();
    }
}
