using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Application.Identity.Abstractions;
using eHub.Domain.Exceptions;
using eHub.Domain.Resources;

namespace eHub.Application.Identity.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    IUserRepository users) : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException(ErrorResources.Get(ErrorCodes.UserNotFound));
        }

        return new CurrentUserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Phone,
            user.AccountKind.ToString(),
            user.IsEmailConfirmed,
            user.ProfilePictureUrl,
            user.IsActive,
            currentUser.Roles,
            user.CreatedAtUtc);
    }
}
