namespace Ekomart.Domain.Enums;

public enum AuditAction
{
    Login = 1,
    Logout = 2,
    Register = 3,
    ProductCreated = 10,
    ProductUpdated = 11,
    ProductDeleted = 12,
    CategoryCreated = 20,
    CategoryUpdated = 21,
    CategoryDeleted = 22,
    OrderCreated = 30,
    OrderStatusChanged = 31,
    SubscriptionRenewed = 40,
    SubscriptionPlanCreated = 41,
    SubscriptionPlanUpdated = 42,
    SubscriptionPlanDeleted = 43,
    UserRoleChanged = 50
}
