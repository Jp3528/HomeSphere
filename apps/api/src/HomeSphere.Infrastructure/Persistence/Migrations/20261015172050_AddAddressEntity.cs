using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    line1 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    line2 = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    district = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    city = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    region = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_addresses", x => x.id);
                    table.ForeignKey(
                        name: "FK_addresses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_addresses_user_id",
                table: "addresses",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses");
        }
    }
}
