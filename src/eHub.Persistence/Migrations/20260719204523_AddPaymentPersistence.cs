using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refunded_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    refunded_currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderPaymentId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AggregateVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.CheckConstraint("ck_payments_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_payments_refunded_bounds", "refunded_amount >= 0 AND refunded_amount <= amount");
                });

            migrationBuilder.CreateTable(
                name: "payment_attempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Detail = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_attempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_attempts_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderRefundId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RequestedByActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_refunds_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_status_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_status_history_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_timeline_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_timeline_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_timeline_entries_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_PaymentId",
                table: "payment_attempts",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_refunds_PaymentId",
                table: "payment_refunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_history_AtUtc",
                table: "payment_status_history",
                column: "AtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_history_PaymentId_AtUtc",
                table: "payment_status_history",
                columns: new[] { "PaymentId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_timeline_entries_PaymentId",
                table: "payment_timeline_entries",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_ExpiresAtUtc_Status",
                table: "payments",
                columns: new[] { "ExpiresAtUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_IdempotencyKey",
                table: "payments",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_Provider_ProviderPaymentId",
                table: "payments",
                columns: new[] { "Provider", "ProviderPaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_Status",
                table: "payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ux_payments_one_active_per_booking",
                table: "payments",
                column: "BookingId",
                unique: true,
                filter: "\"Status\" IN ('CREATED', 'PENDING', 'AUTHORIZED')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_attempts");

            migrationBuilder.DropTable(
                name: "payment_refunds");

            migrationBuilder.DropTable(
                name: "payment_status_history");

            migrationBuilder.DropTable(
                name: "payment_timeline_entries");

            migrationBuilder.DropTable(
                name: "payments");
        }
    }
}
