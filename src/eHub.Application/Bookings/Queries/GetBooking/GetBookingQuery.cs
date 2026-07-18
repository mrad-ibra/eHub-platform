using eHub.Application.Bookings.Abstractions;
using eHub.Application.Common.Context;
using eHub.Application.Common.Messaging;
using eHub.Domain.Bookings;
using eHub.Domain.Exceptions;
using eHub.Localization;

namespace eHub.Application.Bookings.Queries.GetBooking;

public sealed record GetBookingQuery(Guid BookingId) : IQuery<BookingDetailDto>;

public sealed record BookingDetailDto(
    Guid Id,
    string BookingNumber,
    string Status,
    Guid AssetId,
    Guid RenterId,
    Guid HostId,
    DateOnly StartDate,
    DateOnly EndDate,
    int BufferDays,
    decimal UnitAmount,
    decimal TotalAmount,
    Guid CurrencyId,
    DateTime? ExpiresAtUtc,
    int AggregateVersion,
    string SnapshotName,
    string? SnapshotBrand,
    string? SnapshotModel,
    DateTime CreatedAtUtc);

public sealed class GetBookingQueryHandler(
    ICurrentUser currentUser,
    IBookingRepository bookings) : IQueryHandler<GetBookingQuery, BookingDetailDto>
{
    public async Task<BookingDetailDto> Handle(GetBookingQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var booking = await bookings.GetByIdAsync(request.BookingId, cancellationToken)
            ?? throw new NotFoundException(ErrorResources.Get(ErrorCodes.NotFound));

        var canRead = booking.RenterId == userId
                      || booking.HostId == userId
                      || currentUser.IsInRole("Admin");

        if (!canRead)
        {
            throw new ForbiddenAccessException(ErrorResources.Get(ErrorCodes.BookingAccessDenied));
        }

        return new BookingDetailDto(
            booking.Id,
            booking.BookingNumber,
            booking.Status.Value,
            booking.AssetId,
            booking.RenterId,
            booking.HostId,
            booking.Period.StartDate,
            booking.Period.EndDate,
            booking.BufferDays,
            booking.UnitPrice.Amount,
            booking.TotalPrice.Amount,
            booking.TotalPrice.CurrencyId,
            booking.ExpiresAtUtc,
            booking.AggregateVersion,
            booking.AssetSnapshot.Name,
            booking.AssetSnapshot.Brand,
            booking.AssetSnapshot.Model,
            booking.CreatedAtUtc);
    }
}
