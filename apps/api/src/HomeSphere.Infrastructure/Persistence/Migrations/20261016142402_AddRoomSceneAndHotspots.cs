using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeSphere.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomSceneAndHotspots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "room_scenes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    slug = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    image_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room_scenes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scene_hotspots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_scene_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_variant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    x_coordinate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    y_coordinate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scene_hotspots", x => x.id);
                    table.ForeignKey(
                        name: "FK_scene_hotspots_product_variants_product_variant_id",
                        column: x => x.product_variant_id,
                        principalTable: "product_variants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_scene_hotspots_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scene_hotspots_room_scenes_room_scene_id",
                        column: x => x.room_scene_id,
                        principalTable: "room_scenes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_room_scenes_slug",
                table: "room_scenes",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scene_hotspots_product_id",
                table: "scene_hotspots",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_scene_hotspots_product_variant_id",
                table: "scene_hotspots",
                column: "product_variant_id");

            migrationBuilder.CreateIndex(
                name: "ix_scene_hotspots_room_scene_id",
                table: "scene_hotspots",
                column: "room_scene_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scene_hotspots");

            migrationBuilder.DropTable(
                name: "room_scenes");
        }
    }
}
