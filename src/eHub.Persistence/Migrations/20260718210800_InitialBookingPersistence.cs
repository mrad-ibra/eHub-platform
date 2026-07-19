using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialBookingPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "booking_idempotency_entries",
                columns: table => new
                {
                    RenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_idempotency_entries", x => new { x.RenterId, x.IdempotencyKey });
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    RenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostId = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    BufferDays = table.Column<int>(type: "integer", nullable: false),
                    unit_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    snapshot_brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    snapshot_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    snapshot_primary_image_urls = table.Column<string>(type: "jsonb", nullable: false),
                    snapshot_host_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_host_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    snapshot_captured_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    terms_min_rental_days = table.Column<int>(type: "integer", nullable: true),
                    terms_max_rental_days = table.Column<int>(type: "integer", nullable: true),
                    terms_min_driver_age = table.Column<int>(type: "integer", nullable: true),
                    terms_requires_license = table.Column<bool>(type: "boolean", nullable: false),
                    terms_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    terms_buffer_days = table.Column<int>(type: "integer", nullable: false),
                    pickup_use_asset_location = table.Column<bool>(type: "boolean", nullable: false),
                    pickup_address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    pickup_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    dropoff_use_asset_location = table.Column<bool>(type: "boolean", nullable: false),
                    dropoff_address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    dropoff_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    driver_requested = table.Column<bool>(type: "boolean", nullable: false),
                    driver_fee_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    driver_fee_currency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delivery_requested = table.Column<bool>(type: "boolean", nullable: false),
                    delivery_fee_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    delivery_fee_currency_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delivery_address_line = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AggregateVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "booking_status_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_booking_status_history_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_timeline_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_timeline_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_booking_timeline_entries_bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_booking_idempotency_entries_ExpiresAtUtc",
                table: "booking_idempotency_entries",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_booking_status_history_BookingId",
                table: "booking_status_history",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_booking_timeline_entries_BookingId",
                table: "booking_timeline_entries",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_AssetId_Status",
                table: "bookings",
                columns: new[] { "AssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_BookingNumber",
                table: "bookings",
                column: "BookingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bookings_ExpiresAtUtc_Status",
                table: "bookings",
                columns: new[] { "ExpiresAtUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_HostId_CreatedAtUtc",
                table: "bookings",
                columns: new[] { "HostId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_RenterId_CreatedAtUtc",
                table: "bookings",
                columns: new[] { "RenterId", "CreatedAtUtc" });

            // Correctness line (Sprint 5.2A / 5.2A.1): multi-instance overlap prevention.
            // Application ListBlockingByAssetAsync is UX-only early rejection.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS booking_number_seq START 1;");
            migrationBuilder.Sql("""
                ALTER TABLE bookings
                ADD CONSTRAINT bookings_no_overlap
                EXCLUDE USING gist (
                    "AssetId" WITH =,
                    daterange(start_date, end_date + "BufferDays", '[]') WITH &&
                )
                WHERE ("Status" IN (
                    'PENDING_OWNER_APPROVAL',
                    'PENDING_PAYMENT',
                    'CONFIRMED',
                    'IN_PROGRESS'
                ));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE bookings DROP CONSTRAINT IF EXISTS bookings_no_overlap;");
            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS booking_number_seq;");

            migrationBuilder.DropTable(
                name: "booking_idempotency_entries");

            migrationBuilder.DropTable(
                name: "booking_status_history");

            migrationBuilder.DropTable(
                name: "booking_timeline_entries");

            migrationBuilder.DropTable(
                name: "bookings");
        }
    }
}
