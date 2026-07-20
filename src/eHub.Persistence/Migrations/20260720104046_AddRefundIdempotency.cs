using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "payment_refunds",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_payment_refunds_payment_idempotency",
                table: "payment_refunds",
                columns: new[] { "PaymentId", "IdempotencyKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_payment_refunds_payment_idempotency",
                table: "payment_refunds");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "payment_refunds");
        }
    }
}
