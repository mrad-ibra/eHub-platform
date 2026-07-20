using eHub.Persistence;

namespace eHub.UnitTests.Persistence;

public sealed class PostgresExceptionMapperTests
{
    [Theory]
    [InlineData("ux_payments_one_active_per_booking", true)]
    [InlineData("UX_PAYMENTS_ONE_ACTIVE_PER_BOOKING", true)]
    [InlineData("IX_payments_IdempotencyKey", false)]
    public void IsPaymentActivePerBookingConstraint_DetectsConstraint(string name, bool expected)
        => PostgresExceptionMapper.IsPaymentActivePerBookingConstraint(name).Should().Be(expected);

    [Theory]
    [InlineData("IX_payments_IdempotencyKey", true)]
    [InlineData("ux_payments_one_active_per_booking", false)]
    public void IsPaymentIdempotencyConstraint_DetectsConstraint(string name, bool expected)
        => PostgresExceptionMapper.IsPaymentIdempotencyConstraint(name).Should().Be(expected);

    [Theory]
    [InlineData("ux_payment_refunds_payment_idempotency", true)]
    [InlineData("IX_payments_IdempotencyKey", false)]
    public void IsPaymentRefundIdempotencyConstraint_DetectsConstraint(string name, bool expected)
        => PostgresExceptionMapper.IsPaymentRefundIdempotencyConstraint(name).Should().Be(expected);
}
