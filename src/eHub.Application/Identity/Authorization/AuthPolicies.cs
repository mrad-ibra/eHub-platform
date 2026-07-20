namespace eHub.Application.Identity.Authorization;

public static class AuthPolicies
{
    public const string PermissionClaimType = "permission";

    public const string BookingsCreate = "bookings.create";
    public const string BookingsRead = "bookings.read";
    public const string BookingsManage = "bookings.manage";

    public const string PaymentsCreate = "payments.create";
    public const string PaymentsRead = "payments.read";
    public const string PaymentsCancel = "payments.cancel";
    public const string PaymentsRefund = "payments.refund";
}
