namespace eHub.Application.Identity.Authorization;

public static class AuthPolicies
{
    public const string PermissionClaimType = "permission";

    public const string BookingsCreate = "bookings.create";
    public const string BookingsRead = "bookings.read";
    public const string BookingsManage = "bookings.manage";
}
