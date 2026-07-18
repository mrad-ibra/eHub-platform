using eHub.Domain.Common;

namespace eHub.Domain.Identity;

public sealed class User : SoftDeletableEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public AccountKind AccountKind { get; private set; }
    public bool IsEmailConfirmed { get; private set; }
    public string? EmailVerificationToken { get; private set; }
    public DateTime? EmailVerificationTokenExpiresUtc { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiresUtc { get; private set; }
    public string? GoogleSubjectId { get; private set; }
    public string? ProfilePictureUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<UserRole> _roles = [];
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    private User()
    {
    }

    public static User Register(
        string email,
        string passwordHash,
        string fullName,
        AccountKind accountKind,
        DateTime nowUtc)
    {
        var normalizedEmail = NormalizeEmail(email);
        AppGuard.NotEmpty(normalizedEmail, nameof(email));
        AppGuard.NotEmpty(passwordHash, nameof(passwordHash));
        AppGuard.NotEmpty(fullName, nameof(fullName));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            AccountKind = accountKind,
            IsEmailConfirmed = false,
            IsActive = true
        };

        user.SetCreatedAudit(nowUtc);
        return user;
    }

    public static User RegisterFromGoogle(
        string email,
        string unusablePasswordHash,
        string fullName,
        string googleSubjectId,
        string? profilePictureUrl,
        AccountKind accountKind,
        DateTime nowUtc)
    {
        var user = Register(email, unusablePasswordHash, fullName, accountKind, nowUtc);
        user.GoogleSubjectId = AppGuard.NotEmpty(googleSubjectId, nameof(googleSubjectId));
        user.ProfilePictureUrl = profilePictureUrl?.Trim();
        user.ConfirmEmail(nowUtc);
        return user;
    }

    public static User SeedAdmin(
        string email,
        string passwordHash,
        string fullName,
        DateTime nowUtc)
    {
        var user = Register(email, passwordHash, fullName, AccountKind.Admin, nowUtc);
        user.ConfirmEmail(nowUtc);
        return user;
    }

    public void UpdateProfile(string fullName, string? phone, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        FullName = AppGuard.NotEmpty(fullName, nameof(fullName)).Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void ChangePassword(string passwordHash, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        PasswordHash = AppGuard.NotEmpty(passwordHash, nameof(passwordHash));
        ClearPasswordResetToken();
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void SetPasswordResetToken(string token, DateTime expiresUtc, DateTime nowUtc)
    {
        EnsureNotDeleted();
        PasswordResetToken = AppGuard.NotEmpty(token, nameof(token));
        PasswordResetTokenExpiresUtc = expiresUtc;
        SetUpdatedAudit(nowUtc);
    }

    public bool TryResetPasswordWithToken(string token, string newPasswordHash, DateTime nowUtc)
    {
        if (IsDeleted)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(PasswordResetToken)
            || !string.Equals(PasswordResetToken, token, StringComparison.Ordinal)
            || PasswordResetTokenExpiresUtc is null
            || PasswordResetTokenExpiresUtc < nowUtc)
        {
            return false;
        }

        ChangePassword(newPasswordHash, nowUtc);
        return true;
    }

    public void SetEmailVerificationToken(string token, DateTime expiresUtc, DateTime nowUtc)
    {
        EnsureNotDeleted();
        EmailVerificationToken = AppGuard.NotEmpty(token, nameof(token));
        EmailVerificationTokenExpiresUtc = expiresUtc;
        SetUpdatedAudit(nowUtc);
    }

    public void ConfirmEmail(DateTime nowUtc)
    {
        EnsureNotDeleted();
        IsEmailConfirmed = true;
        EmailVerificationToken = null;
        EmailVerificationTokenExpiresUtc = null;
        SetUpdatedAudit(nowUtc);
    }

    public bool TryConfirmEmailWithToken(string token, DateTime nowUtc)
    {
        if (IsDeleted)
        {
            return false;
        }

        if (IsEmailConfirmed)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(EmailVerificationToken)
            || !string.Equals(EmailVerificationToken, token, StringComparison.Ordinal)
            || EmailVerificationTokenExpiresUtc is null
            || EmailVerificationTokenExpiresUtc < nowUtc)
        {
            return false;
        }

        ConfirmEmail(nowUtc);
        return true;
    }

    public void LinkGoogleAccount(
        string googleSubjectId,
        string? fullName,
        string? profilePictureUrl,
        DateTime nowUtc)
    {
        EnsureNotDeleted();
        GoogleSubjectId = AppGuard.NotEmpty(googleSubjectId, nameof(googleSubjectId));

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            FullName = fullName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(profilePictureUrl))
        {
            ProfilePictureUrl = profilePictureUrl.Trim();
        }

        ConfirmEmail(nowUtc);
    }

    public void Deactivate(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        IsActive = false;
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void Activate(DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        IsActive = true;
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void AssignRole(Role role, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        ArgumentNullException.ThrowIfNull(role);

        if (HasRole(role.Id))
        {
            return;
        }

        _roles.Add(UserRole.Create(Id, role.Id, nowUtc));
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public void RemoveRole(Guid roleId, DateTime nowUtc, Guid? updatedBy = null)
    {
        EnsureNotDeleted();
        var existing = _roles.FirstOrDefault(role => role.RoleId == roleId);
        if (existing is null)
        {
            return;
        }

        _roles.Remove(existing);
        SetUpdatedAudit(nowUtc, updatedBy);
    }

    public bool HasRole(Guid roleId)
        => _roles.Any(role => role.RoleId == roleId);

    private void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiresUtc = null;
    }

    private static string NormalizeEmail(string email)
        => email.Trim().ToLowerInvariant();
}
