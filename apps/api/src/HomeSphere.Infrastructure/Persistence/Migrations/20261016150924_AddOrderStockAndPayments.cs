using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStockAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    public_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    discount_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    shipping_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    coupon_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    shipping_method = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    shipping_address_snapshot_json = table.Column<string>(type: "text", nullable: false),
                    billing_address_snapshot_json = table.Column<string>(type: "text", nullable: true),
                    customer_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    paid_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    shipped_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_orders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    product_name_snapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    total_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    attributes_json_snapshot = table.Column<string>(type: "text", nullable: true),
                    image_url_snapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_items_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    new_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    changed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_order_status_history_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transaction_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    raw_payload_json = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_attempts_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_reservations", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_reservations_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_reservations_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_product_variant_id",
                table: "order_items",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_status_history_created_at_utc",
                table: "order_status_history",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_order_status_history_order_id",
                table: "order_status_history",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_created_at_utc",
                table: "orders",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_number",
                table: "orders",
                column: "order_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_public_id",
                table: "orders",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_orders_user_id",
                table: "orders",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_created_at_utc",
                table: "payment_attempts",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_order_id",
                table: "payment_attempts",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_transaction_id",
                table: "payment_attempts",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_order_id",
                table: "stock_reservations",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_product_variant_id",
                table: "stock_reservations",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_reservations_status_expires_at_utc",
                table: "stock_reservations",
                columns: new[] { "status", "expires_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "order_status_history");

            migrationBuilder.DropTable(
                name: "payment_attempts");

            migrationBuilder.DropTable(
                name: "stock_reservations");

            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}
