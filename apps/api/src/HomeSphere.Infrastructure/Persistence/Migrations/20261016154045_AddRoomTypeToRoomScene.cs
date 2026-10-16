using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomTypeToRoomScene : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "room_type",
                table: "room_scenes",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "Living");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "room_type",
                table: "room_scenes");
        }
    }
}
