namespace eHub.Domain.Identity;

public enum LoginFailureReason
{
    InvalidCredentials = 1,
    AccountInactive = 2,
    EmailNotConfirmed = 3
}
